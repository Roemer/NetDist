using NetDist.Core;
using NetDist.Handlers;
using NetDist.Jobs;
using NetDist.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NetDist.Core.Extensions;

namespace NetDist.Server
{
    /// <summary>
    /// Represents a handler which is loaded and active
    /// This object runs in it's own domain
    /// </summary>
    public class LoadedHandler : MarshalByRefObject
    {
        /// <summary>
        /// Logger object
        /// </summary>
        public Logger Logger { get; set; }

        /// <summary>
        /// ID of the loaded handler
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Object which holds the handler settings
        /// </summary>
        public HandlerSettings HandlerSettings { get; private set; }

        /// <summary>
        /// Current state of the handler
        /// </summary>
        public HandlerState HandlerState { get; private set; }

        /// <summary>
        /// Queue for the available jobs
        /// </summary>
        protected ConcurrentQueue<Job> AvailableJobs;

        /// <summary>
        /// List for jobs which are in progress
        /// </summary>
        protected Dictionary<Guid, JobAssigned> PendingJobs;

        /// <summary>
        /// List for jobs which are finished and waiting to be collected
        /// </summary>
        protected ConcurrentQueue<JobFinished> FinishedJobs;

        /// <summary>
        /// Full name of the handler: PluginName/HandlerName/JobName
        /// </summary>
        public string FullName
        {
            get { return String.Format("{0}/{1}/{2}", HandlerSettings.PluginName, HandlerSettings.HandlerName, HandlerSettings.JobName); }
        }

        /// <summary>
        /// Instance of the effective handler
        /// </summary>
        private IHandler _handler;

        /// <summary>
        /// Object used for stuff that should be thread-safe
        /// </summary>
        private readonly object _lockObject = new object();

        private Task _controlTask;
        private CancellationTokenSource _controlTaskCancelToken = new CancellationTokenSource();
        private readonly AutoResetEvent _jobsEmptyWaitHandle = new AutoResetEvent(false);
        private readonly AutoResetEvent _resultAvailableWaitHandle = new AutoResetEvent(false);

        /// <summary>
        /// Constructor
        /// </summary>
        public LoadedHandler(string handlerSettingsString)
        {
            Id = Guid.NewGuid();
            HandlerSettings = JobObjectSerializer.Deserialize<HandlerSettings>(handlerSettingsString);
            AvailableJobs = new ConcurrentQueue<Job>();
            PendingJobs = new Dictionary<Guid, JobAssigned>();
            FinishedJobs = new ConcurrentQueue<JobFinished>();
            HandlerState = HandlerState.Stopped;
        }

        /// <summary>
        /// Lifetime override of the proxy object
        /// </summary>
        public override object InitializeLifetimeService()
        {
            // Infinite lifetime
            return null;
        }

