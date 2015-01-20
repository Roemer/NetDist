using NetDist.Core;
using NetDist.Jobs;
using System;
using System.Net;
using System.Net.Sockets;

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
            // Lookup the ip
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ClientInfo.Ip = ip.ToString();
                    break;
                }
            }
        }

        public abstract Job GetJob();
        public abstract void SendResult(JobResult result);
        public abstract HandlerClientInfo GetHandlerClientInfo(Guid handlerId);
        public abstract byte[] GetFile(Guid handlerId, string fileName);
    }
}
