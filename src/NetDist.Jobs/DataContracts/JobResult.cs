using NetDist.Core.Utilities;
using System;
using System.Text;

namespace NetDist.Jobs.DataContracts
{
    /// <summary>
    /// Class for all jobs which have been run
    /// </summary>
    [Serializable]
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
        /// The result of the job (gzip compressed)
        /// </summary>
        public byte[] JobOutputCompressed { get; set; }

        public JobResult() { }

        private JobResult(Job job, Guid clientId)
        {
            JobId = job.Id;
            HandlerId = job.HandlerId;
            ClientId = clientId;
        }

        public JobResult(Job job, Guid clientId, string jobOutputString)
            : this(job, clientId)
        {
            JobOutputCompressed = ZipUtility.GZipCompress(Encoding.UTF8.GetBytes(jobOutputString));
        }

        public JobResult(Job job, Guid clientId, Exception ex)
            : this(job, clientId)
        {
            HasError = true;
            Error = new JobError(ex);
        }

        public string GetOutput()
        {
            return ZipUtility.GZipExtractToString(JobOutputCompressed, Encoding.UTF8);
        }
    }
}
