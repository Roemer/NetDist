using NetDist.Core.Utilities;
using NetDist.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
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
        protected string HandlersFolder { get { return Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), "handlers"); } }

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
                handler.Value.Item2.StopJoblogic();
            }
        }

        public bool AddHandler(byte[] zipcontent)
        {
            new ZipUtility().Extract(zipcontent, HandlersFolder);
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
                AppDomainInitializerArguments = null
            });
            // Create a loaded handler wrapper in the new app-domain
            var loadedHandler = (LoadedHandler)domain.CreateInstanceAndUnwrap(typeof(LoadedHandler).Assembly.FullName, typeof(LoadedHandler).FullName, false, BindingFlags.Default, null, new object[] { jobLogicFile.HandlerSettingsString }, null, null);
            var success = loadedHandler.InitializeHandler(HandlersFolder, jobLogicFile.HandlerCustomSettingsString);
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

        public void StartJobLogic(Guid id)
        {
            Logger.Info("Starting Handler: '{0}'", id);
            if (_loadedHandlers.ContainsKey(id))
            {
                _loadedHandlers[id].Item2.StartJobLogic();
            }
        }

        public void StopJoblogic(Guid id)
        {
            Logger.Info("Stopping Handler: '{0}'", id);
            if (_loadedHandlers.ContainsKey(id))
            {
                _loadedHandlers[id].Item2.StopJoblogic();
            }
        }
    }
}
