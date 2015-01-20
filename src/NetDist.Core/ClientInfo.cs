using System;

namespace NetDist.Core
{
    public class ClientInfo
    {
        public Guid Id { get; set; }
        public ulong TotalMemory { get; set; }
        public ulong UsedMemory { get; set; }
        public float CpuUsage { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public DateTime StartDate { get; set; }
    }
}
