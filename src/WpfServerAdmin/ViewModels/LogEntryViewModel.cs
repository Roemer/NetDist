using NetDist.Core;
using System;

namespace WpfServerAdmin.ViewModels
{
    public class LogEntryViewModel
    {
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string LogLevel { get; set; }

        public LogEntryViewModel(LogInfoEntry logEntry)
        {
            Message = logEntry.Message;
            Timestamp = logEntry.Timestamp;
            LogLevel = logEntry.Level;
        }
    }
}