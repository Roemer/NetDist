using NetDist.Jobs;
using NetDist.Server.XDomainObjects;
using System;

namespace NetDist.Server
{
    [Serializable]
    public class LoadedHandlerInitializeParams
    {
        public JobScriptFile JobScriptFile { get; set; }
        public HandlerSettings HandlerSettings { get; set; }
        public string JobAssemblyPath { get; set; }
    }
}
