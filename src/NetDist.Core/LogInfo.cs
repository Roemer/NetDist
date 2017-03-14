using System;
using System.Collections.Generic;

namespace NetDist.Core
{
    [Serializable]
    public class LogInfo
    {
        public List<LogInfoEntry> LogEntries { get; set; }
    }

    [Serializable]
    public class LogInfoEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
    }
}
