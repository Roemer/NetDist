using System;

namespace NetDist.Logging
{
    public class LogEventArgs : EventArgs
    {
        public LogLevel LogLevel { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }

        public LogEventArgs(LogLevel logLevel, string message, Exception exception)
        {
            LogLevel = logLevel;
            Message = message;
            Exception = exception;
        }
    }
}
