using NetDist.Core;
using System.IO;

namespace NetDist.Server
{
    /// <summary>
    /// Helper class to manage the packages
    /// </summary>
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
            File.WriteAllText(BuildInfoFileName(packageInfo.PackageName), serializedInfo);
        }

        public PackageInfo GetInfo(string packageName)
        {
            var content = File.ReadAllText(BuildInfoFileName(packageName));
            return JobObjectSerializer.Deserialize<PackageInfo>(content);
        }

        public string GetPackagePath(string packageName)
        {
            return BuildPackageFolderPath(packageName);
        }

        public byte[] GetFile(string packageName, string fileName)
        {
            var fullPath = Path.Combine(BuildPackageFolderPath(packageName), fileName);
            if (!File.Exists(fullPath)) { return null; }
            var content = File.ReadAllBytes(fullPath);
            return content;
        }

        private string BuildPackageFolderPath(string packageName)
        {
            return Path.Combine(_packagesFolder, packageName);
        }

        private string BuildInfoFileName(string packageName)
        {
            return Path.Combine(_packagesFolder, packageName) + ".json";
        }
    }
}
