using System;
using System.Collections.Generic;

namespace NetDist.Core
{
    [Serializable]
    public class HandlerClientInfo
    {
        public string JobFile { get; set; }
        public List<string> Depdendencies { get; set; }

        public HandlerClientInfo()
        {
            Depdendencies = new List<string>();
        }
    }
}
