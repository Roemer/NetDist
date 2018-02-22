using System;

namespace NetDist.Logging
{
    public class ConsoleLogger : LoggerBase
    {
        public ConsoleLogger(LogLevel minLevel = LogLevel.Warn)
            : base(minLevel) { }

        protected override void Log(LogEntry logEntry)
        {
            var message = logEntry.GetMessageWithAdditionalInformation();
            if (logEntry.Exceptions.Count > 0)
            {
                message = String.Format("{0}\r\n  {1}\r\n{2}", message, logEntry.Exceptions[0].ExceptionMessage, logEntry.Exceptions[0].ExceptionStackTrace);
                if (logEntry.Exceptions.Count > 1)
                {
                    message = String.Format("{0}\r\n  {1}\r\n{2}", message, logEntry.Exceptions[1].ExceptionMessage, logEntry.Exceptions[1].ExceptionStackTrace);
                }
            }

            var content = String.Format("[{0:yyyy-MM-dd HH:mm:ss}] [{1}] {2}", logEntry.LogDate, logEntry.LogLevel, message);
            Console.WriteLine(content);
        }
    }
}
