using System;

namespace DependencySample.ServerDependency
{
    public static class StringGenerator
    {
        private static readonly Random Rnd = new Random();

        public static string GetString()
        {
            var list = new[] { "House", "Mouse", "Car" };
            var r = Rnd.Next(list.Length);
            return list[r];
        }
    }
}
