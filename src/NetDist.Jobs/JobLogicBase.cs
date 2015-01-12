
namespace NetDist.Jobs
{
    /// <summary>
    /// Base class for the client logic which is executed as a job
    /// </summary>
    /// <typeparam name="TIn">Type of job input object</typeparam>
    /// <typeparam name="TOut">Type of job output object</typeparam>
    public abstract class JobLogicBase<TIn, TOut> : IJobLogic
        where TIn : IJobInput
        where TOut : IJobOutput
    {
        public abstract TOut Process(TIn input);

        public IJobOutput Process(IJobInput input)
        {
            return Process((TIn)input);
        }
    }
}
