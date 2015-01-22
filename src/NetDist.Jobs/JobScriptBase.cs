using NetDist.Core;
using NetDist.Jobs.DataContracts;
using System;

namespace NetDist.Jobs
{
    /// <summary>
    /// Base class for the client script which is executed as a job
    /// </summary>
    /// <typeparam name="TIn">Type of job input object</typeparam>
    /// <typeparam name="TOut">Type of job output object</typeparam>
    public abstract class JobScriptBase<TIn, TOut> : IJobScript
        where TIn : IJobInput
        where TOut : IJobOutput
    {
        public abstract TOut Process(TIn input);

        public JobResult Process(Job job, Guid clientId)
        {
            var inputString = job.GetInput();
            var inputObject = JobObjectSerializer.Deserialize<TIn>(inputString);
            try
            {
                var outputObject = Process(inputObject);
                var outputString = JobObjectSerializer.Serialize(outputObject);
                var result = new JobResult(job, clientId, outputString);
                return result;
            }
            catch (Exception ex)
            {
                return new JobResult(job, clientId, ex);
            }
        }
    }
}
