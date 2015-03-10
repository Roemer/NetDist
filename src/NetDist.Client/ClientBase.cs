using Microsoft.VisualBasic.Devices;
using NetDist.Core;
using NetDist.Core.Extensions;
using NetDist.Core.Utilities;
using NetDist.Jobs.DataContracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NetDist.Client
{
    public abstract class ClientBase : ObservableObject
    {
        /// <summary>
        /// Number of maximum parallel jobs to execute
        /// </summary>
        public int NumberOfParallelJobs
        {
            get { return GetProperty<int>(); }
            set { SetProperty(value); }
        }
        /// <summary>
        /// The current status
        /// </summary>
        public ClientStatusType Status
        {
            get { return GetProperty<ClientStatusType>(); }
            set { SetProperty(value); }
        }

        /// <summary>
        /// Flag to indicate if the server is reachable
        /// </summary>
        public bool IsServerReachable
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        /// <summary>
        /// Event to be notified when a job was added
        /// </summary>
        public event EventHandler<ClientJobEventArgs> JobAddedEvent;
        /// <summary>
        /// Event to be notified when a job was removed
        /// </summary>
        public event EventHandler<ClientJobEventArgs> JobRemovedEvent;

        /// <summary>
        /// Info object about the client
        /// </summary>
        private readonly ClientInfo _clientInfo;
        /// <summary>
        /// List of jobs currently in progress
        /// </summary>
        private ConcurrentDictionary<Guid, ClientJob> Jobs { get; set; }
        /// <summary>
        /// Flag to indicate that new jobs can be started
        /// </summary>
        private bool _fetchNewJobs;
        /// <summary>
        /// Dictionary for each handler with the current state and information
        /// </summary>
        private readonly ConcurrentDictionary<Guid, ClientHandlerState> _handlerStates = new ConcurrentDictionary<Guid, ClientHandlerState>();
        /// <summary>
        /// EventWaitHandle to set when all jobs are processed
        /// </summary>
        private readonly AutoResetEvent _allJobsDone = new AutoResetEvent(false);
        /// <summary>
        /// EventWaitHandle to indicate that processing is stopped
        /// </summary>
        private readonly AutoResetEvent _stoppedWaitHandle = new AutoResetEvent(false);
        /// <summary>
        /// Date when the client should send the next status update to the server
        /// </summary>
        private DateTime _nextStatusUpdate = DateTime.Now;

        /// <summary>
        /// Constructor
        /// </summary>
        protected ClientBase(IClientSettings settings)
        {
            // General initialization
            Jobs = new ConcurrentDictionary<Guid, ClientJob>();

            // Initialize basic information about the client
            _clientInfo = new ClientInfo
            {
                Id = (settings.Id == Guid.Empty) ? Guid.NewGuid() : settings.Id,
                Name = settings.Name,
                StartDate = DateTime.Now,
                Version = "unknown"
            };

            NumberOfParallelJobs = settings.NumberOfParallelJobs;
            if (settings.AutoStart)
            {
                StartProcessing();
            }
        }

        /// <summary>
        /// Update all relevant information from a new settings object
        /// </summary>
        public void UpdateFromSettings(IClientSettings settings)
        {
            _clientInfo.Id = settings.Id;
            _clientInfo.Name = settings.Name;
            NumberOfParallelJobs = settings.NumberOfParallelJobs;
        }

        /// <summary>
        /// Update the version number of the client
        /// </summary>
        public void UpdateVersion(string newVersion)
        {
            _clientInfo.Version = newVersion;
        }

        /// <summary>
        /// Updates the dynamic information of the client
        /// </summary>
        private void UpdateClientInfo()
        {
            // RAM information
            var ci = new ComputerInfo();
            _clientInfo.TotalMemory = ci.TotalPhysicalMemory;
            _clientInfo.UsedMemory = ci.TotalPhysicalMemory - ci.AvailablePhysicalMemory;
            // CPU information
            _clientInfo.CpuUsage = CpuUsageReader.GetValue();
            // Disk information
            _clientInfo.DiskInformations = new List<DiskInformation>();
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                {
                    var diskInformation = new DiskInformation
                    {
                        Name = drive.Name,
                        Label = drive.VolumeLabel,
                        TotalDiskSpace = (ulong)drive.TotalSize,
                        FreeDiskSpace = (ulong)drive.TotalFreeSpace,
                    };
                    _clientInfo.DiskInformations.Add(diskInformation);
                }
            }
        }

        /// <summary>
        /// Starts processing jobs
        /// </summary>
        public void StartProcessing()
        {
            _fetchNewJobs = true;
            Task.Factory.StartNew(MainTask);
        }

        /// <summary>
        /// Finishes current jobs but stops getting new ones
        /// </summary>
        public void StopProcessing()
        {
            _fetchNewJobs = false;
            _stoppedWaitHandle.Set();
        }

        /// <summary>
        /// Manually start a job
        /// </summary>
        public bool ManuallyStartJob()
        {
            SendClientUpdateIfNeeded();
            return GetAndStartJob();
        }

        /// <summary>
        /// Tries to get a new job and starts processing it
        /// </summary>
        private bool GetAndStartJob()
        {
            var nextJob = GetJob(_clientInfo.Id);
            if (nextJob != null)
            {
                var clientJob = new ClientJob(nextJob);
                Jobs.TryAdd(clientJob.Job.Id, clientJob);
                OnJobAddedEvent(new ClientJobEventArgs(clientJob));
                Task.Factory.StartNew(ProcessJob, clientJob);
                return true;
            }
            return false;
        }

        private void OnJobAddedEvent(ClientJobEventArgs e)
        {
            var handler = JobAddedEvent;
            if (handler != null) handler(this, e);
        }

        private void OnJobRemovedEvent(ClientJobEventArgs e)
        {
            var handler = JobRemovedEvent;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Sends client information to the server at a certain interval
        /// </summary>
        private void SendClientUpdateIfNeeded()
        {
            if (_nextStatusUpdate <= DateTime.Now)
            {
                UpdateClientInfo();
                SendInfo(_clientInfo);
                _nextStatusUpdate = DateTime.Now.AddMinutes(1);
            }
        }

        /// <summary>
        /// Main application loop to fetch and start processing jobs
        /// </summary>
        private void MainTask()
        {
            while (_fetchNewJobs)
            {
                // Send client info regularly
                SendClientUpdateIfNeeded();

                var sleepTime = 10000;
                if (Jobs.Count >= NumberOfParallelJobs)
                {
                    Status = ClientStatusType.WaintingForJobSlot;
                }
                else
                {
                    // Try to get a new job
                    var jobStarted = GetAndStartJob();
                    if (jobStarted)
                    {
                        Status = ClientStatusType.Running;
                        sleepTime = 100;
                    }
                    else
                    {
                        Status = ClientStatusType.WaitingForJobs;
                    }
                }
                // Wait a little before getting the next job
                // OR until a job slot is free
                // OR until processing was stopped
                WaitHandle.WaitAny(new WaitHandle[] { _allJobsDone, _stoppedWaitHandle }, sleepTime);
            }
            Status = ClientStatusType.Idle;
        }

        /// <summary>
        /// Method to process a job
        /// </summary>
        /// <param name="state">The job object to process</param>
        private void ProcessJob(object state)
        {
            // Setup
            var clientJob = (ClientJob)state;
            var job = clientJob.Job;
            // Build the path to the local folder
            var localHandlerFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, job.HandlerId.ToString());
            // Make sure that the directory exists
            Directory.CreateDirectory(localHandlerFolder);

            // Get or add the state for the current handler
            var currentHandlerState = _handlerStates.GetOrAdd(job.HandlerId, guid => new ClientHandlerState());

            // Check if the handler isn't initialized yet or the hash changed
            if (!currentHandlerState.IsInitialized || currentHandlerState.Hash != job.Hash)
            {
                // Lock the handler
                lock (currentHandlerState)
                {
                    // Check again if it isn't initialized yet or the hash changed
                    if (!currentHandlerState.IsInitialized || currentHandlerState.Hash != job.Hash)
                    {
                        // Get information about the handler
                        var handlerInfo = GetHandlerJobInfo(job.HandlerId);
                        // Download the main assembly
                        var mainAssemblyContent = DownloadFile(job.HandlerId, handlerInfo.JobAssemblyName);
                        if (mainAssemblyContent == null)
                        {
                            // Could not find the assembly file
                            var jobResult = new JobResult(job, _clientInfo.Id, new Exception("Job assembly not found"));
                            RemoveAndReturnJobResult(clientJob, jobResult);
                            return;
                        }
                        var mainAssemblyFullPath = Path.Combine(localHandlerFolder, handlerInfo.JobAssemblyName);
                        SaveContentAsFile(mainAssemblyFullPath, mainAssemblyContent);
                        // Download the dependencies
                        foreach (var file in handlerInfo.Depdendencies)
                        {
                            var dependencyContent = DownloadFile(job.HandlerId, file);
                            if (dependencyContent == null)
                            {
                                // Could not find the dependency
                                var jobResult = new JobResult(job, _clientInfo.Id, new Exception(String.Format("Dependency '{0}' not found", file)));
                                RemoveAndReturnJobResult(clientJob, jobResult);
                                return;
                            }
                            var dependencyPath = Path.Combine(localHandlerFolder, file);
                            SaveContentAsFile(dependencyPath, dependencyContent);
                        }
                        // Finish initialization
                        currentHandlerState.MainAssemblyName = mainAssemblyFullPath;
                        currentHandlerState.HandlerJobInfo = handlerInfo;
                        currentHandlerState.Hash = job.Hash;
                        // Set as initialized
                        currentHandlerState.IsInitialized = true;
                    }
                }
            }

            // Prepare to execute the job
            clientJob.HandlerName = currentHandlerState.HandlerJobInfo.HandlerName;
            var jobLibraryName = currentHandlerState.MainAssemblyName;
            try
            {
                // Create an additional app-domain
                var domain = AppDomain.CreateDomain(Guid.NewGuid().ToString(), null, new AppDomainSetup
                {
                    ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                    ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
                    ApplicationName = AppDomain.CurrentDomain.SetupInformation.ApplicationName,
                    LoaderOptimization = LoaderOptimization.MultiDomainHost,
                    ShadowCopyFiles = "true",
                    AppDomainInitializerArguments = null,
                });
                // Create the proxy object for the job script
                var jobScriptProxy = (JobScriptProxy)domain.CreateInstanceAndUnwrap(typeof(JobScriptProxy).Assembly.FullName, typeof(JobScriptProxy).FullName);
                var jobLibraryFullPath = Path.Combine(localHandlerFolder, jobLibraryName);
                // Execute the logic and get the result
                var jobResult = jobScriptProxy.RunJob(_clientInfo.Id, job, jobLibraryFullPath);
                // Free the app domain
                AppDomain.Unload(domain);
                RemoveAndReturnJobResult(clientJob, jobResult);
            }
            catch (Exception ex)
            {
                var jobResult = new JobResult(job, _clientInfo.Id, ex);
                RemoveAndReturnJobResult(clientJob, jobResult);
            }
        }

        private void RemoveAndReturnJobResult(ClientJob clientJob, JobResult jobResult)
        {
            SendResult(jobResult);
            Jobs.Remove(clientJob.Job.Id);
            OnJobRemovedEvent(new ClientJobEventArgs(clientJob));
            if (Jobs.Count < NumberOfParallelJobs)
            {
                _allJobsDone.Set();
            }
        }

        /// <summary>
        /// Download a file
        /// </summary>
        private byte[] DownloadFile(Guid handlerId, string fileName)
        {
            var fileContent = GetFile(handlerId, fileName);
            return fileContent;
        }

        /// <summary>
        /// Saves the bytes to a file
        /// </summary>
        private void SaveContentAsFile(string filePath, byte[] content)
        {
            File.WriteAllBytes(filePath, content);
        }

        #region Abstract methods to implement
        /// <summary>
        /// Get the next free job to process from the server
        /// </summary>
        public abstract Job GetJob(Guid clientId);
        /// <summary>
        /// Send a result from a processed job to the server
        /// </summary>
        public abstract void SendResult(JobResult result);
        /// <summary>
        /// Get all the relevant information for a handler to execute the job
        /// </summary>
        public abstract HandlerJobInfo GetHandlerJobInfo(Guid handlerId);
        /// <summary>
        /// Request a file from a handler
        /// </summary>
        public abstract byte[] GetFile(Guid handlerId, string fileName);
        /// <summary>
        /// Sends information about the client
        /// </summary>
        public abstract void SendInfo(ClientInfo clientInfo);
        #endregion
    }
}
