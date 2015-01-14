
namespace NetDist.Jobs
{
    public interface IJobScript
    {
        IJobOutput Process(IJobInput jobInput);
    }
}
