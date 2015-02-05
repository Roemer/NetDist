using NetDist.Core;
using System;
using System.IO;
using System.Reflection;

namespace NetDist.Server
{
    /// <summary>
    /// Helper class to manage the packages
    /// </summary>
    public class PackageManager
    {
        public string PackageBaseFolder { get; private set; }

        public PackageManager(string packagesFolder)
        {
            if (!Path.IsPathRooted(packagesFolder))
            {
                packagesFolder = Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), packagesFolder);
            }
            PackageBaseFolder = packagesFolder;
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
            return Path.Combine(PackageBaseFolder, packageName);
        }

        private string BuildInfoFileName(string packageName)
        {
            return Path.Combine(PackageBaseFolder, packageName) + ".json";
        }
    }
}
