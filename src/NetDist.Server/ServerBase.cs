using Microsoft.VisualBasic.Devices;
using NetDist.Core;
using NetDist.Core.Extensions;
using NetDist.Core.Utilities;
using NetDist.Jobs;
using NetDist.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NetDist.Server
{
    /// <summary>
    /// Abstract class for server implementations
    /// </summary>
    public abstract class ServerBase
    {
        /// <summary>
        /// Logger object
        /// </summary>
        public Logger Logger { get; private set; }

        /// <summary>
        /// Dictionary which holds all currently loaded handlers
        /// </summary>
        private readonly ConcurrentDictionary<Guid, Tuple<AppDomain, LoadedHandler>> _loadedHandlers;

        /// <summary>
        /// Abstract method to start the server
        /// </summary>
        protected abstract bool InternalStart();

        /// <summary>
        /// Abstract method to stop the server
        /// </summary>
        protected abstract bool InternalStop();

        /// <summary>
        /// Path to the handlers folder
        /// </summary>
        protected string PackagesFolder { get { return Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), "packages"); } }

        /// <summary>
        /// Constructor
        /// </summary>
        protected ServerBase()
        {
            Logger = new Logger();
            _loadedHandlers = new ConcurrentDictionary<Guid, Tuple<AppDomain, LoadedHandler>>();
        }

        /// <summary>
        /// Starts the server
        /// </summary>
        public void Start()
        {
            var success = InternalStart();
            if (!success)
            {
                Logger.Info("Server failed to start");
            }
        }

        /// <summary>
        /// Stops the server and all handlers
        /// </summary>
        public void Stop()
        {
            var success = InternalStop();
            if (!success)
            {
                Logger.Info("Server failed to stop");
            }
            // Stop the handlers
            foreach (var handler in _loadedHandlers)
            {
                StopJobHandler(handler.Value.Item2.Id);
            }
        }

        /// <summary>
        /// Get statistics about the server and the job handlers
        /// </summary>
        public ServerInfo GetStatistics()
        {
            var info = new ServerInfo();
            // RAM information
            var ci = new ComputerInfo();
            info.TotalMemory = ci.TotalPhysicalMemory;
            info.UsedMemory = ci.TotalPhysicalMemory - ci.AvailablePhysicalMemory;
            // CPU information
            info.CpuUsage = CpuUsageReader.GetValue();
            // Handler information
            foreach (var kvp in _loadedHandlers)
            {
                var loadedHandler = kvp.Value.Item2;
                info.Handlers.Add(loadedHandler.GetInfo());
            }
            return info;
        }

        /// <summary>
        /// Get information about currently registered packages
        /// </summary>
        public List<PackageInfo> GetRegisteredPackages()
        {
            var info = new List<PackageInfo>();
            return info;
        }

        /// <summary>
        /// Register a new package (handlers, dependencies, ...)
        /// </summary>
        public bool RegisterPackage(byte[] zipcontent)
        {
            ZipUtility.ZipExtractToDirectory(zipcontent, PackagesFolder, true);
            Logger.Info("Registered new package");
            return true;
        }

        /// <summary>
        /// Called when a new job handler is added
        /// Initializes and starts the appropriate handler
        /// </summary>
        /// <param name="jobScriptFileContent">The full content of the job script file</param>
        public bool AddJobHandler(string jobScriptFileContent)
        {
            // Parse the content
            var jobScriptFile = JobScriptFileParser.Parse(jobScriptFileContent);
            if (jobScriptFile.ParsingFailed)
            {
                Logger.Error("Failed to parse job script: {0}", jobScriptFile.ErrorMessage);
                return false;
            }

            // Create an additional app-domain
            var domain = AppDomain.CreateDomain(Guid.NewGuid().ToString(), null, new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
                ApplicationName = AppDomain.CurrentDomain.SetupInformation.ApplicationName,
                LoaderOptimization = LoaderOptimization.MultiDomainHost,
                ShadowCopyFiles = "true",
                AppDomainInitializerArguments = null,
                //TODO: needed for folders? ShadowCopyDirectories = AppDomain.CurrentDomain.SetupInformation.ApplicationBase
            });
            // Create a loaded handler wrapper in the new app-domain
            var loadedHandler = (LoadedHandler)domain.CreateInstanceAndUnwrap(typeof(LoadedHandler).Assembly.FullName, typeof(LoadedHandler).FullName, false, BindingFlags.Default, null, new[] { jobScriptFile.PackageName }, null, null);
            // Initialize the handler
            var success = loadedHandler.Initialize(jobScriptFile, PackagesFolder);
            if (!success)
            {
                AppDomain.Unload(domain);
                Logger.Warn("Failed to initialize handler: '{0}", loadedHandler.FullName);
                return false;
            }
            // Add the loaded handler to the dictionary
            _loadedHandlers[loadedHandler.Id] = new Tuple<AppDomain, LoadedHandler>(domain, loadedHandler);
            Logger.Info("Added handler: '{0}' ('{1}')", loadedHandler.FullName, loadedHandler.Id);
            return true;
        }

        /// <summary>
        /// Stops and removes a job handler
        /// </summary>
        public bool RemoveJobHandler(Guid handlerId)
        {
            Tuple<AppDomain, LoadedHandler> removedItem;
            var success = _loadedHandlers.TryRemove(handlerId, out removedItem);
            if (success)
            {
                var handlerName = removedItem.Item2.FullName;
                // Stop the handler
                removedItem.Item2.StopJobHandler();
                // Unload the domain
                AppDomain.Unload(removedItem.Item1);
                Logger.Info("Removed handler: '{0}' ('{1}')", handlerName, handlerId);
            }
            return success;
        }

        /// <summary>
        /// Starts the handler so jobs are being distributed
        /// </summary>
        public bool StartJobHandler(Guid id)
        {
            Logger.Info("Starting Handler: '{0}'", id);
            if (_loadedHandlers.ContainsKey(id))
            {
                _loadedHandlers[id].Item2.StartJobHandler();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Stops a handler so no more jobs are distributed and processed
        /// </summary>
        public bool StopJobHandler(Guid id)
        {
            if (_loadedHandlers.ContainsKey(id))
            {
                var loadedHandler = _loadedHandlers[id].Item2;
                var handlerStopped = loadedHandler.StopJobHandler();
                if (handlerStopped)
                {
                    Logger.Info("Stopped Handler: '{0}' ('{1}')", loadedHandler.FullName, id);
                }
                return handlerStopped;
            }
            return false;
        }

        /// <summary>
        /// Get a job from the current pending jobs in the handlers
        /// </summary>
        public Job GetJob(Guid clientId)
        {
            Logger.Info("Client '{0}' requested a job", "TODO");
            lock (_loadedHandlers.GetSyncRoot())
            {
                var handlersWithJobs = _loadedHandlers.Where(x => x.Value.Item2.HasAvailableJobs).ToArray();
                if (handlersWithJobs.Length == 0)
                {
                    return null;
                }
                var nextRandNumber = RandomGenerator.Instance.Next(handlersWithJobs.Length);
                var randomHandler = handlersWithJobs[nextRandNumber];
                var nextJob = randomHandler.Value.Item2.GetNextJob(clientId);
                Logger.Info("Client '{0}' got job '{1}' for handler '{2}'", "client.Id", nextJob.Id, randomHandler.Value.Item2.HandlerSettings.HandlerName);
                return nextJob;
            }
        }

        /// <summary>
        /// Received a result for one of the jobs from a client
        /// </summary>
        public void ReceiveResult(JobResult result)
        {
            lock (_loadedHandlers.GetSyncRoot())
            {
                // Search the appropriate handler
                var handlerId = result.HandlerId;
                if (!_loadedHandlers.ContainsKey(handlerId))
                {
                    Logger.Error("Got result for unknown handler: '{0}'", handlerId);
                    return;
                }
                var handler = _loadedHandlers[handlerId];
                // Forward the result to the handler
                handler.Item2.ReceivedResult(result);
            }
        }
    }
}
