
namespace NetDist.Logging
{
    /// <summary>
    /// Base class for loggers
    /// </summary>
    public abstract class LoggerBase
    {
        public LogLevel MinLevel { get; set; }

        protected LoggerBase(LogLevel minLevel = LogLevel.Warn)
        {
            MinLevel = minLevel;
        }

        protected abstract void Log(LogEntry logEntry);

        public void Log(object sender, LogEventArgs eventArgs)
        {
            if (eventArgs.LogEntry.LogLevel >= MinLevel)
            {
                Log(eventArgs.LogEntry);
            }
        }
    }
}
