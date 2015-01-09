
namespace NetDist.Jobs
{
    public interface IJob
    {
        IJobOutput Process(IJobInput jobInput);
    }
}
