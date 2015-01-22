using Microsoft.VisualBasic.Devices;
using NetDist.Core;
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
        /// Dictionary which olds information about the known clients
        /// </summary>
        private readonly ConcurrentDictionary<Guid, ExtendedClientInfo> _knownClients;

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
        /// Settings object
        /// </summary>
        private IServerSettings _settings;

        /// <summary>
        /// Constructor
        /// </summary>
        protected ServerBase()
        {
            Logger = new Logger();
            _loadedHandlers = new ConcurrentDictionary<Guid, Tuple<AppDomain, LoadedHandler>>();
            _knownClients = new ConcurrentDictionary<Guid, ExtendedClientInfo>();
        }

        protected void InitializeSettings(IServerSettings settings)
        {
            _settings = settings;
            if (settings.AutoStart)
            {
                Start();
            }
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
            // Client information
            foreach (var kvp in _knownClients)
            {
                var clientInfo = kvp.Value;
                info.Clients.Add(clientInfo);
            }
            return info;
        }

        /// <summary>
        /// Get information about currently registered packages
        /// </summary>
        public List<PackageInfo> GetRegisteredPackages()
        {
            var info = new List<PackageInfo>();
            foreach (var dir in new DirectoryInfo(PackagesFolder).EnumerateDirectories())
            {
                var pi = new PackageInfo();
                pi.PackageName = dir.Name;
                foreach (var file in dir.EnumerateFiles())
                {
                    pi.Files.Add(file.Name);
                }
                info.Add(pi);
            }
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
        public AddJobHandlerResult AddJobHandler(string jobScriptFileContent)
        {
            // Prepare the info object
            var info = new AddJobHandlerResult();

            // Parse the content
            var jobScriptFile = JobScriptFileParser.Parse(jobScriptFileContent);
            if (jobScriptFile.ParsingFailed)
            {
                Logger.Error("Failed to parse job script: {0}", jobScriptFile.ErrorMessage);
                info.SetError(AddJobHandlerErrorReason.ParsingFailed, jobScriptFile.ErrorMessage);
                return info;
            }

            // Add now known package name
            info.PackageName = jobScriptFile.PackageName;

            // Create an additional app-domain
            var domain = AppDomain.CreateDomain(Guid.NewGuid().ToString(), null, new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
                ApplicationName = AppDomain.CurrentDomain.SetupInformation.ApplicationName,
                LoaderOptimization = LoaderOptimization.MultiDomainHost,
                ShadowCopyFiles = "true",
                AppDomainInitializerArguments = null
            });
            // Create a loaded handler wrapper in the new app-domain
            var loadedHandler = (LoadedHandler)domain.CreateInstanceAndUnwrap(typeof(LoadedHandler).Assembly.FullName, typeof(LoadedHandler).FullName, false, BindingFlags.Default, null, new object[] { jobScriptFile, PackagesFolder }, null, null);
            // Create a interchangeable event sink to register cross-domain events
            var sink = new EventSink<LogEventArgs>();
            loadedHandler.RegisterSink(sink);
            sink.NotificationFired += (sender, args) => Logger.Log(args.LogLevel, args.Exception, args.Message);
            // Initialize the handler
            var initResult = loadedHandler.Initialize();
            if (initResult.HasError)
            {
                AppDomain.Unload(domain);
                Logger.Warn("Failed to initialize handler for package: '{0}'", info.PackageName);
                info.SetError(initResult.ErrorReason, initResult.ErrorMessage);
                return info;
            }
            // Fill the info object from the result
            info.HandlerId = initResult.HandlerId;
            info.HandlerName = initResult.HandlerName;
            info.JobName = initResult.JobName;
            // Add the loaded handler to the dictionary
            _loadedHandlers[loadedHandler.Id] = new Tuple<AppDomain, LoadedHandler>(domain, loadedHandler);
            Logger.Info("Added handler: '{0}' ('{1}')", loadedHandler.FullName, loadedHandler.Id);
            return info;
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
            Logger.Info("Client '{0}' requested a job", clientId);
            for (var i = 0; i < 10; i++)
            {
                var handlersWithJobs = _loadedHandlers.Where(x => x.Value.Item2.HasAvailableJobs).ToArray();
                if (handlersWithJobs.Length == 0)
                {
                    return null;
                }
                var nextRandNumber = RandomGenerator.Instance.Next(handlersWithJobs.Length);
                var randomHandler = handlersWithJobs[nextRandNumber];
                var nextJob = randomHandler.Value.Item2.GetNextJob(clientId);
                if (nextJob == null)
                {
                    // Can happen if the queue was empty between the "HasAvailableJobs" check and now
                    // just retry a few times
                    Logger.Debug("Job queue was suddenly empty, try again");
                    continue;
                }
                _knownClients[clientId].JobsInProgress++;
                Logger.Info("Client '{0}' got job '{1}' for handler '{2}'", clientId, nextJob.Id, randomHandler.Value.Item2.FullName);
                return nextJob;
            }
            Logger.Warn("Gave up getting a job");
            return null;
        }

        /// <summary>
        /// Get information for the client for the given handler to execute the job
        /// </summary>
        public HandlerJobInfo GetHandlerJobInfo(Guid handlerId)
        {
            var handler = GetHandler(handlerId);
            if (handler != null)
            {
                var info = handler.GetJobInfo();
                return info;
            }
            return null;
        }

        /// <summary>
        /// Gets a file from the specified handler
        /// </summary>
        public byte[] GetFile(Guid handlerId, string file)
        {
            var handler = GetHandler(handlerId);
            if (handler != null)
            {
                var fileContent = handler.GetFile(file);
                return fileContent;
            }
            return null;
        }

        /// <summary>
        /// Received a result for one of the jobs from a client
        /// </summary>
        public void ReceiveResult(JobResult result)
        {
            if (result == null)
            {
                Logger.Error("Received invalid result");
                return;
            }
            var handler = GetHandler(result.HandlerId);
            if (handler == null)
            {
                Logger.Error("Got result for unknown handler: '{0}'", result.HandlerId);
                return;
            }
            // Forward the result to the handler (also failed ones)
            var success = handler.ReceivedResult(result);

            // Update statistics
            _knownClients[result.ClientId].JobsInProgress--;
            if (success)
            {
                _knownClients[result.ClientId].TotalJobsProcessed++;
            }
            else
            {
                _knownClients[result.ClientId].TotalJobsFailed++;
            }
        }

        /// <summary>
        /// Receive information from the client about the client
        /// </summary>
        public void ReceivedClientInfo(ClientInfo info)
        {
            _knownClients.AddOrUpdate(info.Id, guid => new ExtendedClientInfo
            {
                ClientInfo = info,
                LastCommunicationDate = DateTime.Now
            }, (guid, wrapper) =>
            {
                wrapper.ClientInfo = info;
                wrapper.LastCommunicationDate = DateTime.Now;
                return wrapper;
            });
        }

        /// <summary>
        /// Get the handler for the given id
        /// </summary>
        private LoadedHandler GetHandler(Guid handlerId)
        {
            Tuple<AppDomain, LoadedHandler> handler;
            var hasHandler = _loadedHandlers.TryGetValue(handlerId, out handler);
            return hasHandler ? handler.Item2 : null;
        }
    }
}
