using NetDist.Core;

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

        public string Process(string jobInputString)
        {
            var input = JobObjectSerializer.Deserialize<TIn>(jobInputString);
            var jobOutputString = Process(input);
            return JobObjectSerializer.Serialize(jobOutputString);
        }
    }
}
