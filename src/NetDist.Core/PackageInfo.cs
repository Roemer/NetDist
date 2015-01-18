using System.Collections.Generic;

namespace NetDist.Core
{
    public class PackageInfo
    {
        public string PackageName { get; set; }
        public List<string> Files { get; set; }

        public PackageInfo()
        {
            Files = new List<string>();
        }
    }
}
