using System;

namespace NetDist.Jobs
{
    public class JobError
    {
        public DateTime Time { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }

        public JobError()
        {
            Time = DateTime.Now;
        }

        public JobError(string message)
            : this()
        {
            Message = message;
        }

        public JobError(Exception ex)
            : this(ex.Message)
        {
            StackTrace = ex.StackTrace;
        }

        public override string ToString()
        {
            return String.Concat(Message, Environment.NewLine, StackTrace);
        }
    }
}
