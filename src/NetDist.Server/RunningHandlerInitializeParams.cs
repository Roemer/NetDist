using NetDist.Jobs;
using System;

namespace NetDist.Server
{
    [Serializable]
    public class RunningHandlerInitializeParams
    {
        public string JobHash { get; set; }
        public string PackageName { get; set; }
        public HandlerSettings HandlerSettings { get; set; }
        public string JobAssemblyPath { get; set; }
    }
}
