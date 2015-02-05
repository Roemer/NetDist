using System;

namespace NetDist.Client
{
    /// <summary>
    /// Interface for all the basic settings for the client
    /// </summary>
    public interface IClientSettings
    {
        /// <summary>
        /// Unique id of the client
        /// </summary>
        Guid Id { get; }
        /// <summary>
        /// Textual representation of the client's name
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Number of jobs that can run in parallel
        /// </summary>
        int NumberOfParallelJobs { get; }
        /// <summary>
        /// Flag to indicate if the client should start automatically
        /// </summary>
        bool AutoStart { get; }
    }
}
