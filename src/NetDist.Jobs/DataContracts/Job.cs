using NetDist.Core.Utilities;
using System;
using System.Text;

namespace NetDist.Jobs.DataContracts
{
    /// <summary>
    /// Class for job instances which should be executed
    /// </summary>
    [Serializable]
    public class Job
    {
        /// <summary>
        /// The id of the job
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// The id of the handler who created the job
        /// </summary>
        public Guid HandlerId { get; set; }
        /// <summary>
        /// Hash of the current job script
        /// </summary>
        public string Hash { get; set; }
        /// <summary>
        /// Input object, compressed
        /// </summary>
        public byte[] JobInputCompressed { get; set; }

        public Job() { }

        public Job(Guid id, Guid handlerId, string jobInputString, string hash)
        {
            Id = id;
            HandlerId = handlerId;
            Hash = hash;
            JobInputCompressed = ZipUtility.GZipCompress(Encoding.UTF8.GetBytes(jobInputString));
        }

        public string GetInput()
        {
            return ZipUtility.GZipExtractToString(JobInputCompressed, Encoding.UTF8);
        }
    }
}
