using NetDist.Core;
using NetDist.Jobs.DataContracts;
using NetDist.Logging;
using System;

namespace NetDist.Server.XDomainObjects
{
    /// <summary>
    /// Cross-domain proxy object for a loaded handler
    /// </summary>
    public class LoadedHandlerProxy : MarshalByRefObject
    {
        /// <summary>
        /// Member for the contcrete loaded handler
        /// </summary>
        private readonly LoadedHandler _loadedHandler;

        /// <summary>
        /// The id of the handler
        /// </summary>
        public Guid Id { get { return _loadedHandler.Id; } }

        /// <summary>
        /// Package name of the package of this handler
        /// </summary>
        public string PackageName { get { return _loadedHandler.PackageName; } }

        /// <summary>
        /// The full name of the handler
        /// </summary>
        public string FullName { get { return _loadedHandler.FullName; } }

        /// <summary>
        /// Flag to indicate if the handler has available jobs
        /// </summary>
        public bool HasAvailableJobs { get { return _loadedHandler.HasAvailableJobs; } }

        /// <summary>
        /// Constructor
        /// </summary>
        public LoadedHandlerProxy(string packageBaseFolder, JobScriptFile jobScriptFile)
        {
            _loadedHandler = new LoadedHandler(packageBaseFolder, jobScriptFile);
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
        /// Initialize the handler according all the information
        /// </summary>
        public JobScriptInitializeResult Initialize()
        {
            return _loadedHandler.Initialize();
        }

        /// <summary>
        /// Tells the handler to start generating and processing jobs
        /// </summary>
        public bool Start()
        {
            return _loadedHandler.Start();
        }

        /// <summary>
        /// Tells the handler to stop generating and processing jobs
        /// </summary>
        public bool Stop()
        {
            return _loadedHandler.Stop();
        }

        /// <summary>
        /// Tells the handler to keep running but without generating additional jobs
        /// </summary>
        public bool Pause()
        {
            return _loadedHandler.Pause();
        }

        /// <summary>
        /// Tells the handler to stop and not start again until re-enabled
        /// </summary>
        public bool Disable()
        {
            return _loadedHandler.Disable();
        }

        /// <summary>
        /// Re-enables the handler
        /// </summary>
        public bool Enable()
        {
            return _loadedHandler.Enable();
        }

        /// <summary>
        /// Stops the handler and cleans all resources
        /// </summary>
        public bool TearDown()
        {
            return _loadedHandler.TearDown();
        }

        /// <summary>
        /// Gets information about this handler
        /// </summary>
        public HandlerInfo GetInfo()
        {
            return _loadedHandler.GetInfo();
        }

        /// <summary>
        /// Gets an available job from the handler
        /// </summary>
        public Job GetJob(Guid clientId)
        {
            return _loadedHandler.GetJob(clientId);
        }

        /// <summary>
        /// Gets all relevant information a client needs to execute the job for this handler
        /// </summary>
        public HandlerJobInfo GetJobInfo()
        {
            return _loadedHandler.GetJobInfo();
        }

        /// <summary>
        /// Received a result from a client, process it
        /// </summary>
        public bool ReceivedResult(JobResult result)
        {
            return _loadedHandler.ReceivedResult(result);
        }

        /// <summary>
        /// Register the log event to the given sink
        /// </summary>
        public void RegisterLogEventSink(EventSink<LogEventArgs> sink)
        {
            _loadedHandler.Logger.LogEvent += sink.CallbackMethod;
        }
    }
}
