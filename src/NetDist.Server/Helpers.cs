using System;

namespace NetDist.Server
{
    /// <summary>
    /// Various tiny helpers
    /// </summary>
    public static class Helpers
    {
        public static string BuildFullName(string packageName, string handlerName, string jobName)
        {
            return String.Format("{0}/{1}/{2}", packageName, handlerName, jobName);
        }
    }
}
