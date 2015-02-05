
namespace NetDist.Server.WebApi
{
    public class WebApiServerSettings : IServerSettings
    {
        public string PackagesFolder { get; set; }
        public bool AutoStart { get; set; }
        public int Port { get; set; }

        public WebApiServerSettings()
        {
            PackagesFolder = "packages";
            AutoStart = true;
            Port = 9000;
        }
    }
}
