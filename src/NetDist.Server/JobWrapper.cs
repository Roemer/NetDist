using NetDist.Core;
using NetDist.Jobs;
using NetDist.Jobs.DataContracts;
using System;

namespace NetDist.Server
{
    /// <summary>
    /// Wrapper for a job ready to be processed, processing or processed
    /// </summary>
    public class JobWrapper
    {
        /// <summary>
        /// Id of this job
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Id of the handler of this job
        /// </summary>
        public Guid HandlerId { get; set; }

        /// <summary>
        /// Original input object of the job
        /// </summary>
        public IJobInput JobInput { get; set; }

        /// <summary>
        /// Additional data registered to the job from the handler
        /// </summary>
        public object AdditionalData { get; set; }

        /// <summary>
        /// Time when the job was queued
        /// </summary>
        public DateTime EnqueueTime { get; set; }

        /// <summary>
        /// Id of the client which got this job assigned
        /// </summary>
        public Guid? AssignedCliendId { get; set; }

        /// <summary>
        /// Time when the job was assigned to a client
        /// </summary>
        public DateTime? AssignedTime { get; set; }

        /// <summary>
        /// Time when the job finished processing
        /// </summary>
        public DateTime? ResultTime { get; set; }

        /// <summary>
        /// The result from the client after processing
        /// </summary>
        public string ResultString { get; set; }

        /// <summary>
        /// Reset the job to assign it again
        /// </summary>
        public void Reset()
        {
            AssignedCliendId = null;
            AssignedTime = null;
            ResultString = null;
            ResultString = null;
        }

        /// <summary>
        /// Create a job out of this wrapper
        /// </summary>
        public Job CreateJob(string hash)
        {
            var jobInputString = JobObjectSerializer.Serialize(JobInput);
            var job = new Job(Id, HandlerId, jobInputString, hash);
            return job;
        }
    }
}
