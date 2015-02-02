
namespace NetDist.Logging
{
    /// <summary>
    /// Base class for loggers
    /// </summary>
    public abstract class LoggerBase
    {
        public LogLevel MaxLevel { get; set; }

        protected LoggerBase(LogLevel maxLevel = LogLevel.Warn)
        {
            MaxLevel = maxLevel;
        }

        protected abstract void Log(LogEntry logEntry);

        public void Log(object sender, LogEventArgs eventArgs)
        {
            if (eventArgs.LogEntry.LogLevel >= MaxLevel)
            {
                Log(eventArgs.LogEntry);
            }
        }
    }
}
