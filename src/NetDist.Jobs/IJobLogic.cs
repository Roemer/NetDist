
namespace NetDist.Jobs
{
    public interface IJobLogic
    {
        IJobOutput Process(IJobInput jobInput);
    }
}
