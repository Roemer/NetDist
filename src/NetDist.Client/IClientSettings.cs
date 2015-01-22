
namespace NetDist.Client
{
    public interface IClientSettings
    {
        bool AutoStart { get; set; }
        int NumberOfParallelJobs { get; set; }
    }
}
