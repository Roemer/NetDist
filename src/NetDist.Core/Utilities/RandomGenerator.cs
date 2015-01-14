using System;
using System.Threading;

namespace NetDist.Core.Utilities
{
    /// <summary>
    /// Threadsafe random generator
    /// </summary>
    public static class RandomGenerator
    {
        private static readonly object LockObject = new object();
        private static readonly Random SeedRandom = new Random();
        private static readonly ThreadLocal<Random> Random = new ThreadLocal<Random>(GenerateRandom);

        public static Random Instance { get { return Random.Value; } }

        public static Random GenerateRandom()
        {
            lock (LockObject)
            {
                return new Random(SeedRandom.Next());
            }
        }

        public static Int64 NextInt64(this Random rnd, Int64 min = 0, Int64 max = Int64.MaxValue)
        {
            var buffer = new byte[sizeof(Int64)];
            rnd.NextBytes(buffer);
            var rndValue = BitConverter.ToInt64(buffer, 0);
            return (Math.Abs(rndValue % (max - min)) + min);
        }

        public static UInt64 NextUInt64(this Random rnd, UInt64 min = 0, UInt64 max = UInt64.MaxValue)
        {
            var buffer = new byte[sizeof(UInt64)];
            rnd.NextBytes(buffer);
            var rndValue = BitConverter.ToUInt64(buffer, 0);
            return ((rndValue % (max - min)) + min);
        }
    }
}
