using NetDist.Core;
using NetDist.Jobs;
using System;

namespace NetDist.Client
{
    public abstract class ClientBase
    {
        public ClientInfo ClientInfo { get; private set; }

        protected ClientBase()
        {
            ClientInfo = new ClientInfo();
            ClientInfo.Id = Guid.NewGuid();
            ClientInfo.Name = Environment.MachineName.ToLower();
            ClientInfo.StartDate = DateTime.Now;
            ClientInfo.Version = "unknown";
        }

        public abstract Job GetJob();
        public abstract void SendResult(JobResult result);
    }
}
