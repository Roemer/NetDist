using Microsoft.Owin.Hosting;
using System;

namespace NetDist.Server.WebApi
{
    public class WebApiServer : ServerBase<string>
    {
        private IDisposable _app;

        protected override bool InternalStart()
        {
            const string baseAddress = "http://localhost:9000/";
            Logger.Info("Starting OWIN at '{0}'", baseAddress);
            _app = WebApp.Start<Startup>(new StartOptions(baseAddress));

            this.AddJobLogic();

            return true;
        }

        protected override bool InternalStop()
        {
            if (_app != null)
            {
                Logger.Info("Server stopped");
                _app.Dispose();
            }
            return true;
        }
    }
}
