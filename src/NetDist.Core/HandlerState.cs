
namespace NetDist.Core
{
    public enum HandlerState
    {
        /// <summary>
        /// In defined idle time range
        /// </summary>
        Idle,
        /// <summary>
        /// Running and processing jobs
        /// </summary>
        Running,
        /// <summary>
        /// Completely stopped
        /// </summary>
        Stopped,
        /// <summary>
        /// Finished all work
        /// </summary>
        Finished,
        /// <summary>
        /// Paused, not started automatically
        /// </summary>
        Paused,
        /// <summary>
        /// Disabled, not started automatically
        /// </summary>
        Disabled,
        Failed,
    }
}
