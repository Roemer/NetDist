using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace NetDist.Core.Utilities
{
    /// <summary>
    /// Utility functions to handle zip/gzips without 3rd party dependencies
    /// Last updated: 15.01.2015
    /// </summary>
    public static class ZipUtility
    {
        # region Zip
        /// <summary>
        /// Compresses a folder to a zip file
        /// </summary>
        public static void ZipCompressFolderToFile(string sourceDirectoryName, string destinationArchiveFileName, CompressionLevel compressionLevel = CompressionLevel.Optimal, bool includeBaseDirectory = false)
        {
            ZipFile.CreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, compressionLevel, includeBaseDirectory);
        }

        /// <summary>
        /// Compress a folder to a byte array
        /// </summary>
        public static byte[] ZipCompressFolderToBytes(string sourceDirectoryName, CompressionLevel compressionLevel = CompressionLevel.Optimal, bool includeBaseDirectory = false)
        {
            sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);
            using (var ms = new MemoryStream())
            {
                using (var zipArchive = new ZipArchive(ms, ZipArchiveMode.Create, false))
                {
                    var archiveIsEmpty = true;
                    var directoryInfo = new DirectoryInfo(sourceDirectoryName);
                    var fullName = directoryInfo.FullName;
                    if (includeBaseDirectory && directoryInfo.Parent != null)
                    {
                        fullName = directoryInfo.Parent.FullName;
                    }
                    foreach (var current in directoryInfo.EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
                    {
                        archiveIsEmpty = false;
                        var length = current.FullName.Length - fullName.Length;
                        var entryName = current.FullName.Substring(fullName.Length, length);
                        entryName = entryName.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        if (current is FileInfo)
                        {
                            // Process file
                            var sourceFileName = current.FullName;
                            zipArchive.CreateEntryFromFile(sourceFileName, entryName, compressionLevel);
                        }
                        else
                        {
                            // Process directory
                            var directoryInfo2 = current as DirectoryInfo;
                            if (directoryInfo2 != null && IsDirectoryEmpty(directoryInfo2.FullName))
                            {
                                zipArchive.CreateEntry(entryName + Path.DirectorySeparatorChar, compressionLevel);
                            }
                        }
                    }
                    if (includeBaseDirectory && archiveIsEmpty)
                    {
                        // Make sure the base directory exists if the zip is empty
                        zipArchive.CreateEntry(directoryInfo.Name + Path.DirectorySeparatorChar);
                    }
                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// Compresses a file to a zip file
        /// </summary>
        public static void ZipCompressFileToFile(string sourceFileName, string destinationArchiveFileName, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            var f = new FileInfo(sourceFileName);
            using (var zipArchive = ZipFile.Open(destinationArchiveFileName, ZipArchiveMode.Create))
            {
                zipArchive.CreateEntryFromFile(f.FullName, f.Name, compressionLevel);
            }
        }

        /// <summary>
        /// Compress a list of files to a byte array
        /// </summary>
        public static byte[] ZipCompressFilesToBytes(IEnumerable<string> sourceFileNames, CompressionLevel compressionLevel = CompressionLevel.Optimal, string baseDirectory = null)
        {
            using (var ms = new MemoryStream())
            {
                using (var zipArchive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    foreach (var file in sourceFileNames)
                    {
                        var f = new FileInfo(file);
                        var entryName = baseDirectory == null ? f.Name : Path.Combine(baseDirectory, f.Name);
                        zipArchive.CreateEntryFromFile(f.FullName, entryName, compressionLevel);
                    }
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Compress a file to a byte array
        /// </summary>
        public static byte[] ZipCompressFileToBytes(string sourceFileName, CompressionLevel compressionLevel = CompressionLevel.Optimal, string baseDirectory = null)
        {
            return ZipCompressFilesToBytes(new[] { sourceFileName }, compressionLevel, baseDirectory);
        }

        /// <summary>
        /// Compress a byte array to a byte array
        /// </summary>
        public static byte[] ZipCompressBytesToBytes(byte[] sourceData, string entryName, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            using (var ms = new MemoryStream())
            {
                using (var zipArchive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    var zipEntry = zipArchive.CreateEntry(entryName, compressionLevel);
                    using (var zipEntryStream = zipEntry.Open())
                    {
                        zipEntryStream.Write(sourceData, 0, sourceData.Length);
                    }
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Extract a file to a folder
        /// </summary>
        public static void ZipExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName)
        {
            ZipFile.ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName);
        }

        /// <summary>
        /// Extract a byte array to a folder
        /// </summary>
        public static void ZipExtractToDirectory(byte[] zipData, string destinationDirectoryName, bool overwrite = false)
        {
            using (var ms = new MemoryStream(zipData))
            {
                using (var zipArchive = new ZipArchive(ms, ZipArchiveMode.Read, false))
                {
                    if (!overwrite)
                    {
                        // Use the official helper
                        zipArchive.ExtractToDirectory(destinationDirectoryName);
                        return;
                    }
                    // Manually extract
                    foreach (var zipArchiveEntry in zipArchive.Entries)
                    {
                        var fullPath = Path.Combine(destinationDirectoryName, zipArchiveEntry.FullName);
                        if (zipArchiveEntry.Name == "")
                        {
                            // Looks like a directory
                            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                            continue;
                        }
                        // Make sure the directory to the file exists anyway...
                        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                        // Extract the file
                        zipArchiveEntry.ExtractToFile(fullPath, true);
                    }
                }
            }
        }
        #endregion

        #region GZip

        public static void GZipCompressFile(byte[] sourceData, string destinationArchiveFileName)
        {
            var compressedData = GZipCompress(sourceData);
            File.WriteAllBytes(destinationArchiveFileName, compressedData);
        }

        public static byte[] GZipCompress(byte[] sourceData)
        {
            using (var ms = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    gzipStream.Write(sourceData, 0, sourceData.Length);
                }
                return ms.ToArray();
            }
        }

        public static void GZipExtractToFile(byte[] gzipData, string destinationFileName)
        {
            var decodedData = GZipExtract(gzipData);
            File.WriteAllBytes(destinationFileName, decodedData);
        }

        public static string GZipExtractToString(byte[] gzipData, Encoding encoding = null)
        {
            var decodedData = GZipExtract(gzipData);
            return (encoding ?? Encoding.Default).GetString(decodedData);
        }

        public static byte[] GZipExtract(byte[] gzipData)
        {
            using (var gzipStream = new GZipStream(new MemoryStream(gzipData), CompressionMode.Decompress))
            {
                using (var ms = new MemoryStream())
                {
                    gzipStream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }
        #endregion

        #region Helpers
        private static bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }
        #endregion
    }
}
