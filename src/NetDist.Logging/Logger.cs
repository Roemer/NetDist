using System;

namespace NetDist.Logging
{
    public class Logger
    {
        public event EventHandler<LogEventArgs> LogEvent;

        public void Debug(string message, params object[] messageParams)
        {
            Log(LogLevel.Debug, message, messageParams);
        }

        public void Info(string message, params object[] messageParams)
        {
            Log(LogLevel.Info, message, messageParams);
        }

        public void Warn(string message, params object[] messageParams)
        {
            Warn(null, message, messageParams);
        }
        public void Warn(Exception exception, string message = null, params object[] messageParams)
        {
            Log(LogLevel.Warn, exception, message, messageParams);
        }

        public void Error(string message, params object[] messageParams)
        {
            Error(null, message, messageParams);
        }
        public void Error(Exception exception, string message = null, params object[] messageParams)
        {
            Log(LogLevel.Error, exception, message, messageParams);
        }

        public void Fatal(string message, params object[] messageParams)
        {
            Error(null, message, messageParams);
        }
        public void Fatal(Exception exception, string message = null, params object[] messageParams)
        {
            Log(LogLevel.Fatal, exception, message, messageParams);
        }

        public void Log(LogLevel logLevel, string message, params object[] messageParams)
        {
            Log(logLevel, null, message, messageParams);
        }

        public void Log(LogLevel logLevel, Exception exception, string message = null, params object[] messageParams)
        {
            var messageString = (messageParams == null || messageParams.Length == 0 || message == null) ? message : String.Format(message, messageParams);
            var handlers = LogEvent;
            if (handlers != null)
            {
                handlers(this, new LogEventArgs(logLevel, messageString, exception));
            }
        }
    }
}
