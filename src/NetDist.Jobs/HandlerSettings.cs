
namespace NetDist.Jobs
{
    public class HandlerSettings
    {
        /// <summary>
        /// The name of the handler to use
        /// </summary>
        public string HandlerName { get; set; }

        /// <summary>
        /// The name of the job
        /// </summary>
        public string JobName { get; set; }

        /// <summary>
        /// Timeout (in seconds) when a job was assigned to a client until it is reassigned to another client
        /// Use 0 to deactivate the timeout
        /// </summary>
        public int JobTimeout { get; set; }

        /// <summary>
        /// Autostart the handler
        /// </summary>
        public bool AutoStart { get; set; }
    }
}
