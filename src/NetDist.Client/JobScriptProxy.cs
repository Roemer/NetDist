using NetDist.Jobs;
using System;
using System.Reflection;
using NetDist.Jobs.DataContracts;

namespace NetDist.Client
{
    /// <summary>
    /// Proxy class to load job assembly and process a job
    /// </summary>
    public class JobScriptProxy : MarshalByRefObject
    {
        public JobResult RunJob(Guid clientId, Job job, string assemblyPath)
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
            var result = jobInstance.Process(job, clientId);
            return result;
        }
    }
}
