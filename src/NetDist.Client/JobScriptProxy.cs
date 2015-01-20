using NetDist.Jobs;
using System;
using System.Reflection;

namespace NetDist.Client
{
    /// <summary>
    /// Proxy class to load job assembly and process a job
    /// </summary>
    public class JobScriptProxy : MarshalByRefObject
    {
        public string RunJob(string assemblyPath, string input)
        {
            // Load library
            var loadedJobAssembly = AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(assemblyPath));

            // Search and instantiate job script
            Type jobScriptType = null;
            foreach (var type in loadedJobAssembly.GetTypes())
            {
                if (typeof(IJobScript).IsAssignableFrom(type))
                {
                    jobScriptType = type;
                    break;
                }
            }
            // Initialize the job
            var jobInstance = (IJobScript)Activator.CreateInstance(jobScriptType);
            // Run the job logic
            var result = jobInstance.Process(input);
            return result;
        }
    }
}
