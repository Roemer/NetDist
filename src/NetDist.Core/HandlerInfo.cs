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
        public int AvailableJobs { get; set; }
        public int PendingJobs { get; set; }
        public long TotalJobs { get; set; }
        public HandlerState HandlerState { get; set; }
    }
}
