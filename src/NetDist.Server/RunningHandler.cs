using NetDist.Core;
using NetDist.Core.Extensions;
using NetDist.Handlers;
using NetDist.Jobs;
using NetDist.Jobs.DataContracts;
using NetDist.Logging;
using NetDist.Server.XDomainObjects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NetDist.Server
{
    /// <summary>
    /// Represents a handler which is loaded. Contains the user-defined handler and eveything needed to control it
    /// </summary>
    public class RunningHandler
    {
        /// <summary>
        /// Logger object
        /// </summary>
        public Logger Logger { get; private set; }

        /// <summary>
        /// ID of the loaded handler
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Flag to check if there are available jobs
        /// </summary>
        public bool HasAvailableJobs
        {
            get { return !_availableJobs.IsEmpty; }
        }

        /// <summary>
        /// Event when the state of the running handler changed
        /// </summary>
        public event EventHandler<RunningHandlerStateChangedEventArgs> StateChangedEvent;

        private IHandler _handler;
        private HandlerSettings _handlerSettings;
        private ConcurrentQueue<JobWrapper> _availableJobs;
        private Dictionary<Guid, JobWrapper> _pendingJobs;
        private ConcurrentQueue<JobWrapper> _finishedJobs;
        private long _totalProcessedJobs;
        private long _totalFailedJobs;
        private string _jobScriptHash;
        private Task _controlTask;
        private CancellationTokenSource _controlTaskCancelToken;
        private readonly object _lockObject = new object();
        private readonly PackageManager _packageManager;
        private readonly AutoResetEvent _jobsEmptyWaitHandle = new AutoResetEvent(false);
        private readonly AutoResetEvent _resultAvailableWaitHandle = new AutoResetEvent(false);

        /// <summary>
        /// Constructor
        /// </summary>
        public RunningHandler(Guid id, string packageBaseFolder)
        {
            // Initialization
            Id = id;
            _packageManager = new PackageManager(packageBaseFolder);
            Logger = new Logger();
            _availableJobs = new ConcurrentQueue<JobWrapper>();
            _pendingJobs = new Dictionary<Guid, JobWrapper>();
            _finishedJobs = new ConcurrentQueue<JobWrapper>();
        }

        /// <summary>
        /// Initializes the handler and everything it needs to run
        /// </summary>
        public JobScriptInitializeResult InitializeAndStart(RunningHandlerInitializeParams runningHandlerInitializeParams)
        {
            // Initialization
            _jobScriptHash = runningHandlerInitializeParams.JobHash;
            var packageName = runningHandlerInitializeParams.PackageName;
            var currentPackageFolder = _packageManager.GetPackagePath(packageName);

            // Preparations
            var result = new JobScriptInitializeResult();

            // Assign the path to the output assembly
            var jobAssemblyPath = runningHandlerInitializeParams.JobAssemblyPath;

            // Read the settings
            IHandlerCustomSettings customSettings;
            JobFileHandlerSettingsReader.LoadAssemblyAndReadSettings(jobAssemblyPath, out _handlerSettings, out customSettings);

            // Read the package information object
            var packageInfo = _packageManager.GetInfo(packageName);

            // Search for the correct handler type in the given assemblies
            Type handlerType = null;
            foreach (var handlerAssemblyName in packageInfo.HandlerAssemblies)
            {
                // Load the assembly
                var handlerAssemblyPath = Path.Combine(currentPackageFolder, handlerAssemblyName);
                var handlerAssembly = AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(handlerAssemblyPath));

                // Try loading the from the assembly
                Type[] types;
                try
                {
                    types = handlerAssembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    result.SetError(AddJobScriptError.TypeException, ex.LoaderExceptions.First().Message);
                    return result;
                }

                // Search for the correct handler type
                foreach (var type in types)
                {
                    if (typeof(IHandler).IsAssignableFrom(type))
                    {
                        var att = type.GetCustomAttribute<HandlerNameAttribute>(true);
                        if (att != null)
                        {
                            if (att.HandlerName == _handlerSettings.HandlerName)
                            {
                                handlerType = type;
                                break;
                            }
                        }
                    }
                }
                if (handlerType != null) { break; }
            }

            if (handlerType == null)
            {
                result.SetError(AddJobScriptError.JobScriptMissing, String.Format("Handler type for handler '{0}' not found", _handlerSettings.HandlerName));
                return result;
            }
            // Initialize the handler
            var handlerInstance = (IHandler)Activator.CreateInstance(handlerType);
            // Initialize the handler with the custom settings
            handlerInstance.InitializeCustomSettings(customSettings);
            // Attach to the logger
            handlerInstance.Logger.LogEvent += (sender, args) => Logger.Log(args.LogLevel, args.Exception, args.Message);
            // Call the virtual initialize method
            handlerInstance.Initialize();
            // Event when a job was added
            handlerInstance.EnqueueJobEvent += EnqueueJob;
            // Assign the handler
            _handler = handlerInstance;

            // Start the control thread
            _controlTaskCancelToken = new CancellationTokenSource();
            _controlTask = new Task(ControlThread, _controlTaskCancelToken.Token);
            _controlTask.ContinueWith(t =>
            {
                Logger.Fatal(t.Exception, "Handler: '{0}' has exception", Id);
                TearDown(true);
                OnStateChangedEvent(new RunningHandlerStateChangedEventArgs(RunningHandlerState.Failed));
            }, TaskContinuationOptions.OnlyOnFaulted);
            _controlTask.Start();

            // Notify the start event
            _handler.OnStart();

            // Fill and return the info object
            result.HandlerId = Id;
            return result;
        }

        /// <summary>
        /// Replaces the job script assembly with a new one
        /// </summary>
        public void ReplaceJobScriptHash(string newJobHash)
        {
            _jobScriptHash = newJobHash;
        }

        /// <summary>
        /// Get information about this handler
        /// </summary>
        public LoadedHandlerStats GetInfo()
        {
            var stats = new LoadedHandlerStats
            {
                JobsAvailable = _availableJobs.Count,
                JobsPending = _pendingJobs.Count,
                TotalJobsProcessed = Interlocked.Read(ref _totalProcessedJobs),
                TotalJobsFailed = Interlocked.Read(ref _totalFailedJobs),
            };
            // Calculate the total job count
            stats.TotalJobsAvailable = _handler.GetTotalJobCount();
            if (stats.TotalJobsAvailable < 0)
            {
                // Set it to the current available jobs if it is unknown
                stats.TotalJobsAvailable = stats.JobsAvailable;
            }
            return stats;
        }

        /// <summary>
        /// Stops and cleans up the handler
        /// </summary>
        public bool TearDown(bool notifyStop)
        {
            // Notify the control task to stop
            _controlTaskCancelToken.Cancel();
            // Wait until the task is finished (but not when faulted)
            if (!_controlTask.IsFaulted)
            {
                _controlTask.Wait();
            }
            // Clear the various queues/lists/stats
            _availableJobs = new ConcurrentQueue<JobWrapper>();
            lock (_pendingJobs.GetSyncRoot())
            {
                _pendingJobs = new Dictionary<Guid, JobWrapper>();
            }
            _finishedJobs = new ConcurrentQueue<JobWrapper>();
            Interlocked.Exchange(ref _totalProcessedJobs, 0);
            Interlocked.Exchange(ref _totalFailedJobs, 0);
            // Signal the handler to stop
            if (notifyStop)
            {
                _handler.OnStop();
            }
            return true;
        }

        /// <summary>
        /// Gets the next job from the available queue
        /// </summary>
        public Job GetJob(Guid clientId)
        {
            JobWrapper assignedJob;
            var success = _availableJobs.TryDequeue(out assignedJob);
            if (success)
            {
                // Set the assigned values
                assignedJob.AssignedTime = DateTime.Now;
                assignedJob.AssignedCliendId = clientId;
                // Add it to pending jobs
                lock (_pendingJobs.GetSyncRoot())
                {
                    _pendingJobs[assignedJob.Id] = assignedJob;
                }
                // Check if more jobs are available
                if (_availableJobs.IsEmpty)
                {
                    // If not, set the waithandle to get new jobs
                    _jobsEmptyWaitHandle.Set();
                }
                return assignedJob.CreateJob(_jobScriptHash);
            }
            return null;
        }

        public bool ReceivedResult(JobResult result)
        {
            lock (_pendingJobs.GetSyncRoot())
            {
                // Get the job which is in progress
                var jobInProgress = _pendingJobs[result.JobId];
                // Check if the clientid mismatches
                if (jobInProgress.AssignedCliendId != result.ClientId)
                {
                    Logger.Warn("Got job '{0}' result for differet client ('{1}' instead '{2}')", result.JobId, result.ClientId, jobInProgress.AssignedCliendId);
                    return false;
                }

                // Check if there was an error processing the job
                if (result.HasError)
                {
                    Logger.Error("Got failed result for job '{0}': {1}", result.JobId, result.Error.ToString());
                    Interlocked.Increment(ref _totalFailedJobs);
                    // If so, remove it from the in-progress list
                    _pendingJobs.Remove(result.JobId);
                    // Reset the assigned values
                    jobInProgress.Reset();
                    // Add the job to the queue again
                    _availableJobs.Enqueue(jobInProgress);
                    return false;
                }

                var resultString = result.GetOutput();
                Logger.Info("Handler: Got result for job '{0}': {1}", result.JobId, resultString);
                Interlocked.Increment(ref _totalProcessedJobs);

                // Remove job from in-progress list
                _pendingJobs.Remove(result.JobId);
                // Set the result values
                jobInProgress.ResultTime = DateTime.Now;
                jobInProgress.ResultString = resultString;
                // Add it to the finished queue
                _finishedJobs.Enqueue(jobInProgress);
                _resultAvailableWaitHandle.Set();
            }
            return true;
        }

        /// <summary>
        /// Enqueues the given job
        /// </summary>
        private void EnqueueJob(IJobInput jobInput, object additionalData = null)
        {
            var jobWrapper = new JobWrapper
            {
                Id = Guid.NewGuid(),
                HandlerId = Id,
                JobInput = jobInput,
                EnqueueTime = DateTime.Now,
                AdditionalData = additionalData
            };
            _availableJobs.Enqueue(jobWrapper);
        }

        /// <summary>
        /// Control thread for this handler which is run while the handler exists
        /// - Refills the job-queue if needed
        /// - Checks for job timeouts and then resends the jobs
        /// - Collects and processes the results
        /// </summary>
        private void ControlThread()
        {
            // Initialize
            const int defaultSleep = 5000;

            // Control loop until the handler is being removed
            while (!_controlTaskCancelToken.IsCancellationRequested)
            {
                // Collect results
                JobWrapper finishedJob;
                while (_finishedJobs.TryDequeue(out finishedJob))
                {
                    Logger.Debug("Collecting finished job '{0}' with result '{1}'", finishedJob.Id, finishedJob.ResultString);
                    _handler.ProcessResult(finishedJob.JobInput, finishedJob.ResultString);
                }

                // Check for jobs with a timeout
                if (_handlerSettings.JobTimeout > 0)
                {
                    var now = DateTime.Now;
                    var jobsToRequeue = new List<JobWrapper>();
                    lock (_pendingJobs.GetSyncRoot())
                    {
                        foreach (var kvp in _pendingJobs)
                        {
                            if (now - kvp.Value.AssignedTime > TimeSpan.FromSeconds(_handlerSettings.JobTimeout))
                            {
                                // Job had a timeout
                                jobsToRequeue.Add(kvp.Value);
                            }
                        }
                        foreach (var job in jobsToRequeue)
                        {
                            Logger.Warn("Job '{0}' had a timeout", job.Id);
                            _pendingJobs.Remove(job.Id);
                            job.Reset();
                            _availableJobs.Enqueue(job);
                        }
                    }
                }

                // Refill available jobs if needed
                if (_availableJobs.IsEmpty)
                {
                    Logger.Debug("Job queue is empty, requesting new jobs");
                    // Fill with jobs
                    _handler.CreateMoreJobs();
                    Logger.Debug("Job queue contains now {0} job(s)", _availableJobs.Count);
                }

                // Stop if the handler was marked as finished
                if (_handler.IsFinished)
                {
                    Logger.Info("Handler: '{0}' finished successfully", Id);
                    lock (_lockObject)
                    {
                        _handler.OnFinished();
                        OnStateChangedEvent(new RunningHandlerStateChangedEventArgs(RunningHandlerState.Finished));
                        return;
                    }
                }

                // Sleep a little or until any of the various events was set
                WaitHandle.WaitAny(new[] { _controlTaskCancelToken.Token.WaitHandle, _jobsEmptyWaitHandle, _resultAvailableWaitHandle }, defaultSleep);
            }
        }

        protected virtual void OnStateChangedEvent(RunningHandlerStateChangedEventArgs e)
        {
            var handler = StateChangedEvent;
            if (handler != null) handler(this, e);
        }
    }
}
