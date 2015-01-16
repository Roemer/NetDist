using System;

namespace NetDist.Core
{
    public class ClientInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime LastUpdateTime { get; set; }
    }
}
