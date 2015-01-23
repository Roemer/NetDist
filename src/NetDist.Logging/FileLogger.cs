using NetDist.Core.Utilities;
using System;
using System.IO;
using System.Text;

namespace NetDist.Logging
{
    /// <summary>
    /// Round-robin file based logger
    /// </summary>
    public class FileLogger : LoggerBase
    {
        private readonly string _basePath;
        private readonly string _filenameSuffix;

        public FileLogger(string filenameSuffix, string basePath = null)
        {
            _basePath = String.IsNullOrWhiteSpace(basePath) ? Path.Combine(Directory.GetCurrentDirectory(), @"Log\") : basePath;
            _filenameSuffix = filenameSuffix;
        }

        protected override void Log(LogLevel logLevel, string message, Exception exception = null)
        {
            if (exception != null)
            {
                var exceptionString = ExceptionString(exception);
                message = String.Format("{0}\r\n    {1}", message, exceptionString);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_basePath) ?? String.Empty);
            // Write the to the file
            var fileName = String.Format("{0}{1:HH}_{2}.log", _basePath, DateTime.Now, _filenameSuffix);
            var content = String.Format("[{0:yyyy-MM-dd HH:mm:ss}] {1} {2}\r\n", DateTime.Now, logLevel.ToString().ToUpper(), message);
            using (var fs = FileUtility.WaitForFile(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
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

        private string ExceptionString(Exception exception)
        {
            var output = new StringBuilder();
            var currentException = exception;
            if (currentException != null)
            {
                output.AppendFormat("\r\n    {0}", CleanExceptionMessage(currentException));
                while (currentException.InnerException != null)
                {
                    currentException = currentException.InnerException;
                    output.AppendFormat("\r\n    {0}", CleanExceptionMessage(currentException));
                }
            }
            return output.ToString();
        }

        private string CleanExceptionMessage(Exception exception)
        {
            var output = String.Format("[{0}] {1} => {2}", exception.GetType(), exception.Message, exception.StackTrace);
            output = output.Replace(Environment.NewLine, " | ");
            return output;
        }
    }
}
