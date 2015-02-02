using System;

namespace NetDist.Logging
{
    public class Logger
    {
        public event EventHandler<LogEventArgs> LogEvent;

        public void Debug(string message, params object[] messageParams)
        {
            Debug(null, message, messageParams);
        }
        public void Debug(Action<LogEntry> fillAction, string message, params object[] messageParams)
        {
            var logEntry = new LogEntry(LogLevel.Debug).SetMessage(message, messageParams);
            if (fillAction != null) { fillAction(logEntry); }
            Log(logEntry);
        }

        public void Info(string message, params object[] messageParams)
        {
            Info(null, message, messageParams);
        }
        public void Info(Action<LogEntry> fillAction, string message, params object[] messageParams)
        {
            var logEntry = new LogEntry(LogLevel.Info).SetMessage(message, messageParams);
            if (fillAction != null) { fillAction(logEntry); }
            Log(logEntry);
        }

        public void Warn(string message, params object[] messageParams)
        {
            Warn(null, null, message, messageParams);
        }
        public void Warn(Action<LogEntry> fillAction, string message, params object[] messageParams)
        {
            Warn(fillAction, null, message, messageParams);
        }
        public void Warn(Exception exception, string message, params object[] messageParams)
        {
            Warn(null, exception, message, messageParams);
        }
        public void Warn(Action<LogEntry> fillAction, Exception exception, string message = null, params object[] messageParams)
        {
            var logEntry = new LogEntry(LogLevel.Warn).SetMessage(message, messageParams).SetException(exception);
            if (fillAction != null) { fillAction(logEntry); }
            Log(logEntry);
        }

        public void Error(string message, params object[] messageParams)
        {
            Error(null, null, message, messageParams);
        }
        public void Error(Action<LogEntry> fillAction, string message, params object[] messageParams)
        {
            Error(fillAction, null, message, messageParams);
        }
        public void Error(Exception exception, string message, params object[] messageParams)
        {
            Error(null, exception, message, messageParams);
        }
        public void Error(Action<LogEntry> fillAction, Exception exception, string message = null, params object[] messageParams)
        {
            var logEntry = new LogEntry(LogLevel.Error).SetMessage(message, messageParams).SetException(exception);
            if (fillAction != null) { fillAction(logEntry); }
            Log(logEntry);
        }

        public void Fatal(string message, params object[] messageParams)
        {
            Fatal(null, null, message, messageParams);
        }
        public void Fatal(Action<LogEntry> fillAction, string message, params object[] messageParams)
        {
            Fatal(fillAction, null, message, messageParams);
        }
        public void Fatal(Exception exception, string message, params object[] messageParams)
        {
            Fatal(null, exception, message, messageParams);
        }
        public void Fatal(Action<LogEntry> fillAction, Exception exception, string message = null, params object[] messageParams)
        {
            var logEntry = new LogEntry(LogLevel.Fatal).SetMessage(message, messageParams).SetException(exception);
            if (fillAction != null) { fillAction(logEntry); }
            Log(logEntry);
        }

        public void Log(LogEntry logEntry)
        {
            var handlers = LogEvent;
            if (handlers != null)
            {
                handlers(null, new LogEventArgs(logEntry));
            }
        }
    }
}
