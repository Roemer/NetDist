using NCrontab;
using NetDist.Core;
using NetDist.Jobs;
using NetDist.Logging;
using NetDist.Server.XDomainObjects;
using System;
using System.Reflection;

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
        public bool CanDeliverJob { get { return HandlerProxy != null && _handlerState == HandlerState.Running && HandlerProxy.HasAvailableJobs; } }

        /// <summary>
        /// Time when the handler was last started
        /// </summary>
        private DateTime? LastStartTime { get; set; }

        /// <summary>
        /// Time when the handler will start next time
        /// </summary>
        public DateTime? NextStartTime { get; set; }

        /// <summary>
        /// Job script object used for this handler
        /// </summary>
        public JobScriptFile JobScript { get; set; }

        /// <summary>
        /// Full path to the current job script assembly used by this handler
        /// </summary>
        public string JobScriptAssembly { get; set; }

        /// <summary>
        /// Handler settings object for this handler instance
        /// </summary>
        public HandlerSettings HandlerSettings { get; private set; }

        /// <summary>
        /// The app domain object where this handler is running
        /// </summary>
        public AppDomain AppDomain { get; set; }

        /// <summary>
        /// Proxy object to communicate with this handler
        /// </summary>
        public LoadedHandlerProxy HandlerProxy { get; set; }

        private readonly object _lockObject = new object();
        private readonly PackageManager _packageManager;
        private readonly Logger _logger;
        private HandlerState _handlerState;
        private CrontabSchedule _cronSchedule;
        private IdleInformation _idleInfo;

        /// <summary>
        /// Constructor
        /// </summary>
        public HandlerInstance(Logger logger, PackageManager packageManager)
        {
            _logger = logger;
            _packageManager = packageManager;
            Id = Guid.NewGuid();
            _handlerState = HandlerState.Stopped;
        }

        public void UpdateSettings(HandlerSettings handlerSettings)
        {
            HandlerSettings = handlerSettings;
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
                    _logger.Warn("Failed to parse Crontab: '{0}' - Ex: {1}", HandlerSettings.Schedule, ex.Message);
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
        }

        public HandlerInfo GetInfo()
        {
            var info = new HandlerInfo();
            if (HandlerProxy != null)
            {
                // Additional statistics about the running part of the handler
                var stats = HandlerProxy.GetInfo();
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
            info.LastStartTime = LastStartTime;
            info.NextStartTime = NextStartTime;
            return info;
        }

        public bool Start()
        {
            lock (_lockObject)
            {
                if (HandlerProxy == null)
                {
                    AppDomain = CreateDomain();
                    LoadedHandlerProxy proxy;
                    var initResult = InitializeProxy(out proxy);
                    if (initResult.HasError)
                    {
                        _logger.Error("Handler init failed: {0}: {1}", initResult.ErrorReason, initResult.ErrorMessage);
                        SetFailed();
                        return false;
                    }
                    HandlerProxy = proxy;
                    LastStartTime = DateTime.Now;
                }
                HandlerProxy.Start();
                _handlerState = HandlerState.Running;
            }
            return true;
        }

        public bool Stop()
        {
            lock (_lockObject)
            {
                if (HandlerProxy != null)
                {
                    HandlerProxy.Stop();
                    HandlerProxy = null;
                }
                if (AppDomain != null)
                {
                    AppDomain.Unload(AppDomain);
                    AppDomain = null;
                }
                _handlerState = HandlerState.Stopped;
            }
            return true;
        }

        private void SetFailed()
        {
            Stop();
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

        private JobScriptInitializeResult InitializeProxy(out LoadedHandlerProxy handlerProxy)
        {
            // Create a proxy for the handler
            handlerProxy = (LoadedHandlerProxy)AppDomain.CreateInstanceAndUnwrap(typeof(LoadedHandlerProxy).Assembly.FullName, typeof(LoadedHandlerProxy).FullName, false, BindingFlags.Default, null, new object[] { Id, _packageManager.PackageBaseFolder }, null, null);
            // Create a interchangeable event sink to register cross-domain events to catch logging events
            var sink = new EventSink<LogEventArgs>();
            handlerProxy.RegisterLogEventSink(sink);
            sink.NotificationFired += (sender, args) => _logger.Log(args.LogLevel, args.Exception, args.Message);
            // Initialize the handler
            var initParams = new LoadedHandlerInitializeParams
            {
                HandlerSettings = HandlerSettings,
                JobScriptFile = JobScript,
                JobAssemblyPath = JobScriptAssembly
            };
            var initResult = handlerProxy.Initialize(initParams);
            return initResult;
        }

        /// <summary>
        /// Checks if the handler needs a scheduled start or if it should re-schedule the next start
        /// </summary>
        public void ScheduledStartOrReschedule()
        {
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
