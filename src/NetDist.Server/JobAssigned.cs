using System;

namespace NetDist.Server
{
    public class JobAssigned
    {
        public Job Job { get; set; }
        public DateTime StartTime { get; set; }
    }
}
