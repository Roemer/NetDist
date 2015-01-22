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
        public Guid Id { get; set; }
        public Guid HandlerId { get; set; }
        public byte[] JobInputCompressed { get; set; }

        public Job() { }

        public Job(Guid id, Guid handlerId, string jobInputString)
        {
            Id = id;
            HandlerId = handlerId;
            JobInputCompressed = ZipUtility.GZipCompress(Encoding.UTF8.GetBytes(jobInputString));
        }

        public string GetInput()
        {
            return ZipUtility.GZipExtractToString(JobInputCompressed, Encoding.UTF8);
        }
    }
}
