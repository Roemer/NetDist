using System.Web.Http;

namespace NetDist.Server.WebApi.Controllers
{
    public abstract class ControllerBase : ApiController
    {
        protected WebApiServer Server { get; private set; }

        protected ControllerBase(WebApiServer server)
        {
            Server = server;
        }
    }
}
