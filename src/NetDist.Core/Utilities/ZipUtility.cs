using System;
using System.IO;
using System.IO.Compression;

namespace NetDist.Core.Utilities
{
    public class ZipUtility
    {
        public void Extract(byte[] data, string destinationFolder)
        {
            var des = new DirectoryInfo(destinationFolder);
            des.Create();
            using (var ms = new MemoryStream(data))
            {
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Read, false))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.Name == "")
                        {
                            // It's a folder, create it (recursively)
                            var folder = des;
                            var pathParts = entry.FullName.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var dir in pathParts)
                            {
                                folder = folder.CreateSubdirectory(dir);
                            }
                        }
                        else
                        {
                            // It's a file, save it
                            using (var fileData = entry.Open())
                            {
                                using (var fileStream = File.Create(Path.Combine(des.FullName, entry.FullName)))
                                {
                                    fileData.CopyTo(fileStream);
                                }
                            }
                        }
                    }
                }
            }
        }

        public byte[] Compress(string file)
        {
            var f = new FileInfo(file);
            using (var ms = new MemoryStream())
            {
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    var fileContent = File.ReadAllBytes(f.FullName);
                    var zipEntry = archive.CreateEntry(f.Name);
                    using (var entryStream = zipEntry.Open())
                    {
                        entryStream.Write(fileContent, 0, fileContent.Length);
                    }
                }
                return ms.ToArray();
            }
        }
    }
}
