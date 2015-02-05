using System;

namespace NetDist.Logging
{
    public class ConsoleLogger : LoggerBase
    {
        public ConsoleLogger(LogLevel minLevel = LogLevel.Warn)
            : base(minLevel) { }

        protected override void Log(LogEntry logEntry)
        {
            var message = logEntry.Message;
            if (logEntry.Exceptions.Count > 0)
            {
                message = String.Format("{0}\r\n    {1}", message, logEntry.Exceptions[0]);
            }
            if (logEntry.HandlerId.HasValue)
            {
                message = String.Format("Handler: {0} - {1}", logEntry.HandlerId.Value, message);
            }
            else if (logEntry.ClientId.HasValue)
            {
                message = String.Format("Client: {0} - {1}", logEntry.ClientId.Value, message);
            }
            else
            {
                message = String.Format("Server - {0}", message);
            }
            var content = String.Format("[{0:yyyy-MM-dd HH:mm:ss}] [{1}] {2}", logEntry.LogDate, logEntry.LogLevel, message);
            Console.WriteLine(content);
        }
    }
}
