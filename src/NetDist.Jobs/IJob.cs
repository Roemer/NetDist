
namespace NetDist.Jobs
{
    public interface IJob
    {
        IJobOutput Process(string jobInputString);
    }
}
