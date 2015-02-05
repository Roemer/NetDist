using NetDist.Core.Utilities;
using System;
using System.IO;

namespace NetDist.Logging
{
    /// <summary>
    /// Round-robin file based logger
    /// </summary>
    public class FileLogger : LoggerBase
    {
        private readonly string _basePath;
        private readonly string _filenameSuffix;

        public FileLogger(string filenameSuffix, LogLevel minLevel = LogLevel.Warn, string basePath = null)
            : base(minLevel)
        {
            _basePath = String.IsNullOrWhiteSpace(basePath) ? Path.Combine(Directory.GetCurrentDirectory(), @"Log\") : basePath;
            _filenameSuffix = filenameSuffix;
        }

        protected override void Log(LogEntry logEntry)
        {
            var message = logEntry.Message;
            if (logEntry.Exceptions.Count > 0)
            {
                message = String.Format("{0}\r\n    {1}", message, logEntry.Exceptions[0]);
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
            var content = String.Format("[{0:yyyy-MM-dd HH:mm:ss}] [{1}] {2}", logEntry.LogDate, logEntry.LogLevel, message);

            Directory.CreateDirectory(Path.GetDirectoryName(_basePath) ?? String.Empty);
            // Write the to the file
            var fileName = String.Format("{0}{1:HH}_{2}.log", _basePath, DateTime.Now, _filenameSuffix);
            using (var fs = FileUtility.WaitForFile(fileName, FileMode.Append, FileAccess.Write, FileShare.Read))
            {
                using (var sw = new StreamWriter(fs))
                {
                    sw.WriteLine(content);
                }
            }
            // Delete the upcoming file
            var upcomingFileName = String.Format("{0}{1:HH}_{2}.log", _basePath, DateTime.Now.AddHours(1), _filenameSuffix);
            if (File.Exists(upcomingFileName))
            {
                File.Delete(upcomingFileName);
            }
        }
    }
}
