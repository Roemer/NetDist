using System;

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

        protected abstract void Log(LogLevel logLevel, string message, Exception exception = null);

        public void Log(object sender, LogEventArgs eventArgs)
        {
            if (eventArgs.LogLevel >= MaxLevel)
            {
                Log(eventArgs.LogLevel, eventArgs.Message, eventArgs.Exception);
            }
        }
    }
}
