using System.Web.Http;

namespace NetDist.Server.WebApi.Controllers
{
    public abstract class ControllerBase : ApiController
    {
        protected ServerBase Server { get; private set; }

        protected ControllerBase(ServerBase server)
        {
            Server = server;
        }
    }
}
