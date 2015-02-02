using System;
using System.Collections.Generic;

namespace NetDist.Logging
{
    /// <summary>
    /// Helper class to create log entries
    /// </summary>
    [Serializable]
    public class LogEntry
    {
        [Serializable]
        public class LogEntryException
        {
            public string ExceptionType { get; set; }
            public string ExceptionMessage { get; set; }
            public string ExceptionStackTrace { get; set; }

            public LogEntryException(Exception exception)
            {
                ExceptionType = exception.GetType().Name;
                ExceptionMessage = exception.Message;
                ExceptionStackTrace = exception.StackTrace;
            }

            public LogEntryException(string exceptionType, string exceptionMessage, string exceptionStackTrace)
            {
                ExceptionType = exceptionType;
                ExceptionMessage = exceptionMessage;
                ExceptionStackTrace = exceptionStackTrace;
            }

            public override string ToString()
            {
                return String.Format("[{0}] {1} => {2}", ExceptionType, ExceptionMessage, ExceptionStackTrace);
            }
        }

        public DateTime LogDate { get; set; }
        public LogLevel LogLevel { get; set; }
        public Guid? HandlerId { get; set; }
        public Guid? ClientId { get; set; }
        public string Message { get; set; }
        public string Remarks { get; set; }
        public List<LogEntryException> Exceptions { get; set; }

        public LogEntry(LogLevel logLevel)
        {
            LogDate = DateTime.Now;
            LogLevel = logLevel;
            Exceptions = new List<LogEntryException>();
        }

        public LogEntry SetHandlerId(Guid? handlerId)
        {
            HandlerId = handlerId;
            return this;
        }

        public LogEntry SetClientId(Guid? clientId)
        {
            ClientId = clientId;
            return this;
        }

        public LogEntry SetMessage(string message, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                message = String.Format(message, args);
            }
            Message = message;
            return this;
        }

        public LogEntry SetRemarks(string remarks, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                remarks = String.Format(remarks, args);
            }
            Remarks = remarks;
            return this;
        }

        public LogEntry SetException(Exception exception, bool withInnerExceptions = true)
        {
            Exceptions.Clear();
            if (exception != null)
            {
                Exceptions.Add(new LogEntryException(exception));
                if (withInnerExceptions)
                {
                    while (exception.InnerException != null)
                    {
                        exception = exception.InnerException;
                        Exceptions.Add(new LogEntryException(exception));
                    }
                }
            }
            return this;
        }

        public LogEntry SetException(string exceptionType, string exceptionMessage, string exceptionStackTrace)
        {
            Exceptions.Add(new LogEntryException(exceptionType, exceptionMessage, exceptionStackTrace));
            return this;
        }
    }
}
