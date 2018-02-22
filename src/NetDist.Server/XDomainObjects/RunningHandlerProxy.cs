using NetDist.Core;
using NetDist.Jobs.DataContracts;
using NetDist.Logging;
using System;

namespace NetDist.Server.XDomainObjects
{
    /// <summary>
    /// Cross-domain proxy object for a running handler
    /// </summary>
    public class RunningHandlerProxy : MarshalByRefObject
    {
        /// <summary>
        /// Member for the contcrete loaded handler
        /// </summary>
        private readonly RunningHandler _runningHandler;

        /// <summary>
        /// Flag to indicate if the handler has available jobs
        /// </summary>
        public bool HasAvailableJobs { get { return _runningHandler.HasAvailableJobs; } }

        /// <summary>
        /// Constructor
        /// </summary>
        public RunningHandlerProxy(Guid id, string packageBaseFolder)
        {
            _runningHandler = new RunningHandler(id, packageBaseFolder);
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
        public JobScriptInitializeResult InitializeAndStart(RunningHandlerInitializeParams runningHandlerInitializeParams)
        {
            return _runningHandler.InitializeAndStart(runningHandlerInitializeParams);
        }

        /// <summary>
        /// Replaces the job script with a new one
        /// </summary>
        public void ReplaceJobScriptHash(string newJobHash)
        {
            _runningHandler.ReplaceJobScriptHash(newJobHash);
        }

        /// <summary>
        /// Tells the handler to stop and clean up
        /// </summary>
        public bool TearDown(bool notifyStop)
        {
            return _runningHandler.TearDown(notifyStop);
        }

        /// <summary>
        /// Gets information about this handler
        /// </summary>
        public LoadedHandlerStats GetInfo()
        {
            return _runningHandler.GetInfo();
        }

        /// <summary>
        /// Gets an available job from the handler
        /// </summary>
        public Job GetJob(Guid clientId)
        {
            return _runningHandler.GetJob(clientId);
        }

        /// <summary>
        /// Received a result from a client, process it
        /// </summary>
        public bool ReceivedResult(JobResult result, ExtendedClientInfo clientInfo)
        {
            return _runningHandler.ReceivedResult(result, clientInfo);
        }

        /// <summary>
        /// Register the log event to the given sink
        /// </summary>
        public void RegisterLogEventSink(EventSink<LogEventArgs> sink)
        {
            _runningHandler.Logger.LogEvent += sink.CallbackMethod;
        }

        /// <summary>
        /// Register event for state changes
        /// </summary>
        public void RegisterStateChangedEventSink(EventSink<RunningHandlerStateChangedEventArgs> sink)
        {
            _runningHandler.StateChangedEvent += sink.CallbackMethod;
        }
    }
}
