using System;
using System.Collections.Generic;

namespace NetDist.Server
{
    [Serializable]
    public class JobScriptFile
    {
        public bool ParsingFailed { get; private set; }
        public string ErrorMessage { get; private set; }
        public List<string> CompilerLibraries { get; set; }
        public List<string> Dependencies { get; set; }
        public string PackageName { get; set; }
        public string JobScript { get; set; }

        public JobScriptFile()
        {
            CompilerLibraries = new List<string>();
            Dependencies = new List<string>();
        }

        public void SetError(string errorMessage)
        {
            ParsingFailed = true;
            ErrorMessage = errorMessage;
        }
    }
}
