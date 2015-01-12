using NetDist.Jobs;
using System;

namespace NetDist.Server
{
    /// <summary>
    /// Class for job instances which should be executed
    /// </summary>
    public class Job
    {
        public Guid Id { get; set; }
        public Guid HandlerId { get; set; }
        public IJobInput JobInput { get; private set; }

        public Job(Guid handlerId, IJobInput jobInput)
        {
            Id = Guid.NewGuid();
            HandlerId = handlerId;
            JobInput = jobInput;
        }
    }
}
