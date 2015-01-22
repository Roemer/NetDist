
namespace NetDist.Client.WebApi
{
    public class WebApiClientSettings : IClientSettings
    {
        public bool AutoStart { get; set; }
        public int NumberOfParallelJobs { get; set; }
        public string ServerUri { get; set; }

        public WebApiClientSettings()
        {
            AutoStart = false;
            NumberOfParallelJobs = 3;
            ServerUri = @"http://localhost:9000/";
        }
    }
}
