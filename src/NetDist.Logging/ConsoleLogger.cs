using System;
using System.Text;

namespace NetDist.Logging
{
    public class ConsoleLogger : LoggerBase
    {
        public ConsoleLogger(LogLevel maxLevel = LogLevel.Warn)
            : base(maxLevel) { }

        protected override void InternalLog(LogLevel logLevel, string message, Exception exception = null)
        {
            if (exception != null)
            {
                var exceptionString = ExceptionString(exception);
                message = String.Format("{0}\r\n    {1}", message, exceptionString);
            }

            var content = String.Format("[{0:yyyy-MM-dd HH:mm:ss}] {1} {2}\r\n", DateTime.Now, logLevel.ToString().ToUpper(), message);
            Console.WriteLine(content);
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
