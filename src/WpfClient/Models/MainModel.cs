using NetDist.Client;
using NetDist.Client.WebApi;
using NetDist.Core.Utilities;
using NetDist.Jobs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetDist.Core;
using Wpf.Shared;
using WpfClient.Common;

namespace WpfClient.Models
{
    public class MainModel : ObservableObject
    {
        /// <summary>
        /// Version of the client
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// Number of maximum parallel jobs to execute
        /// </summary>
        public int NumberOfParallelTasks { get; set; }
        /// <summary>
        /// The current status
        /// </summary>
        public ClientStatusType Status
        {
            get { return GetProperty<ClientStatusType>(); }
            set { SetProperty(value); }
        }
        /// <summary>
        /// Analyzer for the network traffic
        /// </summary>
        public NetworkTrafficAnalyzer NetworkAnalyzer { get; private set; }
        /// <summary>
        /// Object to allow the communication with the server
        /// </summary>
        public WebApiClient Client { get; private set; }
        /// <summary>
        /// List of jobs currently in progress
        /// </summary>
        public ObservableCollection<Job> Jobs { get; set; }

        private readonly Dictionary<Guid, ManualResetEvent> _downloadFileLocks = new Dictionary<Guid, ManualResetEvent>();
        private readonly Dictionary<Guid, string> _handlerMainJobFile = new Dictionary<Guid, string>();
        private readonly AutoResetEvent _allJobsDone = new AutoResetEvent(false);
        private bool _processing;

        /// <summary>
        /// Constructir
        /// </summary>
        public MainModel()
        {
            NetworkAnalyzer = new NetworkTrafficAnalyzer();
            Client = new WebApiClient();
            Jobs = new ObservableCollection<Job>();
            NumberOfParallelTasks = 3;
        }

        public void Start()
        {
            _processing = true;
            Task.Factory.StartNew(MainTask);
        }

        public void Stop()
        {
            _processing = false;
        }

        public void GetJob()
        {
            GetAndStartJob();
        }

        private void MainTask()
        {
            while (_processing)
            {
                var sleepTime = 10000;
                if (Jobs.Count >= NumberOfParallelTasks)
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
                // Wait a little before getting the next job OR until a job slot is free
                _allJobsDone.WaitOne(sleepTime);

                // TODO: Send client info regularly
                Client.SendInfo();
            }
            Status = ClientStatusType.Idle;
        }

        private bool GetAndStartJob()
        {
            var nextJob = Client.GetJob();
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

        private void ProcessJob(object state)
        {
            // Setup
            var job = (Job)state;
            JobResult jobResult = null;
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
                var handlerInfo = Client.GetHandlerClientInfo(job.HandlerId);
                // Download the needed files
                var mainFilePath = DownloadAndSaveFile(job.HandlerId, localHandlerFolder, handlerInfo.JobFile);
                foreach (var file in handlerInfo.Depdendencies)
                {
                    DownloadAndSaveFile(job.HandlerId, localHandlerFolder, file);
                }
                // Reset the event
                resetEvent.Set();

                // Add the handler to the "known" list
                _handlerMainJobFile.Add(job.HandlerId, mainFilePath);
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
                jobResult = new JobResult(job, Client.ClientInfo.Id, result);
                // Free the app domain
                AppDomain.Unload(domain);
            }
            catch (Exception ex)
            {
                jobResult = new JobResult(job, Client.ClientInfo.Id, ex);
            }
            Client.SendResult(jobResult);
            lock (((ICollection)Jobs).SyncRoot)
            {
                Jobs.Remove(job);
            }
            if (Jobs.Count < NumberOfParallelTasks)
            {
                _allJobsDone.Set();
            }
        }

        private string DownloadAndSaveFile(Guid handlerId, string localHandlerFolder, string fileName)
        {
            var fileContent = Client.GetFile(handlerId, fileName);
            var fullFilePath = Path.Combine(localHandlerFolder, fileName);
            File.WriteAllBytes(fullFilePath, fileContent);
            return fullFilePath;
        }
    }
}
