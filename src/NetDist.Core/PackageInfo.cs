using System.Collections.Generic;

namespace NetDist.Core
{
    public class PackageInfo
    {
        public string PackageName { get; set; }
        public List<string> Handlers { get; set; }

        public PackageInfo()
        {
            Handlers = new List<string>();
        }
    }
}
