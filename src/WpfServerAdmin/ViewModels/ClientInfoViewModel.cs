using System;

namespace WpfServerAdmin.ViewModels
{
    public class ClientInfoViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public ulong TotalMemory { get; set; }
        public ulong UsedMemory { get; set; }
        public float Cpu { get; set; }
        public float MemoryPercentage { get { return UsedMemory / (float)TotalMemory * 100; } }
        public long TotalJobsProcessed { get; set; }
        public long TotalJobsFailed { get; set; }
        public int JobsInProgress { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}
