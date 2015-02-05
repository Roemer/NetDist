using Microsoft.Owin.Hosting;
using NetDist.Logging;
using System;

namespace NetDist.Server.WebApi
{
    public class WebApiServer : ServerBase<WebApiServerSettings>
    {
        /// <summary>
        /// Reference to the web-api server object
        /// </summary>
        private IDisposable _app;

        /// <summary>
        /// Constructor
        /// </summary>
        public WebApiServer(WebApiServerSettings settings, params EventHandler<LogEventArgs>[] defaultLogHandlers)
            : base(settings, defaultLogHandlers)
        {
        }

        protected override bool InternalStart()
        {
            var baseUri = String.Format("{0}://{1}:{2}", "http", "*", Settings.Port);
            Logger.Info("Starting OWIN at '{0}'", baseUri);
            _app = WebApp.Start(new StartOptions(baseUri), builder => new Startup(this).Configuration(builder));

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
