using Microsoft.CSharp;
using NetDist.Core;
using NetDist.Core.Extensions;
using NetDist.Core.Utilities;
using NetDist.Handlers;
using NetDist.Jobs;
using NetDist.Logging;
using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetDist.Server.XDomainObjects;

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
        /// Flag to check if there are available jobs
        /// </summary>
        public bool HasAvailableJobs
        {
            get { return HandlerState == HandlerState.Running && !AvailableJobs.IsEmpty; }
        }

        /// <summary>
        /// Queue for the available jobs
        /// </summary>
        protected ConcurrentQueue<JobWrapper> AvailableJobs;

        /// <summary>
        /// List for jobs which are in progress
        /// </summary>
        protected Dictionary<Guid, JobWrapper> PendingJobs;

        /// <summary>
        /// List for jobs which are finished and waiting to be collected
        /// </summary>
        protected ConcurrentQueue<JobWrapper> FinishedJobs;

        /// <summary>
        /// Full name of the handler: PluginName/HandlerName/JobName
        /// </summary>
        public string FullName
        {
            get { return String.Format("{0}/{1}/{2}", _jobScriptFile.PackageName, HandlerSettings.HandlerName, HandlerSettings.JobName); }
        }

        /// <summary>
        /// Instance of the effective handler
        /// </summary>
        private IHandler _handler;

        private long _totalProcessedJobs;
        private long _totalFailedJobs;

        /// <summary>
        /// Object used for stuff that should be thread-safe
        /// </summary>
        private readonly object _lockObject = new object();

        private readonly JobScriptFile _jobScriptFile;
        private readonly string _currentPackageFolder;
        private Task _controlTask;
        private CancellationTokenSource _controlTaskCancelToken = new CancellationTokenSource();
        private readonly AutoResetEvent _jobsEmptyWaitHandle = new AutoResetEvent(false);
        private readonly AutoResetEvent _resultAvailableWaitHandle = new AutoResetEvent(false);

        /// <summary>
        /// Constructor
        /// </summary>
        public LoadedHandler(JobScriptFile jobScriptFile, string packageBaseFolder)
        {
            Id = Guid.NewGuid();
            _jobScriptFile = jobScriptFile;
            _currentPackageFolder = Path.Combine(packageBaseFolder, jobScriptFile.PackageName);
            Logger = new Logger();
            AvailableJobs = new ConcurrentQueue<JobWrapper>();
            PendingJobs = new Dictionary<Guid, JobWrapper>();
            FinishedJobs = new ConcurrentQueue<JobWrapper>();
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
        /// Initializes the handler and everything it needs to run
        /// </summary>
        public JobHandlerInitializeResult Initialize()
        {
            // Preparations
            var result = new JobHandlerInitializeResult
            {
                PackageName = _jobScriptFile.PackageName
            };

            // Prepare compiler
            var codeProvider = new CSharpCodeProvider();
            var options = new CompilerParameters
            {
                GenerateInMemory = false,
                OutputAssembly = Path.Combine(_currentPackageFolder, String.Format("_job_{0}.dll", HashCalculator.CalculateMd5Hash(_jobScriptFile.JobScript))),
                IncludeDebugInformation = true,
                CompilerOptions = String.Format("/lib:{0}", _currentPackageFolder)
            };
            // Add libraries
            foreach (var library in _jobScriptFile.CompilerLibraries)
            {
                options.ReferencedAssemblies.Add(library);
            }
            // Compile it
            var compilerResults = codeProvider.CompileAssemblyFromSource(options, _jobScriptFile.JobScript);
            if (compilerResults.Errors.HasErrors)
            {
                var sb = new StringBuilder();
                // Don't add output for now
                //sb.AppendLine("Output:");
                //for (int i = 0; i < compilerResults.Output.Count; i++)
                //{
                //    sb.AppendLine(compilerResults.Output[i]);
                //}
                sb.AppendLine("Errors:");
                for (int i = 0; i < compilerResults.Errors.Count; i++)
                {
                    sb.AppendFormat("{0}: {1}", i, compilerResults.Errors[i]).AppendLine();
                }
                var errorString = sb.ToString();
                Logger.Error("Failed to compile job script: {0}", errorString);
                // Fill result object
                result.SetError(AddJobHandlerErrorReason.CompilationFailed, errorString);
                return result;
            }

            // Instantiate the job to get out the settings
            var jobAssembly = AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(compilerResults.PathToAssembly));
            // Search for the initializer
            Type jobInitializerType = null;
            foreach (var type in jobAssembly.GetTypes())
            {
                if (typeof(IJobHandlerInitializer).IsAssignableFrom(type))
                {
                    jobInitializerType = type;
                    break;
                }
            }
            if (jobInitializerType == null)
            {
                result.SetError(AddJobHandlerErrorReason.JobInitializerMissing, "Job initializer type not found");
                return result;
            }
            // Initialize the job
            var jobInstance = (IJobHandlerInitializer)Activator.CreateInstance(jobInitializerType);
            // Read the settings
            HandlerSettings = jobInstance.GetHandlerSettings();
            var customSettings = jobInstance.GetCustomHandlerSettings();

            // Add new information
            result.HandlerName = HandlerSettings.HandlerName;
            result.JobName = HandlerSettings.JobName;

            // Initialize the handler
            var pluginPath = Path.Combine(_currentPackageFolder, String.Format("{0}.dll", _jobScriptFile.PackageName));
            var handlerAssembly = AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(pluginPath));

            Type typeToLoad = null;
            foreach (var type in handlerAssembly.GetTypes())
            {
                if (typeof(IHandler).IsAssignableFrom(type))
                {
                    var att = type.GetCustomAttribute<HandlerNameAttribute>(true);
                    if (att != null)
                    {
                        if (att.HandlerName == HandlerSettings.HandlerName)
                        {
                            typeToLoad = type;
                            break;
                        }
                    }
                }
            }
            if (typeToLoad == null)
            {
                result.SetError(AddJobHandlerErrorReason.JobHandlerMissing, String.Format("Handler type for handler '{0}' not found", HandlerSettings.HandlerName));
                return result;
            }
            var handlerInstance = (IHandler)Activator.CreateInstance(typeToLoad);
            // Initialize the handler with the custom settings
            handlerInstance.InitializeCustomSettings(customSettings);
            // Call the virtual initialize method
            handlerInstance.Initialize();
            // Assign the handler
            _handler = handlerInstance;
            // Fill and return the info object
            result.HandlerId = Id;
            return result;
        }

        /// <summary>
        /// Get information about this handler
        /// </summary>
        public HandlerInfo GetInfo()
        {
            var hInfo = new HandlerInfo
            {
                Id = Id,
                PluginName = _jobScriptFile.PackageName,
                HandlerName = HandlerSettings.HandlerName,
                JobName = HandlerSettings.JobName,
                TotalJobsAvailable = 0, // TODO
                JobsAvailable = AvailableJobs.Count,
                JobsPending = PendingJobs.Count,
                TotalJobsProcessed = Interlocked.Read(ref _totalProcessedJobs),
                TotalJobsFailed = Interlocked.Read(ref _totalFailedJobs),
                HandlerState = HandlerState
            };
            return hInfo;
        }

        /// <summary>
        /// Starts the job handler so jobs are generated and processed
        /// </summary>
        public void StartJobHandler()
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
                        StopJobHandler();
                    }, TaskContinuationOptions.OnlyOnFaulted);
                    HandlerState = HandlerState.Running;
                    _controlTask.Start();
                }
            }
        }

        /// <summary>
        /// Stops the job handler
        /// </summary>
        public bool StopJobHandler()
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
                    // Clear the various queues/lists/stats
                    AvailableJobs = new ConcurrentQueue<JobWrapper>();
                    lock (PendingJobs.GetSyncRoot())
                    {
                        PendingJobs = new Dictionary<Guid, JobWrapper>();
                    }
                    FinishedJobs = new ConcurrentQueue<JobWrapper>();
                    Interlocked.Exchange(ref _totalProcessedJobs, 0);
                    Interlocked.Exchange(ref _totalFailedJobs, 0);
                    // Signal the handler to stop
                    _handler.OnStop();
                    HandlerState = HandlerState.Stopped;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the next job from the available queue
        /// </summary>
        public Job GetNextJob(Guid clientId)
        {
            JobWrapper assignedJob;
            var success = AvailableJobs.TryDequeue(out assignedJob);
            if (success)
            {
                // Set the assigned values
                assignedJob.AssignedTime = DateTime.Now;
                assignedJob.AssignedCliendId = clientId;
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
                return assignedJob.Job;
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
                    Logger.Warn("Got job '{0}' result for stopped handler", result.JobId);
                    return;
                }

                lock (PendingJobs.GetSyncRoot())
                {
                    // Get the job which is in progress
                    var jobInProgress = PendingJobs[result.JobId];
                    // Check if the clientid mismatches
                    if (jobInProgress.AssignedCliendId != result.ClientId)
                    {
                        Logger.Warn("Got job '{0}' result for differet client ('{1}' instead '{2}')", result.JobId, result.ClientId, jobInProgress.AssignedCliendId);
                        return;
                    }

                    // Check if there was an error processing the job
                    if (result.HasError)
                    {
                        Logger.Error("Got failed result for job '{0}': {1}", result.JobId, result.Error.ToString());
                        Interlocked.Increment(ref _totalFailedJobs);
                        // If so, remove it from the in-progress list
                        PendingJobs.Remove(result.JobId);
                        // Reset the assigned values
                        jobInProgress.Reset();
                        // Add the job to the queue again
                        AvailableJobs.Enqueue(jobInProgress);
                        return;
                    }

                    Logger.Info("Got result for job '{0}': {1}", result.JobId, result.JobOutputString);
                    Interlocked.Increment(ref _totalProcessedJobs);

                    // Remove job from in-progress list
                    PendingJobs.Remove(result.JobId);
                    // Set the result values
                    jobInProgress.ResultTime = DateTime.Now;
                    jobInProgress.ResultString = result.JobOutputString;
                    // Add it to the finished queue
                    FinishedJobs.Enqueue(jobInProgress);
                    _resultAvailableWaitHandle.Set();
                }
            });
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
                JobWrapper finishedJob;
                while (FinishedJobs.TryDequeue(out finishedJob))
                {
                    Logger.Debug("Collecting finished job '{0}' with result '{1}'", finishedJob.Job.Id, finishedJob.ResultString);
                    _handler.ProcessResult(finishedJob.Job.JobInput, finishedJob.ResultString);
                }

                // Check for jobs with a timeout
                if (HandlerSettings.JobTimeout > 0)
                {
                    var now = DateTime.Now;
                    var jobsToRequeue = new List<JobWrapper>();
                    lock (PendingJobs.GetSyncRoot())
                    {
                        foreach (var kvp in PendingJobs)
                        {
                            if (now - kvp.Value.AssignedTime > TimeSpan.FromSeconds(HandlerSettings.JobTimeout))
                            {
                                // Job had a timeout
                                jobsToRequeue.Add(kvp.Value);
                            }
                        }
                        foreach (var job in jobsToRequeue)
                        {
                            Logger.Warn("Job '{0}' had a timeout", job.Job.Id);
                            PendingJobs.Remove(job.Job.Id);
                            job.Reset();
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
                        var jobWrapper = new JobWrapper
                        {
                            Job = job,
                            EnqueueTime = DateTime.Now
                        };
                        AvailableJobs.Enqueue(jobWrapper);
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
