
namespace NetDist.Server.WebApi
{
    public class WebApiServerSettings : IServerSettings
    {
        public bool AutoStart { get; set; }
        public int Port { get; set; }

        public WebApiServerSettings()
        {
            AutoStart = true;
            Port = 9000;
        }
    }
}
