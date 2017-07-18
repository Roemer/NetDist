﻿using Microsoft.Owin.Hosting;
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
            AddSavedJobScripts();
        }

        protected override bool InternalStart()
        {
            var baseUri = String.Format("{0}://{1}:{2}", "http", "*", Settings.Port);
            Logger.Info("Starting OWIN at '{0}'", baseUri);
            try
            {
                _app = WebApp.Start(new StartOptions(baseUri), builder => new Startup(this).Configuration(builder));
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Failed to start OWIN at '{0}'", baseUri);
            }

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

        private void AddSavedJobScripts()
        {
            foreach (var jobScriptInfo in _handlerManager.GetSavedJobScripts())
            {
                AddJobScript(jobScriptInfo);
            }
        }
    }
}
