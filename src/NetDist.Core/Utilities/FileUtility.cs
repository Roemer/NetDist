using System.IO;
using System.Threading;

namespace NetDist.Core.Utilities
{
    public static class FileUtility
    {
        public static FileStream WaitForFile(string fullPath, FileMode mode, FileAccess access, FileShare share)
        {
            for (var numTries = 0; numTries < 10; numTries++)
            {
                try
                {
                    var fs = new FileStream(fullPath, mode, access, share);
                    fs.ReadByte();
                    fs.Seek(0, SeekOrigin.Begin);
                    return fs;
                }
                catch (IOException)
                {
                    Thread.Sleep(250);
                }
            }

            return null;
        }
    }
}
