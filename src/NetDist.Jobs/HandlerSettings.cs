using System;
using System.Collections.Generic;

namespace NetDist.Jobs
{
    [Serializable]
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
        /// Timeout (in seconds) when a job was assigned to a client until it is reassigned to another client.
        /// Use 0 to deactivate the timeout.
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
        /// Maximum amount of sequenced results with errors.
        /// Use 0 to deactivate this check.
        /// </summary>
        public decimal MaxSequencedErrors { get; set; }

        /// <summary>
        /// String to define a range when the handler should not send jobs
        /// Example:
        /// 22:00 - 06:00
        /// </summary>
        public string IdleTime { get; set; }

        /// <summary>
        /// Selectors of clients which are allowed to process this job
        /// Takes precedence if ClientsDenied is also set
        /// </summary>
        public List<ClientSelector> ClientsAllowed { get; set; }

        /// <summary>
        /// Selectors of clients which are not allowed to process this job
        /// Ignored if ClientsAllowed is also set
        /// </summary>
        public List<ClientSelector> ClientsDenied { get; set; }

        public HandlerSettings()
        {
            ClientsAllowed = new List<ClientSelector>();
            ClientsDenied = new List<ClientSelector>();
            // Set default values
            MaxSequencedErrors = 5;
        }

        [Serializable]
        public class ClientSelector
        {
            /// <summary>
            /// Regular expression to select the name of a client
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// ID of the client
            /// </summary>
            public Guid? Id { get; set; }
        }
    }
}
