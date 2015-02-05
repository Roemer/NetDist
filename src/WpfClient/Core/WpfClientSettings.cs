using NetDist.Client.WebApi;

namespace WpfClient.Core
{
    public class WpfClientSettings : WebApiClientSettings
    {
        public bool IsDebug { get; set; }
        public string FtpUrl { get; set; }
        public string FtpUser { get; set; }
        public string FtpPassword { get; set; }
    }
}
