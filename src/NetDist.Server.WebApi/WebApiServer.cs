using Microsoft.Owin.Hosting;
using NetDist.Logging;
using System;

namespace NetDist.Server.WebApi
{
    public class WebApiServer : ServerBase<string>
    {
        private IDisposable _app;

        public WebApiServer()
        {
            Logger = new ConsoleLogger();
        }

        protected override bool StartServer()
        {
            const string baseAddress = "http://localhost:9000/";
            Logger.Info("Starting OWIN at '{0}'", baseAddress);
            _app = WebApp.Start<Startup>(new StartOptions(baseAddress));
            return true;
        }

        protected override void StopServer()
        {
            if (_app != null)
            {
                _app.Dispose();
            }
        }
    }
}
