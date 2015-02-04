using NetDist.Jobs;
using System;
using System.Reflection;

namespace NetDist.Server
{
    /// <summary>
    /// Reads the handler settings out of a job assembly file
    /// Uses an own app-domain
    /// </summary>
    public static class JobFileHandlerSettingsReader
    {
        /// <summary>
        /// Loads the assembly into the current domain and reads the handler and custom settings
        /// </summary>
        public static bool LoadAssemblyAndReadSettings(string jobAssemblyPath, out HandlerSettings handlerSettings, out IHandlerCustomSettings customSettings)
        {
            handlerSettings = null;
            customSettings = null;
            var loader = new JobAssemblyLoader();
            var loadingSuccess = loader.LoadAssembly(jobAssemblyPath);
            if (!loadingSuccess)
            {
                return false;
            }
            handlerSettings = loader.GetHandlerSettings();
            customSettings = loader.GetCustomSettings();
            return true;
        }

        /// <summary>
        /// Reads the handler settings on an own app domain which is unloaded afterwards
        /// </summary>
        public static bool ReadSettingsInOwnDomain(string jobAssemblyPath, out HandlerSettings handlerSettings)
        {
            handlerSettings = null;

            var domain = AppDomain.CreateDomain(Guid.NewGuid().ToString(), null, new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
                ApplicationName = AppDomain.CurrentDomain.SetupInformation.ApplicationName,
                LoaderOptimization = LoaderOptimization.MultiDomainHost,
                ShadowCopyFiles = "true",
                AppDomainInitializerArguments = null
            });

            var proxy = (JobAssemblyLoader)domain.CreateInstanceAndUnwrap(typeof(JobAssemblyLoader).Assembly.FullName, typeof(JobAssemblyLoader).FullName);
            var loadingSuccess = proxy.LoadAssembly(jobAssemblyPath);
            if (!loadingSuccess)
            {
                return false;
            }
            handlerSettings = proxy.GetHandlerSettings();
            AppDomain.Unload(domain);
            return true;
        }

        /// <summary>
        /// Helper class which does the actual loading of the job assembly
        /// </summary>
        private class JobAssemblyLoader : MarshalByRefObject
        {
            private HandlerSettings _handlerSettings;
            private IHandlerCustomSettings _customSettings;

            public bool LoadAssembly(string jobScriptAssemblyPath)
            {
                var jobScriptAssembly = AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(jobScriptAssemblyPath));

                Type handlerInitializerType = null;
                foreach (var type in jobScriptAssembly.GetTypes())
                {
                    if (typeof(IHandlerInitializer).IsAssignableFrom(type))
                    {
                        handlerInitializerType = type;
                        break;
                    }
                }

                if (handlerInitializerType == null)
                {
                    return false;
                }

                // Initialize the handler initializer
                var jobInstance = (IHandlerInitializer)Activator.CreateInstance(handlerInitializerType);
                // Read the settings
                _handlerSettings = jobInstance.GetHandlerSettings();
                _customSettings = jobInstance.GetCustomHandlerSettings();
                return true;
            }

            public HandlerSettings GetHandlerSettings()
            {
                return _handlerSettings;
            }

            public IHandlerCustomSettings GetCustomSettings()
            {
                return _customSettings;
            }

            public override object InitializeLifetimeService()
            {
                return null;
            }
        }
    }
}
