using System;
using System.Collections.Generic;

namespace NetDist.Core
{
    [Serializable]
    public class HandlerJobInfo
    {
        public string HandlerName { get; set; }
        public string JobAssemblyName { get; set; }
        public List<string> Depdendencies { get; set; }

        public HandlerJobInfo()
        {
            Depdendencies = new List<string>();
        }
    }
}
