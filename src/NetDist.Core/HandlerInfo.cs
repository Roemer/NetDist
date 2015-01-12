using System;

namespace NetDist.Core
{
    [Serializable]
    public class HandlerInfo
    {
        public string Name { get; set; }
        public Guid Id { get; set; }
        public int AvailableJobs { get; set; }
        public int PendingJobs { get; set; }
        public HandlerState HandlerState { get; set; }
    }
}
