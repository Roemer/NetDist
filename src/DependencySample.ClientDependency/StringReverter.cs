using System;
using System.Linq;

namespace DependencySample.ClientDependency
{
    public static class StringReverter
    {
        public static string ReverseString(string value)
        {
            return String.IsNullOrWhiteSpace(value) ? value : new String(value.Reverse().ToArray());
        }
    }
}
