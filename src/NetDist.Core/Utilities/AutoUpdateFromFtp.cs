using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;

namespace NetDist.Core.Utilities
{
    /// <summary>
    /// Implementation for an auto updating executable from an ftp
    /// The version check is done by a "version.txt" file. This should also always be included in the new version.
    /// Last updated: 05.02.2015
    /// </summary>
    public class AutoUpdateFromFtp : AutoUpdateBase
    {
        private readonly string _ftpAddress;
        private readonly Action _terminateAction;
        private readonly NetworkCredential _credentials;
        private const string VersionFileName = "version.txt";
        private const string VersionZipFileName = "version.zip";

        public AutoUpdateFromFtp(string ftpAddress, string ftpUsername, string ftpPassword, Action terminateAction, params string[] excludeFileNames)
            : base(excludeFileNames)
        {
            _ftpAddress = ftpAddress;
            _terminateAction = terminateAction;
            _credentials = new NetworkCredential(ftpUsername, ftpPassword);
        }

        public override string GetCurrentVersion()
        {
            var versionFile = Path.Combine(Directory.GetCurrentDirectory(), VersionFileName);
            string versionString = "0";
            if (File.Exists(versionFile))
            {
                versionString = File.ReadLines(versionFile).FirstOrDefault();
            }
            return versionString;
        }

        protected override void Terminate()
        {
            _terminateAction();
        }

        protected override string GetNewestVersion()
        {
            var url = String.Format("{0}/{1}", _ftpAddress, VersionFileName);
            var webClient = new WebClient { Credentials = _credentials };
            try
            {
                var result = webClient.DownloadString(url);
                string line1 = result.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                return line1;
            }
            catch (WebException ex)
            {
                return "0";
            }
        }

        protected override void PrepareNewVersion(string preparationDirectoryPath)
        {
            var zipFileName = Path.Combine(preparationDirectoryPath, VersionZipFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(zipFileName) ?? String.Empty);
            var webClient = new WebClient { Credentials = _credentials };
            var url = String.Format("{0}/{1}", _ftpAddress, VersionZipFileName);
            webClient.DownloadFile(url, zipFileName);
            // Extract into the given folder
            ZipFile.ExtractToDirectory(zipFileName, preparationDirectoryPath);
            File.Delete(zipFileName);
        }
    }
}
