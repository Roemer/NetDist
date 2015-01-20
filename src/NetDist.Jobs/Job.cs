using System;

namespace NetDist.Jobs
{
    /// <summary>
    /// Class for job instances which should be executed
    /// </summary>
    [Serializable]
    public class Job
    {
        public Guid Id { get; set; }
        public Guid HandlerId { get; set; }
        public string JobInputString { get; private set; }

        public Job(Guid handlerId, string jobInputString)
        {
            Id = Guid.NewGuid();
            HandlerId = handlerId;
            JobInputString = jobInputString;
        }
    }
}
