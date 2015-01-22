using NetDist.Core;
using NetDist.Core.Utilities;
using NetDist.Jobs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualBasic.Devices;

namespace NetDist.Client
{
    public abstract class ClientBase : ObservableObject
    {
        /// <summary>
        /// Info object about the client
        /// </summary>
        public ClientInfo ClientInfo { get; private set; }

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
        /// List of jobs currently in progress
        /// </summary>
        public ObservableCollection<Job> Jobs { get; set; }

        /// <summary>
        /// Flag to indicate that new jobs can be started
        /// </summary>
        private bool _fetchNewJobs;
        /// <summary>
        /// Dictionary to lock while a job is downloading the files for a handler
        /// so no other thread will download them as well
        /// </summary>
        private readonly Dictionary<Guid, ManualResetEvent> _downloadFileLocks = new Dictionary<Guid, ManualResetEvent>();
        /// <summary>
        /// Dictionary with the job assembly name of each handler
        /// </summary>
        private readonly Dictionary<Guid, string> _handlerMainJobFile = new Dictionary<Guid, string>();
        /// <summary>
        /// EventWaitHandle to set when all jobs are processed
        /// </summary>
        private readonly AutoResetEvent _allJobsDone = new AutoResetEvent(false);
        /// <summary>
        /// EventWaitHandle to indicate that processing is stopped
        /// </summary>
        private readonly AutoResetEvent _stoppedWaitHandle = new AutoResetEvent(false);

        /// <summary>
        /// Constructor
        /// </summary>
        protected ClientBase()
        {
            // General initialization
            Jobs = new ObservableCollection<Job>();

            // Initialize basic information about the client
            ClientInfo = new ClientInfo
            {
                Id = Guid.NewGuid(),
                Name = Environment.MachineName.ToLower(),
                StartDate = DateTime.Now,
                Version = "unknown"
            };
        }

        protected void InitializeSettings(IClientSettings settings)
        {
            NumberOfParallelJobs = settings.NumberOfParallelJobs;
            if (settings.AutoStart)
            {
                StartProcessing();
            }
        }

        /// <summary>
        /// Updates the dynamic information of the client
        /// </summary>
        private void UpdateClientInfo()
        {
            // RAM information
            var ci = new ComputerInfo();
            ClientInfo.TotalMemory = ci.TotalPhysicalMemory;
            ClientInfo.UsedMemory = ci.TotalPhysicalMemory - ci.AvailablePhysicalMemory;
            // CPU information
            ClientInfo.CpuUsage = CpuUsageReader.GetValue();
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
        /// Tries to get a new job and starts processing it
        /// </summary>
        public bool GetAndStartJob()
        {
            var nextJob = GetJob();
            if (nextJob != null)
            {
                lock (((ICollection)Jobs).SyncRoot)
                {
                    Jobs.Add(nextJob);
                }
                Task.Factory.StartNew(ProcessJob, nextJob);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Main application loop to fetch and start processing jobs
        /// </summary>
        private void MainTask()
        {
            while (_fetchNewJobs)
            {
                // TODO: Send client info regularly
                UpdateClientInfo();
                SendInfo();

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
            var job = (Job)state;
            JobResult jobResult;
            var localHandlerFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, job.HandlerId.ToString());
            Directory.CreateDirectory(localHandlerFolder);

            // Check if we already have a lock for this handler
            if (_downloadFileLocks.ContainsKey(job.HandlerId))
            {
                // If so, wait for it
                _downloadFileLocks[job.HandlerId].WaitOne();
            }
            // Check if we don't know this handler already
            else if (!_handlerMainJobFile.ContainsKey(job.HandlerId))
            {
                // Create a lock for this handler id
                var resetEvent = new ManualResetEvent(false);
                _downloadFileLocks.Add(job.HandlerId, resetEvent);
                // Get information about the handler
                var handlerInfo = GetHandlerJobInfo(job.HandlerId);
                // Download the needed files
                var mainFilePath = DownloadAndSaveFile(job.HandlerId, localHandlerFolder, handlerInfo.JobAssemblyName);
                foreach (var file in handlerInfo.Depdendencies)
                {
                    DownloadAndSaveFile(job.HandlerId, localHandlerFolder, file);
                }
                // Add the handler to the "known" list
                _handlerMainJobFile.Add(job.HandlerId, mainFilePath);
                // Reset the event
                resetEvent.Set();
            }

            // Now the handler is known and no lock is on it
            var jobLibraryName = _handlerMainJobFile[job.HandlerId];
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
                var result = jobScriptProxy.RunJob(jobLibraryFullPath, job.JobInputString);
                // Create the result
                jobResult = new JobResult(job, ClientInfo.Id, result);
                // Free the app domain
                AppDomain.Unload(domain);
            }
            catch (Exception ex)
            {
                jobResult = new JobResult(job, ClientInfo.Id, ex);
            }
            SendResult(jobResult);
            lock (((ICollection)Jobs).SyncRoot)
            {
                Jobs.Remove(job);
            }
            if (Jobs.Count < NumberOfParallelJobs)
            {
                _allJobsDone.Set();
            }
        }

        /// <summary>
        /// Download a file and save it in the given folder
        /// </summary>
        private string DownloadAndSaveFile(Guid handlerId, string localHandlerFolder, string fileName)
        {
            var fileContent = GetFile(handlerId, fileName);
            var fullFilePath = Path.Combine(localHandlerFolder, fileName);
            File.WriteAllBytes(fullFilePath, fileContent);
            return fullFilePath;
        }

        #region Abstract methods to implement
        /// <summary>
        /// Get the next free job to process from the server
        /// </summary>
        public abstract Job GetJob();
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
        public abstract void SendInfo();
        #endregion
    }
}
