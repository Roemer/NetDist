using NetDist.Core.Utilities;
using NetDist.Logging;
using System;

namespace WpfServer.ViewModels
{
    public class LogEntryViewModel : ObservableObject
    {
        public DateTime Date { get; set; }
        public LogLevel LogLevel { get; set; }
        public string Message { get; set; }

        public LogEntryViewModel(LogEntry logEntry)
        {
            Date = logEntry.LogDate;
            LogLevel = logEntry.LogLevel;

            var message = logEntry.Message;
            if (logEntry.Exceptions.Count > 0)
            {
                message = String.Format("{0}\r\n  {1}\r\n  {2}", message, logEntry.Exceptions[0].ExceptionMessage, "View log files for further information!");
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
            Message = message;
        }
    }
}
