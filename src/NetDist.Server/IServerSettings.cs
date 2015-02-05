
namespace NetDist.Server
{
    public interface IServerSettings
    {
        string PackagesFolder { get; }
        bool AutoStart { get; }
    }
}
