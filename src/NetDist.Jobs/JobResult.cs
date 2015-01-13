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
        public Guid JobId { get; set; }

        /// <summary>
        /// The ID of the handler which wanted the job to be done
        /// </summary>
        public Guid HandlerId { get; set; }

        /// <summary>
        /// The Id of the client which processed the job
        /// </summary>
        public Guid ClientId { get; set; }

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

        private JobResult(Guid jobId, Guid handlerId, Guid clientId)
        {
            JobId = jobId;
            HandlerId = handlerId;
            ClientId = clientId;
        }

        public JobResult(Guid jobId, Guid handlerId, Guid clientId, IJobOutput output)
            : this(jobId, handlerId, clientId)
        {
            //TODO?: JobOutputString = JobObjectSerializer.Serialize(output);
        }

        public JobResult(Guid jobId, Guid handlerId, Guid clientId, string outputString)
            : this(jobId, handlerId, clientId)
        {
            JobOutputString = outputString;
        }

        public JobResult(Guid jobId, Guid handlerId, Guid clientId, Exception ex)
            : this(jobId, handlerId, clientId)
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
