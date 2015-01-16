
namespace NetDist.Client.WebApi
{
    public class WebApiClientSettings
    {
        public string ServerUri { get; set; }

        public WebApiClientSettings()
        {
            ServerUri = @"http://localhost:9000/";
        }
    }
}
