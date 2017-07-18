using System;

namespace NetDist.Core
{
    [Serializable]
    public class JobScriptInfo
    {
        public string JobScript { get; set; }

        public bool IsDisabled { get; set; }

        public bool AddedScriptFromSaved { get; set; }
    }
}
