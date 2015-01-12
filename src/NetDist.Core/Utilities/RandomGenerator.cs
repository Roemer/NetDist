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
    }
}
