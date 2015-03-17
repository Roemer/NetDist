using System;

namespace NetDist.Core
{
    [Serializable]
    public class LoadedHandlerStats
    {
        public long TotalJobsAvailable { get; set; }
        public int JobsAvailable { get; set; }
        public int JobsPending { get; set; }
        public long TotalJobsProcessed { get; set; }
        public long TotalJobsFailed { get; set; }
        public long SequencedJobsFailed { get; set; }
    }
}
