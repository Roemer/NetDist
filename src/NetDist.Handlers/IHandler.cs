using NetDist.Jobs;
using NetDist.Logging;
using System;

namespace NetDist.Handlers
{
    /// <summary>
    /// Interface for the handlers, used by the server only
    /// </summary>
    internal interface IHandler
    {
        /// <summary>
        /// Used to check if a handler has finished all it's work
        /// </summary>
        /// <returns>True if finished, false otherwise</returns>
        bool IsFinished { get; }

        /// <summary>
        /// Event when a new job is enqueued
        /// </summary>
        event Action<IJobInput, object> EnqueueJobEvent;

        /// <summary>
        /// The logger object
        /// </summary>
        Logger Logger { get; }

        /// <summary>
        /// Initializes the custom settings object from the serialized string
        /// </summary>
        void InitializeCustomSettings(object customSettings);

        /// <summary>
        /// Allows custom initialization to be called which need the custom settings object
        /// </summary>
        void Initialize();

        /// <summary>
        /// Get the total count of jobs which are available
        /// </summary>
        long GetTotalJobCount();

        /// <summary>
        /// No more jobs in the queue, generate additional jobs
        /// </summary>
        void CreateMoreJobs();

        /// <summary>
        /// Process a result
        /// </summary>
        void ProcessResult(IJobInput jobInput, string jobResultString, object additionalData);

        /// <summary>
        /// Method which is called when the handler is started
        /// </summary>
        void OnStart();

        /// <summary>
        /// Method which is called when the handler is stopped
        /// </summary>
        void OnStop();

        /// <summary>
        /// Method which is called when the handler is finished
        /// </summary>
        void OnFinished();
    }
}
