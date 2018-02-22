using NCrontab;
using NetDist.Core;
using NetDist.Jobs;
using NetDist.Jobs.DataContracts;
using NetDist.Logging;
using NetDist.Server.XDomainObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NetDist.Server
{
    /// <summary>
    /// Instance object for a handler which is loaded
    /// </summary>
    public class HandlerInstance
    {
        /// <summary>
        /// Id of this handler
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Full name of the handler
        /// </summary>
        public string FullName { get { return Helpers.BuildFullName(JobScript.PackageName, HandlerSettings.HandlerName, HandlerSettings.JobName); } }

        /// <summary>
        /// Flag to indicate if this handler can deliver jobs
        /// </summary>
        public bool CanDeliverJob { get { return _handlerProxy != null && _handlerState == HandlerState.Running && _handlerProxy.HasAvailableJobs && (HandlerSettings.MaxParallelJobs == 0 || _handlerProxy.GetInfo().JobsPending < HandlerSettings.MaxParallelJobs); } }

        /// <summary>
        /// Time when the handler will start next time
        /// </summary>
        public DateTime? NextStartTime { get; private set; }

        /// <summary>
        /// Job script object used for this handler
        /// </summary>
        public JobScriptFile JobScript { get; private set; }

        /// <summary>
        /// Full path to the current job script assembly used by this handler
        /// </summary>
        public string JobScriptAssembly { get; private set; }

        /// <summary>
        /// Handler settings object for this handler instance
        /// </summary>
        public HandlerSettings HandlerSettings { get; private set; }

        private readonly object _lockObject = new object();
        private readonly PackageManager _packageManager;
        private readonly Logger _logger;
        private HandlerState _handlerState;
        private string _handlerMessage;
        private CrontabSchedule _cronSchedule;
        private IdleInformation _idleInfo;
        private AppDomain _appDomain;
        private RunningHandlerProxy _handlerProxy;
        private DateTime? _lastStartTime;
        private readonly Queue<LogInfoEntry> _log = new Queue<LogInfoEntry>();

        /// <summary>
        /// Constructor
        /// </summary>
        public HandlerInstance(Logger logger, PackageManager packageManager)
        {
            _logger = logger;
            _logger.LogEvent += LoggerOnLogEvent;

            _packageManager = packageManager;
            Id = Guid.NewGuid();
            _handlerState = HandlerState.Stopped;
        }

        private void LoggerOnLogEvent(object sender, LogEventArgs logEventArgs)
        {
            var logEntry = logEventArgs.LogEntry;

            // Skip debug / info log messages
            if (logEntry.HandlerId != Id || logEntry.LogLevel < LogLevel.Warn)
            {
                return;
            }

            var message = logEntry.GetMessageWithAdditionalInformation();
            if (logEntry.Exceptions.Count > 0)
            {
                message = String.Format("{0}\r\n  {1}\r\n{2}", message, logEntry.Exceptions[0].ExceptionMessage, logEntry.Exceptions[0].ExceptionStackTrace);
                if (logEntry.Exceptions.Count > 1)
                {
                    message = String.Format("{0}\r\n  {1}\r\n{2}", message, logEntry.Exceptions[1].ExceptionMessage, logEntry.Exceptions[1].ExceptionStackTrace);
                }
            }

            _log.Enqueue(new LogInfoEntry { Level = logEntry.LogLevel.ToString(), Timestamp = logEntry.LogDate, Message = message });
            if (_log.Count > 100)
            {
                _log.Dequeue();
            }
        }

        /// <summary>
        /// Initializes the instance from all necessary information
        /// </summary>
        public bool InitializeFromJobScript(JobScriptFile jobScriptFile, string outputAssembly, HandlerSettings handlerSettings)
        {
            // Check if any data changed at all
            if (JobScript != null && JobScript.Hash == jobScriptFile.Hash)
            {
                return false;
            }

            // Set the new values
            JobScript = jobScriptFile;
            JobScriptAssembly = outputAssembly;
            HandlerSettings = handlerSettings;

            // Notify the running handler of the new value
            if (_handlerProxy != null)
            {
                _handlerProxy.ReplaceJobScriptHash(JobScript.Hash);
            }

            // Initialize cron scheduler
            _cronSchedule = null;
            NextStartTime = null;
            if (!String.IsNullOrWhiteSpace(HandlerSettings.Schedule))
            {
                try
                {
                    _cronSchedule = CrontabSchedule.Parse(HandlerSettings.Schedule);
                    NextStartTime = _cronSchedule.GetNextOccurrence(DateTime.Now);
                }
                catch (Exception ex)
                {
                    _logger.Warn(entry => entry.SetHandlerInfo(Id, FullName), "Failed to parse crontab expression '{0}' => {1}", HandlerSettings.Schedule, ex.Message);
                }
            }
            // Initialize idle time
            _idleInfo = null;
            if (!String.IsNullOrWhiteSpace(HandlerSettings.IdleTime) && HandlerSettings.IdleTime.Contains("-"))
            {
                TimeSpan idleTimeStart;
                TimeSpan idleTimeEnd;
                var idleTimeParts = HandlerSettings.IdleTime.Split(new[] { "-" }, StringSplitOptions.RemoveEmptyEntries);
                var validFrom = TimeSpan.TryParse(idleTimeParts[0], out idleTimeStart);
                var validTo = TimeSpan.TryParse(idleTimeParts[1], out idleTimeEnd);
                if (validFrom && validTo)
                {
                    _idleInfo = new IdleInformation { Start = idleTimeStart, End = idleTimeEnd };
                }
            }
            return true;
        }

        public bool IsAllowedForClient(ClientInfo clientInfo)
        {
            // 1. Priority: Explicitely allowed
            if (HandlerSettings.ClientsAllowed.Count != 0)
            {
                // True if it is explicitely allowed
                foreach (var clientSelector in HandlerSettings.ClientsAllowed)
                {
                    if (clientSelector.Id != null && clientSelector.Id != Guid.Empty)
                    {
                        if (clientInfo.Id == clientSelector.Id)
                        {
                            return true;
                        }
                    }
                    if (!String.IsNullOrWhiteSpace(clientSelector.Name))
                    {
                        var regex = new Regex(String.Format("^{0}$", clientSelector.Name), RegexOptions.IgnoreCase);
                        if (regex.IsMatch(clientInfo.Name))
                        {
                            return true;
                        }
                    }
                }
                // False if not in the allowed list
                return false;
            }
            // 2. Priority: Explicitely denied
            if (HandlerSettings.ClientsDenied.Count != 0)
            {
                // False if it is explicitely denied
                foreach (var clientSelector in HandlerSettings.ClientsDenied)
                {
                    if (clientSelector.Id != null && clientSelector.Id != Guid.Empty)
                    {
                        if (clientInfo.Id == clientSelector.Id)
                        {
                            return false;
                        }
                    }
                    if (!String.IsNullOrWhiteSpace(clientSelector.Name))
                    {
                        var regex = new Regex(String.Format("^{0}$", clientSelector.Name), RegexOptions.IgnoreCase);
                        if (regex.IsMatch(clientInfo.Name))
                        {
                            return false;
                        }
                    }
                }
                // True if not in the denied list
                return true;
            }
            // 3. Priority: Neither allowed nor denied
            return true;
        }

        public HandlerInfo GetInfo()
        {
            var info = new HandlerInfo();
            if (_handlerProxy != null)
            {
                // Additional statistics about the running part of the handler
                var stats = _handlerProxy.GetInfo();
                info.JobsAvailable = stats.JobsAvailable;
                info.JobsPending = stats.JobsPending;
                info.TotalJobsAvailable = stats.TotalJobsAvailable;
                info.TotalJobsFailed = stats.TotalJobsFailed;
                info.TotalJobsProcessed = stats.TotalJobsProcessed;
            }
            // General information about the handler
            info.Id = Id;
            info.PackageName = JobScript.PackageName;
            info.HandlerName = HandlerSettings.HandlerName;
            info.JobName = HandlerSettings.JobName;
            info.HandlerState = _handlerState;
            info.HandlerMessage = _handlerMessage;
            info.LastStartTime = _lastStartTime;
            info.NextStartTime = NextStartTime;
            return info;
        }

        public LogInfo GetJobLog()
        {
            var result = new LogInfo { LogEntries = _log.OrderByDescending(i => i.Timestamp).ToList() };
            return result;
        }

        public bool Start()
        {
            lock (_lockObject)
            {
                // Do not start if handler is in failed state
                if (_handlerState == HandlerState.Failed)
                {
                    _logger.Error(entry => entry.SetHandlerInfo(Id, FullName), "Starting handler failed because he was in Failed state.");
                    return false;
                }
                // Do not start if handler is in disabled state
                if (_handlerState == HandlerState.Disabled)
                {
                    _logger.Error(entry => entry.SetHandlerInfo(Id, FullName), "Starting handler failed because he was in Disabled state.");
                    return false;
                }
                if (_handlerProxy == null)
                {
                    _appDomain = CreateDomain();
                    RunningHandlerProxy proxy;
                    var initResult = InitializeAndStartRunningHandler(out proxy);
                    if (initResult.HasError)
                    {
                        _logger.Error(entry => entry.SetHandlerInfo(Id, FullName), "Starting handler failed with reason '{0}' => {1}", initResult.ErrorReason, initResult.ErrorMessage);
                        SetFailed();
                        return false;
                    }
                    _handlerProxy = proxy;
                    _lastStartTime = DateTime.Now;
                    _handlerMessage = String.Empty;
                }
                _handlerState = HandlerState.Running;
            }
            return true;
        }

        public bool Stop()
        {
            lock (_lockObject)
            {
                _handlerState = HandlerState.Stopped;
                CleanupProxy(true);
                CleanupAppDomain();
            }
            return true;
        }

        public bool SetFinished()
        {
            lock (_lockObject)
            {
                _handlerState = HandlerState.Finished;
                CleanupProxy(false);
                CleanupAppDomain();
            }
            return true;
        }

        /// <summary>
        /// Tells the handler to keep running but without generating additional jobs
        /// </summary>
        public bool Pause()
        {
            lock (_lockObject)
            {
                if (_handlerState == HandlerState.Running || _handlerState == HandlerState.Idle)
                {
                    _handlerState = HandlerState.Paused;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Stops the handler and does not start it again
        /// </summary>
        public bool Disable()
        {
            lock (_lockObject)
            {
                Stop();
                _handlerState = HandlerState.Disabled;
            }
            return true;
        }

        /// <summary>
        /// Re-enables the handler so it could start again
        /// </summary>
        public bool Enable()
        {
            lock (_lockObject)
            {
                if (_handlerState == HandlerState.Disabled)
                {
                    _handlerState = HandlerState.Stopped;
                    return true;
                }
            }
            return false;
        }

        public Job GetJob(Guid clientId)
        {
            return _handlerProxy.GetJob(clientId);
        }

        public bool ReceivedResult(JobResult result, ExtendedClientInfo clientInfo)
        {
            // Catch case where we receive results for an already stopped or disabled handler
            if (_handlerState == HandlerState.Stopped || _handlerState == HandlerState.Disabled)
            {
                _logger.Warn(entry => entry.SetHandlerInfo(Id, FullName).SetClientInfo(clientInfo.ClientInfo.Id, clientInfo.ClientInfo.Name), "Got job result for stopped or disabled handler.");
                return false;
            }
            // Catch case where the RunningHandlerProxy is already null
            if (_handlerProxy == null)
            {
                _logger.Warn(entry => entry.SetHandlerInfo(Id, FullName).SetClientInfo(clientInfo.ClientInfo.Id, clientInfo.ClientInfo.Name), "Got job result for inexisting RunningHandlerProxy.");
                return false;
            }
            var success = _handlerProxy.ReceivedResult(result, clientInfo);
            if (!success && HandlerSettings.MaxSequencedErrors > 0 && _handlerProxy.GetInfo().SequencedJobsFailed > HandlerSettings.MaxSequencedErrors)
            {
                _handlerMessage = "Too many sequenced errors";
                SetFailed();
            }
            return success;
        }

        private void SetFailed()
        {
            Stop();
            NextStartTime = null;
            _handlerState = HandlerState.Failed;
        }

        private AppDomain CreateDomain()
        {
            // Create an additional app-domain
            var domain = AppDomain.CreateDomain(Guid.NewGuid().ToString(), null, new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
                ApplicationName = AppDomain.CurrentDomain.SetupInformation.ApplicationName,
                LoaderOptimization = LoaderOptimization.MultiDomainHost,
                ShadowCopyFiles = "true",
                AppDomainInitializerArguments = null
            });
            return domain;
        }

        private JobScriptInitializeResult InitializeAndStartRunningHandler(out RunningHandlerProxy handlerProxy)
        {
            // Create a proxy for the handler
            handlerProxy = (RunningHandlerProxy)_appDomain.CreateInstanceAndUnwrap(typeof(RunningHandlerProxy).Assembly.FullName, typeof(RunningHandlerProxy).FullName, false, BindingFlags.Default, null, new object[] { Id, _packageManager.PackageBaseFolder }, null, null);
            // Create a interchangeable event sink to register cross-domain events to catch logging events
            var sink = new EventSink<LogEventArgs>();
            handlerProxy.RegisterLogEventSink(sink);
            sink.NotificationFired += (sender, args) => _logger.Log(args.LogEntry.SetHandlerInfo(Id, FullName));
            // Register event sink to listen on state changes
            var stateSink = new EventSink<RunningHandlerStateChangedEventArgs>();
            handlerProxy.RegisterStateChangedEventSink(stateSink);
            stateSink.NotificationFired += (sender, args) =>
            {
                switch (args.State)
                {
                    case RunningHandlerState.Failed:
                        SetFailed();
                        break;
                    case RunningHandlerState.Finished:
                        SetFinished();
                        break;
                }
            };
            // Initialize the handler
            var initParams = new RunningHandlerInitializeParams
            {
                HandlerSettings = HandlerSettings,
                JobHash = JobScript.Hash,
                PackageName = JobScript.PackageName,
                JobAssemblyPath = JobScriptAssembly
            };
            var initResult = handlerProxy.InitializeAndStart(initParams);
            return initResult;
        }

        private void CleanupProxy(bool notifyStop)
        {
            if (_handlerProxy != null)
            {
                _handlerProxy.TearDown(notifyStop);
                _handlerProxy = null;
            }
        }

        private void CleanupAppDomain()
        {
            if (_appDomain != null)
            {
                try
                {
                    AppDomain.Unload(_appDomain);
                }
                catch (AppDomainUnloadedException)
                {
                    _logger.Debug(entry => entry.SetHandlerInfo(Id, FullName), "AppDomain already unloaded.");
                }
                _appDomain = null;
            }
        }

        /// <summary>
        /// Checks if the handler needs a scheduled start or if it should re-schedule the next start
        /// </summary>
        public void ScheduledStartOrReschedule()
        {
            // Do not start again if handler was failed
            if (_handlerState == HandlerState.Failed)
            {
                return;
            }
            // Only perform this if there is a cron scheduler
            if (_cronSchedule != null)
            {
                // Check handler states which allow a restart
                if (_handlerState == HandlerState.Finished || _handlerState == HandlerState.Stopped || _handlerState == HandlerState.Failed)
                {
                    // Check if it should start according to the schedule
                    if (NextStartTime < DateTime.Now)
                    {
                        Start();
                        NextStartTime = _cronSchedule.GetNextOccurrence(DateTime.Now);
                    }
                }
                else
                {
                    // Handler is running, calculate the next start date from the current time
                    // This prevents the handler from immediately starting after it was stopped or finished after the initial next start time
                    NextStartTime = _cronSchedule.GetNextOccurrence(DateTime.Now);
                }
            }
        }

        /// <summary>
        /// Checks for idle time and sets the status appropriately
        /// </summary>
        public void CheckIdle()
        {
            bool isInIdleTime = false;
            if (_idleInfo != null)
            {
                // Set to idle if needed
                var now = DateTime.Now.TimeOfDay;
                if (_idleInfo.Start < _idleInfo.End)
                {
                    if (now >= _idleInfo.Start && now <= _idleInfo.End) { isInIdleTime = true; }
                }
                else
                {
                    if (now >= _idleInfo.Start || now <= _idleInfo.End) { isInIdleTime = true; }
                }
            }

            lock (_lockObject)
            {
                // Check if we're outside the idletime but the handler is still idle
                if (!isInIdleTime && _handlerState == HandlerState.Idle)
                {
                    // Set it to running
                    _handlerState = HandlerState.Running;
                }

                // We're inside the idle time but the handler is still running
                if (isInIdleTime && _handlerState == HandlerState.Running)
                {
                    // Set it to idle
                    _handlerState = HandlerState.Idle;
                }
            }
        }

        /// <summary>
        /// Helper class for idle time
        /// </summary>
        private class IdleInformation
        {
            public TimeSpan Start { get; set; }
            public TimeSpan End { get; set; }
        }
    }
}
