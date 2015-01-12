using System.Collections.Generic;

namespace NetDist.Core
{
    public class ServerInfo
    {
        public ulong TotalMemory { get; set; }
        public ulong UsedMemory { get; set; }
        public List<HandlerInfo> Handlers { get; set; }

        public ServerInfo()
        {
            Handlers = new List<HandlerInfo>();
        }
    }
}
