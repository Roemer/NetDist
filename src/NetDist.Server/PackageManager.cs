using NetDist.Core;
using System.IO;

namespace NetDist.Server
{
    public class PackageManager
    {
        private readonly string _packagesFolder;

        public PackageManager(string packagesFolder)
        {
            _packagesFolder = packagesFolder;
        }

        public void Save(PackageInfo packageInfo)
        {
            var serializedInfo = JobObjectSerializer.Serialize(packageInfo);
            File.WriteAllText(BuildFileName(packageInfo.PackageName), serializedInfo);
        }

        public PackageInfo Get(string packageName)
        {
            var content = File.ReadAllText(BuildFileName(packageName));
            return JobObjectSerializer.Deserialize<PackageInfo>(content);
        }

        private string BuildFileName(string packageName)
        {
            return Path.Combine(_packagesFolder, packageName) + ".json";
        }
    }
}
