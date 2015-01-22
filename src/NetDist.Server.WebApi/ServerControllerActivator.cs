using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;

namespace NetDist.Server.WebApi
{
    public class ServerControllerActivator : IHttpControllerActivator
    {
        private readonly ServerBase _server;

        public ServerControllerActivator(ServerBase server)
        {
            _server = server;
        }

        public IHttpController Create(HttpRequestMessage request, HttpControllerDescriptor controllerDescriptor, Type controllerType)
        {
            return (IHttpController)Activator.CreateInstance(controllerType, _server);
        }
    }
}
