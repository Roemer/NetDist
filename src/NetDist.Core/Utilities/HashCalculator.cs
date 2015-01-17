using System.Security.Cryptography;
using System.Text;

namespace NetDist.Core.Utilities
{
    public static class HashCalculator
    {
        public static string CalculateMd5Hash(string value)
        {
            var data = Encoding.ASCII.GetBytes(value);
            return CalculateMd5Hash(data);
        }

        public static string CalculateMd5Hash(byte[] data)
        {
            // Create the Provider
            var md5 = MD5.Create();
            // Calculate the Hash
            var hash = md5.ComputeHash(data);
            // Convert the Hash to a Hex String
            var sb = new StringBuilder();
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
