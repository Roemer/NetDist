
namespace NetDist.ServerAdmin.WebApi
{
    public class WebApiServerAdminSettings
    {
        public string ServerUri { get; set; }

        public WebApiServerAdminSettings()
        {
            ServerUri = @"http://localhost:9000/";
        }
    }
}
