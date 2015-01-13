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
using System.Threading.Tasks;

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
            Parallel.ForEach(_loadedHandlers, kvp => kvp.Value.Item2.StopJoblogic());
        }

        /// <summary>
        /// Get statistics about the server and the job-logics
        /// </summary>
        public ServerInfo GetStatistics()
        {
            var info = new ServerInfo();
            // RAM information
            var ci = new ComputerInfo();
            info.TotalMemory = ci.TotalPhysicalMemory;
            info.UsedMemory = ci.TotalPhysicalMemory - ci.AvailablePhysicalMemory;
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
            new ZipUtility().Extract(zipcontent, PackagesFolder);
            return true;
        }

        /// <summary>
        /// Called when a new job-logic is added
        /// Initializes and starts the appropriate handler
        /// </summary>
        /// <param name="jobLogicFileContent">The full content of the job logic file</param>
        public bool AddJobLogic(string jobLogicFileContent)
        {
            // Parse the content
            var jobLogicFile = JobLogicFileParser.ParseJob(jobLogicFileContent);
            if (jobLogicFile.ParsingFailed)
            {
                Logger.Error("Failed to parse job: {0}", jobLogicFile.ErrorMessage);
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
            var loadedHandler = (LoadedHandler)domain.CreateInstanceAndUnwrap(typeof(LoadedHandler).Assembly.FullName, typeof(LoadedHandler).FullName, false, BindingFlags.Default, null, new object[] { jobLogicFile.HandlerSettingsString }, null, null);
            var success = loadedHandler.InitializeHandler(PackagesFolder, jobLogicFile.HandlerCustomSettingsString);
            if (!success)
            {
                AppDomain.Unload(domain);
                Logger.Warn("Failed to add handler: '{0}", loadedHandler.FullName);
                return false;
            }
            // Add the loaded handler to the dictionary
            _loadedHandlers[loadedHandler.Id] = new Tuple<AppDomain, LoadedHandler>(domain, loadedHandler);
            Logger.Info("Added handler: '{0}' ('{1}')", loadedHandler.FullName, loadedHandler.Id);
            return true;
        }

        /// <summary>
        /// Stops and removes a job-logic
        /// </summary>
        public bool RemoveJobLogic(Guid handlerId)
        {
            Tuple<AppDomain, LoadedHandler> removedItem;
            var success = _loadedHandlers.TryRemove(handlerId, out removedItem);
            if (success)
            {
                // Stop the handler
                removedItem.Item2.StopJoblogic();
                // Unload the domain
                AppDomain.Unload(removedItem.Item1);
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
                _loadedHandlers[id].Item2.StartJobLogic();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Stops a handler so no more jobs are distributed and processed
        /// </summary>
        public bool StopJobHandler(Guid id)
        {
            Logger.Info("Stopping Handler: '{0}'", id);
            if (_loadedHandlers.ContainsKey(id))
            {
                _loadedHandlers[id].Item2.StopJoblogic();
                return true;
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
