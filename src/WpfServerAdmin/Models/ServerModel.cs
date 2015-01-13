using NetDist.ServerAdmin.WebApi;

namespace WpfServerAdmin.Models
{
    public class ServerModel
    {
        public WebApiServerAdmin Server { get; private set; }

        public ServerModel()
        {
            Server = new WebApiServerAdmin();
        }
    }
}
