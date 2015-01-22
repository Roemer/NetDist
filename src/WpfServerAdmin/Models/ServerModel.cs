using NetDist.ServerAdmin.WebApi;
using Wpf.Shared;

namespace WpfServerAdmin.Models
{
    public class ServerModel
    {
        public WebApiServerAdmin Server { get; private set; }

        private readonly PortableConfiguration _conf = new PortableConfiguration(new JsonNetSerializer());

        public ServerModel()
        {
            var settings = _conf.Load<WebApiServerAdminSettings>("AdminSettings");
            Server = new WebApiServerAdmin(settings);
        }
    }
}
