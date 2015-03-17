using System;

namespace NetDist.Core
{
    [Serializable]
    public class HandlerInfo
    {
        public string PackageName { get; set; }
        public string HandlerName { get; set; }
        public string JobName { get; set; }
        public Guid Id { get; set; }
        public long TotalJobsAvailable { get; set; }
        public int JobsAvailable { get; set; }
        public int JobsPending { get; set; }
        public long TotalJobsProcessed { get; set; }
        public long TotalJobsFailed { get; set; }
        public HandlerState HandlerState { get; set; }
        public string HandlerMessage { get; set; }
        public DateTime? LastStartTime { get; set; }
        public DateTime? NextStartTime { get; set; }
    }
}
