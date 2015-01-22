using NetDist.Jobs.DataContracts;
using System;

namespace NetDist.Jobs
{
    public interface IJobScript
    {
        JobResult Process(Job job, Guid clientId);
    }
}
