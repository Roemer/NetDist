using System;

namespace NetDist.Jobs
{
    /// <summary>
    /// Class for all jobs which have been run
    /// </summary>
    public class JobResult
    {
        /// <summary>
        /// The ID of the job
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The ID of the handler which wanted the job to be done
        /// </summary>
        public Guid HandlerId { get; set; }

        /// <summary>
        /// Flag to indicate if the job finished correctly or not
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// Object which holds the error information in case one occured
        /// </summary>
        public JobError Error { get; set; }

        /// <summary>
        /// The result of the job
        /// </summary>
        public string JobOutputString { get; set; }

        public JobResult()
        {
        }

        private JobResult(Guid jobId, Guid handlerId)
        {
            Id = jobId;
            HandlerId = handlerId;
        }

        public JobResult(Guid jobId, Guid handlerId, IJobOutput output)
            : this(jobId, handlerId)
        {
            //TODO?: JobOutputString = JobObjectSerializer.Serialize(output);
        }

        public JobResult(Guid jobId, Guid handlerId, string outputString)
            : this(jobId, handlerId)
        {
            JobOutputString = outputString;
        }

        public JobResult(Guid jobId, Guid handlerId, Exception ex)
            : this(jobId, handlerId)
        {
            HasError = true;
            Error = new JobError(ex);
        }

        public void SetFailed(Exception ex)
        {
            HasError = true;
            Error = new JobError(ex);
            JobOutputString = null;
        }
    }
}
