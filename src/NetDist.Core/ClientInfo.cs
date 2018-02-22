using System;
using System.Collections.Generic;

namespace NetDist.Core
{
    [Serializable]
    public class ClientInfo
    {
        public Guid Id { get; set; }
        public ulong TotalMemory { get; set; }
        public ulong UsedMemory { get; set; }
        public float CpuUsage { get; set; }
        public List<DiskInformation> DiskInformations { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public DateTime StartDate { get; set; }

        public ClientInfo()
        {
            DiskInformations = new List<DiskInformation>();
        }
    }

    [Serializable]
    public class DiskInformation
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public ulong TotalDiskSpace { get; set; }
        public ulong FreeDiskSpace { get; set; }
    }
}
