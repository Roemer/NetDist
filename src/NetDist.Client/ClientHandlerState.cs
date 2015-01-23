using NetDist.Core;

namespace NetDist.Client
{
    public class ClientHandlerState
    {
        public bool IsInitialized { get; set; }
        public string MainAssemblyName { get; set; }
        public HandlerJobInfo HandlerJobInfo { get; set; }
    }
}
