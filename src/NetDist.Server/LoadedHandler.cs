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
    public class LoadedHandler
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
        /// Current state of the handler
        /// </summary>
        public HandlerState HandlerState { get; private set; }

        /// <summary>
        /// Flag to check if there are available jobs
        /// </summary>
        public bool HasAvailableJobs
        {
            get { return HandlerState == HandlerState.Running && !_availableJobs.IsEmpty; }
        }

        /// <summary>
        /// Instance of the effective handler
        /// </summary>
        private IHandler _handler;

        /// <summary>
        /// Instance of handler settings
        /// </summary>
        private HandlerSettings _handlerSettings;

        /// <summary>
        /// Queue for the available jobs
        /// </summary>
        private ConcurrentQueue<JobWrapper> _availableJobs;

        /// <summary>
        /// List for jobs which are in progress
        /// </summary>
        private Dictionary<Guid, JobWrapper> _pendingJobs;

        /// <summary>
        /// List for jobs which are finished and waiting to be collected
        /// </summary>
        private ConcurrentQueue<JobWrapper> _finishedJobs;

        private long _totalProcessedJobs;
        private long _totalFailedJobs;

        /// <summary>
        /// Object used for stuff that should be thread-safe
        /// </summary>
        private readonly object _lockObject = new object();

        private readonly PackageManager _packageManager;
        private JobScriptFile _jobScriptFile;
        private string _jobAssemblyPath;
        private Task _controlTask;
        private CancellationTokenSource _controlTaskCancelToken = new CancellationTokenSource();
        private readonly AutoResetEvent _jobsEmptyWaitHandle = new AutoResetEvent(false);
        private readonly AutoResetEvent _resultAvailableWaitHandle = new AutoResetEvent(false);
        private readonly AutoResetEvent _pauseWaitHandle = new AutoResetEvent(false);
        private readonly AutoResetEvent _disabledWaitHandle = new AutoResetEvent(false);

        /// <summary>
        /// Constructor
        /// </summary>
        public LoadedHandler(Guid id, string packageBaseFolder)
        {
            // Initialization
            Id = id;
            _packageManager = new PackageManager(packageBaseFolder);
            Logger = new Logger();
            _availableJobs = new ConcurrentQueue<JobWrapper>();
            _pendingJobs = new Dictionary<Guid, JobWrapper>();
            _finishedJobs = new ConcurrentQueue<JobWrapper>();
            HandlerState = HandlerState.Stopped;
        }

        /// <summary>
        /// Initializes the handler and everything it needs to run
        /// </summary>
        public JobScriptInitializeResult Initialize(LoadedHandlerInitializeParams loadedHandlerInitializeParams)
        {
            // Initialization
            _jobScriptFile = loadedHandlerInitializeParams.JobScriptFile;
            var currentPackageFolder = _packageManager.GetPackagePath(_jobScriptFile.PackageName);

            // Preparations
            var result = new JobScriptInitializeResult();

            // Assign the path to the output assembly
            _jobAssemblyPath = loadedHandlerInitializeParams.JobAssemblyPath;

            // Read the settings
            IHandlerCustomSettings customSettings;
            JobFileHandlerSettingsReader.LoadAssemblyAndReadSettings(_jobAssemblyPath, out _handlerSettings, out customSettings);

            // Read the package information object
            var packageInfo = _packageManager.GetInfo(_jobScriptFile.PackageName);

            // Initialize the handler
            // TODO: Search in more than just the first assembly
            var handlerAssemblyPath = Path.Combine(currentPackageFolder, packageInfo.HandlerAssemblies[0]);
            var handlerAssembly = AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(handlerAssemblyPath));

            // Try loading the types
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

            // Search for the correct handler
            Type typeToLoad = null;
            foreach (var type in types)
            {
                if (typeof(IHandler).IsAssignableFrom(type))
                {
                    var att = type.GetCustomAttribute<HandlerNameAttribute>(true);
                    if (att != null)
                    {
                        if (att.HandlerName == _handlerSettings.HandlerName)
                        {
                            typeToLoad = type;
                            break;
                        }
                    }
                }
            }
            if (typeToLoad == null)
            {
                result.SetError(AddJobScriptError.JobScriptMissing, String.Format("Handler type for handler '{0}' not found", _handlerSettings.HandlerName));
                return result;
            }
            var handlerInstance = (IHandler)Activator.CreateInstance(typeToLoad);
            // Initialize the handler with the custom settings
            handlerInstance.InitializeCustomSettings(customSettings);
            // Attach to the logger
            handlerInstance.Logger.LogEvent += (sender, args) => Logger.Log(args.LogLevel, args.Exception, args.Message);
            // Call the virtual initialize method
            handlerInstance.Initialize();
            // Event when a job was added
            handlerInstance.EnqueueJobEvent += EnqueueJob;
            // Initial state is stopped
            HandlerState = HandlerState.Stopped;
            // Assign the handler
            _handler = handlerInstance;

            // Start the control thread
            _controlTaskCancelToken = new CancellationTokenSource();
            _controlTask = new Task(ControlThread, _controlTaskCancelToken.Token);
            _controlTask.ContinueWith(t =>
            {
                Logger.Fatal(t.Exception, "Exception in handler '{0}'", Id);
                Stop();
            }, TaskContinuationOptions.OnlyOnFaulted);
            _controlTask.Start();

            // Fill and return the info object
            result.HandlerId = Id;
            return result;
        }

        /// <summary>
        /// Replaces the job script assembly with a new one
        /// </summary>
        public bool ReplaceJobScript(JobScriptFile jobScriptFile, string newJobAssemblyPath)
        {
            // Don't replace it if it is the same as already registered
            if (newJobAssemblyPath == _jobAssemblyPath)
            {
                return false;
            }
            _jobScriptFile = jobScriptFile;
            _jobAssemblyPath = newJobAssemblyPath;
            return true;
        }

        /// <summary>
        /// Clear all resources
        /// </summary>
        public bool TearDown()
        {
            Disable();
            if (_controlTask != null)
            {
                // Notify the control task to stop
                _controlTaskCancelToken.Cancel();
                // Wait until the task is finished (but not when faulted)
                if (!_controlTask.IsFaulted)
                {
                    _controlTask.Wait();
                }
            }
            return true;
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
        /// Starts the job handler so jobs are generated and processed
        /// </summary>
        public bool Start()
        {
            lock (_lockObject)
            {
                if (HandlerState == HandlerState.Paused)
                {
                    // Continue processing
                    HandlerState = HandlerState.Running;
                    _pauseWaitHandle.Set();
                    return true;
                }
                // Start if
                if (HandlerState == HandlerState.Stopped || HandlerState == HandlerState.Finished)
                {
                    // Notify the handler that it has started
                    _handler.OnStart();
                    HandlerState = HandlerState.Running;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Stops the job handler
        /// </summary>
        public bool Stop()
        {
            lock (_lockObject)
            {
                if (HandlerState == HandlerState.Running || HandlerState == HandlerState.Paused)
                {
                    // Set the state to stopped
                    HandlerState = HandlerState.Stopped;
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
                    _handler.OnStop();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Set the handler state to paused
        /// </summary>
        public bool Pause()
        {
            lock (_lockObject)
            {
                if (HandlerState == HandlerState.Running || HandlerState == HandlerState.Idle)
                {
                    HandlerState = HandlerState.Paused;
                    _pauseWaitHandle.Reset();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Stop and disable processing
        /// </summary>
        public bool Disable()
        {
            lock (_lockObject)
            {
                Stop();
                HandlerState = HandlerState.Disabled;
            }
            return true;
        }

        /// <summary>
        /// Re-enable processing
        /// </summary>
        public bool Enable()
        {
            lock (_lockObject)
            {
                if (HandlerState == HandlerState.Disabled)
                {
                    HandlerState = HandlerState.Stopped;
                    _disabledWaitHandle.Set();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the next job from the available queue
        /// </summary>
        public Job GetJob(Guid clientId)
        {
            // Don't send out new jobs when not running
            if (HandlerState != HandlerState.Running)
            {
                return null;
            }

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
                return assignedJob.CreateJob(_jobScriptFile.Hash);
            }
            return null;
        }

        public bool ReceivedResult(JobResult result)
        {
            // Catch case where we receive results for an already stopped handler
            if (HandlerState == HandlerState.Stopped)
            {
                Logger.Warn("Got job '{0}' result for stopped handler", result.JobId);
                return false;
            }

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
                Logger.Info("Got result for job '{0}': {1}", result.JobId, resultString);
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
        /// - Controls various timings (idle-time, disabled, ...)
        /// </summary>
        private void ControlThread()
        {
            // Initialize
            const int defaultSleep = 5000;

            // Control loop until the handler is being removed
            while (!_controlTaskCancelToken.IsCancellationRequested)
            {
                // Block until re-enabled
                if (HandlerState == HandlerState.Disabled)
                {
                    _disabledWaitHandle.WaitOne();
                }

                // Block until unpaused
                if (HandlerState == HandlerState.Paused)
                {
                    _pauseWaitHandle.WaitOne();
                }

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
                    Logger.Info("Handler '{0}' finished successfully", Id);
                    lock (_lockObject)
                    {
                        _handler.OnFinished();
                        HandlerState = HandlerState.Finished;
                    }
                }

                // Sleep a little or until any of the various events was set
                WaitHandle.WaitAny(new[] { _controlTaskCancelToken.Token.WaitHandle, _jobsEmptyWaitHandle, _resultAvailableWaitHandle }, defaultSleep);
            }
        }
    }
}
