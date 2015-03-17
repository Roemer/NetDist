using System.Linq;
using Microsoft.VisualBasic.Devices;
using NetDist.Core;
using NetDist.Core.Utilities;
using NetDist.Jobs.DataContracts;
using NetDist.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace NetDist.Server
{
    /// <summary>
    /// Abstract class for server implementations
    /// </summary>
    public abstract class ServerBase<TSet> where TSet : IServerSettings
    {
        /// <summary>
        /// Logger object
        /// </summary>
        public Logger Logger { get; private set; }

        /// <summary>
        /// Settings object
        /// </summary>
        protected TSet Settings { get; private set; }

        /// <summary>
        /// Abstract method to start the server
        /// </summary>
        protected abstract bool InternalStart();

        /// <summary>
        /// Abstract method to stop the server
        /// </summary>
        protected abstract bool InternalStop();

        private readonly PackageManager _packageManager;
        private readonly HandlerManager _handlerManager;
        private readonly ClientManager _clientManager;

        /// <summary>
        /// Constructor
        /// </summary>
        protected ServerBase(TSet settings, params EventHandler<LogEventArgs>[] defaultLogHandlers)
        {
            Settings = settings;
            // Initialize the logger
            Logger = new Logger();
            foreach (var logEvent in defaultLogHandlers)
            {
                Logger.LogEvent += logEvent;
            }
            // Initialize others
            _packageManager = new PackageManager(Settings.PackagesFolder);
            _handlerManager = new HandlerManager(Logger, _packageManager);
            _clientManager = new ClientManager();
            // Make sure the packages folder exists
            Directory.CreateDirectory(Settings.PackagesFolder);

            // Autostart if wanted
            if (Settings.AutoStart)
            {
                Start();
            }
        }

        /// <summary>
        /// Starts the server
        /// </summary>
        public void Start()
        {
            try
            {
                var success = InternalStart();
                if (!success)
                {
                    Logger.Error("Failed to start");
                }
                _handlerManager.StartSchedulerTask();
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Failed to start");
            }
        }

        /// <summary>
        /// Stops the server and all handlers
        /// </summary>
        public void Stop()
        {
            try
            {
                var success = InternalStop();
                if (!success)
                {
                    Logger.Error("Failed to stop");
                }
                _handlerManager.TearDown();
                _clientManager.Clear();
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Failed to stop");
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
            // Handler statistics
            var handlerStats = _handlerManager.GetStatistics();
            info.Handlers.AddRange(handlerStats);
            // Client statistics
            info.Clients.AddRange(_clientManager.GetStatistics().OrderBy(i => i.ClientInfo.Name));
            return info;
        }

        /// <summary>
        /// Get information about currently registered packages
        /// </summary>
        public List<PackageInfo> GetRegisteredPackages()
        {
            var info = new List<PackageInfo>();
            foreach (var file in new DirectoryInfo(Settings.PackagesFolder).EnumerateFiles())
            {
                info.Add(_packageManager.GetInfo(Path.GetFileNameWithoutExtension(file.FullName)));
            }
            return info;
        }

        /// <summary>
        /// Register a package (handlers, dependencies, ...)
        /// </summary>
        public bool RegisterPackage(PackageInfo packageInfo, byte[] zipcontent)
        {
            // Save package information
            _packageManager.Save(packageInfo);
            // Unpack the zip file
            ZipUtility.ZipExtractToDirectory(zipcontent, Settings.PackagesFolder, true);
            Logger.Info("Registered package '{0}' with {1} handler file(s) and {2} dependent file(s)", packageInfo.PackageName, packageInfo.HandlerAssemblies.Count, packageInfo.Dependencies.Count);
            return true;
        }

        /// <summary>
        /// Called when a new job script is added
        /// Initializes and starts the appropriate handler
        /// </summary>
        public AddJobScriptResult AddJobScript(JobScriptInfo jobScriptInfo)
        {
            return _handlerManager.Add(jobScriptInfo);
        }

        /// <summary>
        /// Stops and removes a job handler
        /// </summary>
        public bool RemoveJobScript(Guid handlerId)
        {
            return _handlerManager.Remove(handlerId);
        }

        /// <summary>
        /// Starts the handler so jobs are being distributed
        /// </summary>
        public bool StartJobScript(Guid handlerId)
        {
            return _handlerManager.Start(handlerId);
        }

        /// <summary>
        /// Stops a handler so no more jobs are distributed and processed
        /// </summary>
        public bool StopJobScript(Guid handlerId)
        {
            return _handlerManager.Stop(handlerId);
        }

        public bool PauseJobScript(Guid handlerId)
        {
            return _handlerManager.Pause(handlerId);
        }

        public bool DisableJobScript(Guid handlerId)
        {
            return _handlerManager.Disable(handlerId);
        }

        public bool EnableJobScript(Guid handlerId)
        {
            return _handlerManager.Enable(handlerId);
        }

        public bool RemoveClient(Guid clientId)
        {
            return _clientManager.Remove(clientId);
        }

        /// <summary>
        /// Get a job from the current pending jobs in the handlers
        /// </summary>
        public Job GetJob(Guid clientId)
        {
            var clientInfo = _clientManager.GetOrCreate(clientId);
            Logger.Debug(entry => entry.SetClientId(clientId), "'{0}' requested a job", clientInfo.ClientInfo.Name);
            var job = _handlerManager.GetJob(clientInfo);
            if (job != null)
            {
                // Update statistics
                clientInfo.JobsInProgress++;
            }
            return job;
        }

        /// <summary>
        /// Get information for the client for the given handler to execute the job
        /// </summary>
        public HandlerJobInfo GetHandlerJobInfo(Guid handlerId)
        {
            return _handlerManager.GetHandlerJobInfo(handlerId);
        }

        /// <summary>
        /// Gets a file from the package of the specified handler
        /// </summary>
        public byte[] GetFile(Guid handlerId, string file)
        {
            var handlerPackageName = _handlerManager.GetPackageName(handlerId);
            if (handlerPackageName != null)
            {
                var fileContent = _packageManager.GetFile(handlerPackageName, file);
                return fileContent;
            }
            return null;
        }

        /// <summary>
        /// Received a result for one of the jobs from a client
        /// </summary>
        public void ReceiveResult(JobResult result)
        {
            var success = _handlerManager.ProcessResult(result);
            // Update statistics
            var clientInfo = _clientManager.GetOrCreate(result.ClientId);
            clientInfo.JobsInProgress--;
            if (success)
            {
                clientInfo.TotalJobsProcessed++;
            }
            else
            {
                clientInfo.TotalJobsFailed++;
            }
        }

        /// <summary>
        /// Receive information from the client about the client
        /// </summary>
        public void ReceivedClientInfo(ClientInfo info)
        {
            var clientInfo = _clientManager.GetOrCreate(info.Id);
            clientInfo.LastCommunicationDate = DateTime.Now;
            clientInfo.ClientInfo = info;
        }
    }
}
