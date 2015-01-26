using System.Collections.Generic;

namespace NetDist.Core
{
    public class PackageInfo
    {
        public string PackageName { get; set; }
        public List<string> HandlerAssemblies { get; set; }
        public List<string> Dependencies { get; set; }

        public PackageInfo()
        {
            HandlerAssemblies = new List<string>();
            Dependencies = new List<string>();
        }
    }
}