        /// <summary>
        /// Tries to initialize the appropriate handler
        /// </summary>
        public bool InitializeHandler(string handlersFolder, string handlerCustomSettingsString)
        {
            var pluginName = HandlerSettings.PluginName;
            var handlerName = HandlerSettings.HandlerName;

            var pluginPath = Path.Combine(handlersFolder, String.Format("{0}.dll", pluginName));
            var handlerAssembly = Assembly.LoadFile(pluginPath);

            Type typeToLoad = null;
            foreach (var type in handlerAssembly.GetTypes())
            {
                if (typeof(IHandler).IsAssignableFrom(type))
                {
                    var att = type.GetCustomAttribute<HandlerNameAttribute>(true);
                    if (att != null)
                    {
                        if (att.HandlerName == handlerName)
                        {
                            typeToLoad = type;
                            break;
                        }
                    }
                }
            }
            if (typeToLoad != null)
            {
                var handlerInstance = (IHandler)Activator.CreateInstance(typeToLoad);
                // Initialize the handler with the custom settings
                handlerInstance.InitializeCustomSettings(handlerCustomSettingsString);
                // Call the virtual initialize method
                handlerInstance.Initialize();
                // Assign the handler
                _handler = handlerInstance;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Starts the job-logic so jobs are generated and processed
        /// </summary>
        public void StartJobLogic()
        {
            lock (_lockObject)
            {
                // Check if the control thread is not yet running
                if (_controlTask == null)
                {
                    // Start the control task
                    _controlTaskCancelToken = new CancellationTokenSource();
                    _controlTask = new Task(ControlThread);
                    _controlTask.ContinueWith(t =>
                    {
                        Logger.Error(t.Exception, "Exception in handler '{0}'", Id);
                        StopJoblogic();
                    }, TaskContinuationOptions.OnlyOnFaulted);
                    HandlerState = HandlerState.Running;
                    _controlTask.Start();
                }
            }
        }

        /// <summary>
        /// Stops the job-logic
        /// </summary>
        public void StopJoblogic()
        {
            // Use an own task since this might take a little longer
            Task.Run(() =>
            {
                lock (_lockObject)
                {
                    // Check if the control thread is running
                    if (_controlTask != null)
                    {
                        // Notify the task to stop
                        _controlTaskCancelToken.Cancel();
                        // Wait until the task is finished (but not when faulted)
                        if (!_controlTask.IsFaulted)
                        {
                            _controlTask.Wait();
                        }
                        // Reset the control task
                        _controlTask = null;
                        // Clear the various queues/lists
                        AvailableJobs = new ConcurrentQueue<Job>();
                        lock (PendingJobs.GetSyncRoot())
                        {
                            PendingJobs = new Dictionary<Guid, JobAssigned>();
                        }
                        FinishedJobs = new ConcurrentQueue<JobFinished>();
                        // Signal the handler to stop
                        _handler.OnStop();
                        HandlerState = HandlerState.Stopped;
                    }
                }
            });
        }

        /// <summary>
        /// Gets the next job from the available queue
        /// </summary>
        public Job GetNextJob()
        {
            Job nextJob;
            var success = AvailableJobs.TryDequeue(out nextJob);
            if (success)
            {
                var assignedJob = new JobAssigned
                {
                    Job = nextJob,
                    StartTime = DateTime.Now
                };
                // Add it to pending jobs
                lock (PendingJobs.GetSyncRoot())
                {
                    PendingJobs[assignedJob.Job.Id] = assignedJob;
                }
                // Check if more jobs are available
                if (AvailableJobs.IsEmpty)
                {
                    // If not, set the waithandle to get new jobs
                    _jobsEmptyWaitHandle.Set();
                }
                return nextJob;
            }
            return null;
        }

        public void ReceivedResult(JobResult result)
        {
            Task.Run(() =>
            {
                // Catch case where we receive results for an already stopped handler
                if (HandlerState == HandlerState.Stopped)
                {
                    Logger.Warn("Got job '{0}' result for stopped handler", result.Id);
                    return;
                }

                // Check if there was an error processing the job
                if (result.HasError)
                {
                    Logger.Error("Got failed result for job '{0}': {1}", result.Id, result.Error.ToString());
                    // If so, remove it from the in-progress list
                    var jobFailed = RemoveJobFromInProgress(result.Id);
                    // Add the job to the queue again
                    AvailableJobs.Enqueue(jobFailed.Job);
                    return;
                }

                Logger.Info("Got result for job '{0}': {1}", result.Id, result.JobOutputString);

                // Remove Job from in-progress list
                var jobInProgress = RemoveJobFromInProgress(result.Id);

                // Add job to finished list
                var finishedJob = new JobFinished
                {
                    Job = jobInProgress.Job,
                    ResultString = result.JobOutputString
                };
                FinishedJobs.Enqueue(finishedJob);
                _resultAvailableWaitHandle.Set();
            });
        }

        /// <summary>
        /// Removes a job from the in-progress list and returns the job
        /// </summary>
        private JobAssigned RemoveJobFromInProgress(Guid jobId)
        {
            JobAssigned jobInProgress;
            lock (PendingJobs.GetSyncRoot())
            {
                jobInProgress = PendingJobs[jobId];
                PendingJobs.Remove(jobId);
            }
            return jobInProgress;
        }

        /// <summary>
        /// Control thread for this handler which is run when it is started
        /// - Refills the job-queue if needed
        /// - Checks for job timeouts and then resends the jobs
        /// - Collects and processes the results
        /// </summary>
        private void ControlThread()
        {
            // Notify the handler that it has started
            _handler.OnStart();

            while (!_controlTaskCancelToken.IsCancellationRequested)
            {
                // Collect results
                JobFinished finishedJob;
                while (FinishedJobs.TryDequeue(out finishedJob))
                {
                    Logger.Info("Collecting finished job '{0}' with result '{1}'", finishedJob.Job.Id, finishedJob.ResultString);
                    _handler.ProcessResult(finishedJob.Job.JobInput, finishedJob.ResultString);
                }

                // Check for jobs with a timeout
                var jobTimeout = 900; // TODO: needs to be a config somewhere
                if (jobTimeout > 0)
                {
                    var now = DateTime.Now;
                    var jobsToRequeue = new List<Job>();
                    lock (PendingJobs.GetSyncRoot())
                    {
                        foreach (var kvp in PendingJobs)
                        {
                            if (now - kvp.Value.StartTime > TimeSpan.FromSeconds(jobTimeout))
                            {
                                // Job had a timeout
                                jobsToRequeue.Add(kvp.Value.Job);
                            }
                        }
                        foreach (var job in jobsToRequeue)
                        {
                            Logger.Warn("Job '{0}' had a timeout", job.Id);
                            PendingJobs.Remove(job.Id);
                            AvailableJobs.Enqueue(job);
                        }
                    }
                }

                // Refill available jobs if needed
                if (AvailableJobs.IsEmpty)
                {
                    Logger.Debug("Job queue is empty, adding new jobs");
                    // Fill with Jobs
                    var newJobInputs = _handler.GetJobs();
                    foreach (var input in newJobInputs)
                    {
                        var job = new Job(Id, input);
                        AvailableJobs.Enqueue(job);
                    }
                }

                // Stop if the handler is finished
                if (_handler.IsFinished)
                {
                    Logger.Info("Handler '{0}' finished successfully", Id);
                    lock (_lockObject)
                    {
                        _handler.OnFinished();
                        HandlerState = HandlerState.Finished;
                        _controlTask = null;
                        return;
                    }
                }

                // Sleep a little or until any of the various events was set
                WaitHandle.WaitAny(new[] { _controlTaskCancelToken.Token.WaitHandle, _jobsEmptyWaitHandle, _resultAvailableWaitHandle }, 5000);
            }
        }
    }
}
