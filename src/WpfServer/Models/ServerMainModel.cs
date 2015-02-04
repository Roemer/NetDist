using NetDist.Core.Utilities;
using NetDist.Logging;
using NetDist.Server.WebApi;
using System;
using Wpf.Shared;

namespace WpfServer.Models
{
    public class ServerMainModel : ObservableObject
    {
        public WebApiServer Server { get; private set; }
        public event Action<LogEntry> LogEvent;

        private readonly PortableConfiguration _conf = new PortableConfiguration(new JsonNetSerializer());

        public void Initialize()
        {
            var settings = _conf.Load<WebApiServerSettings>("ServerSettings");
            Server = new WebApiServer(settings, new ConsoleLogger(LogLevel.Error).Log, new FileLogger("Server", LogLevel.Warn).Log, OnLogEvent);
        }

        private void OnLogEvent(object sender, LogEventArgs logEventArgs)
        {
            var handler = LogEvent;
            if (handler != null) handler(logEventArgs.LogEntry);
        }
    }
}
