using System;

namespace NetDist.Logging
{
    [Serializable]
    public class LogEventArgs : EventArgs
    {
        public LogEntry LogEntry { get; set; }

        public LogEventArgs(LogEntry logEntry)
        {
            LogEntry = logEntry;
        }
    }
}
