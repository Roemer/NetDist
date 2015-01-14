using System;

namespace NetDist.Core
{
    [Serializable]
    public class HandlerInfo
    {
        public string PluginName { get; set; }
        public string HandlerName { get; set; }
        public string JobName { get; set; }
        public Guid Id { get; set; }
        public long TotalJobsAvailable { get; set; }
        public int JobsAvailable { get; set; }
        public int JobsPending { get; set; }
        public long TotalJobsProcessed { get; set; }
        public long TotalJobsFailed { get; set; }
        public HandlerState HandlerState { get; set; }
    }
}
