using System.Collections.Generic;

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

        /// <summary>
        /// CronTab schedule string for the handler
        /// </summary>
        public string Schedule { get; set; }

        /// <summary>
        /// Names of the clients which are allowed to process this job
        /// Takes precedence if ClientsDenied is also set
        /// </summary>
        public List<string> ClientsAllowed { get; set; }

        /// <summary>
        /// Names of the clients which are not allowed to process this job
        /// Ignored if ClientsAllowed is also set
        /// </summary>
        public List<string> ClientsDenied { get; set; }

        public HandlerSettings()
        {
            ClientsAllowed = new List<string>();
            ClientsDenied = new List<string>();
        }
    }
}
