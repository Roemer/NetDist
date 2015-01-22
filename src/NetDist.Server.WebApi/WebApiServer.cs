using Microsoft.Owin.Hosting;
using NetDist.Logging;
using System;

namespace NetDist.Server.WebApi
{
    public class WebApiServer : ServerBase
    {
        /// <summary>
        /// Settings object
        /// </summary>
        private readonly WebApiServerSettings _settings;

        /// <summary>
        /// Reference to the web-api server object
        /// </summary>
        private IDisposable _app;

        /// <summary>
        /// Constructor
        /// </summary>
        public WebApiServer(WebApiServerSettings settings, params EventHandler<LogEventArgs>[] defaultLogEvents)
        {
            foreach (var logEvent in defaultLogEvents)
            {
                Logger.LogEvent += logEvent;
            }
            _settings = settings;
            InitializeSettings(settings);
        }

        protected override bool InternalStart()
        {
            var baseUri = String.Format("{0}://{1}:{2}", "http", "*", _settings.Port);
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
