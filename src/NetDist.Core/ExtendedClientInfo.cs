using System;

namespace NetDist.Core
{
    public class ExtendedClientInfo
    {
        public ClientInfo ClientInfo { get; set; }
        public DateTime LastCommunicationDate { get; set; }
        public long TotalJobsProcessed { get; set; }
        public long TotalJobsFailed { get; set; }
        public int JobsInProgress { get; set; }
    }
}
