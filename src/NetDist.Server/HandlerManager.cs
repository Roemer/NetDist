using NetDist.Core;
using NetDist.Core.Extensions;
using NetDist.Core.Utilities;
using NetDist.Jobs;
using NetDist.Jobs.DataContracts;
using NetDist.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NetDist.Server
{
    /// <summary>
    /// Manager class for all handlers
    /// </summary>
    public class HandlerManager
    {
        private readonly Dictionary<Guid, HandlerInstance> _loadedHandlers;
        private Logger Logger { get; set; }
        private readonly PackageManager _packageManager;
        private readonly Task _schedulerTask;
        private readonly CancellationTokenSource _schedulerTaskCancelToken;

        /// <summary>
        /// Constructor
        /// </summary>
        public HandlerManager(Logger logger, PackageManager packageManager)
        {
            Logger = logger;
            _packageManager = packageManager;
            _loadedHandlers = new Dictionary<Guid, HandlerInstance>();

            // Initialize the task to regularly check if a handler should be restarted or postpone the next start
            _schedulerTaskCancelToken = new CancellationTokenSource();
            _schedulerTask = new Task(SchedulerThread, _schedulerTaskCancelToken.Token);
            _schedulerTask.Start();
        }

        /// <summary>
        /// Get the package name of the given handler
        /// </summary>
        public string GetPackageName(Guid handlerId)
        {
            var handler = GetHandler(handlerId);
            return handler != null ? handler.JobScript.PackageName : null;
        }

        /// <summary>
        /// Get statistics about all handlers
        /// </summary>
        public List<HandlerInfo> GetStatistics()
        {
            var retList = new List<HandlerInfo>();
            foreach (var kvp in _loadedHandlers)
            {
                var loadedHandler = kvp.Value;
                retList.Add(loadedHandler.GetInfo());
            }
            return retList;
        }

        public void TearDown()
        {
            // Notify the scheduler task to stop
            _schedulerTaskCancelToken.Cancel();
            // Wait until the task is finished (but not when faulted)
            if (!_schedulerTask.IsFaulted)
            {
                _schedulerTask.Wait();
            }
            // Stop all handlers
            foreach (var handler in _loadedHandlers)
            {
                Stop(handler.Value.Id);
            }
        }

        /// <summary>
        /// Adds or updates the handler with the given job script
        /// </summary>
        public AddJobScriptResult Add(JobScriptInfo jobScriptInfo)
        {
            // Prepare the info object
            var addResult = new AddJobScriptResult();

            // Parse the content
            var jobScriptFile = JobScriptFileParser.Parse(jobScriptInfo.JobScript);
            if (jobScriptFile.ParsingFailed)
            {
                Logger.Error("Failed to parse job script: {0}", jobScriptFile.ErrorMessage);
                addResult.SetError(AddJobScriptError.ParsingFailed, jobScriptFile.ErrorMessage);
                return addResult;
            }

            // Compile the job file
            var compileResult = JobScriptCompiler.Compile(jobScriptFile, _packageManager.GetPackagePath(jobScriptFile.PackageName));
            // Check for compilation error
            if (compileResult.ResultType == CompileResultType.Failed)
            {
                Logger.Error("Failed to compile job script: {0}", compileResult.ErrorString);
                addResult.SetError(AddJobScriptError.CompilationFailed, compileResult.ErrorString);
                return addResult;
            }
            // Check if it already was compiled
            if (compileResult.ResultType == CompileResultType.AlreadyCompiled)
            {
                Logger.Info("Job script already exists");
            }

            // Get the settings from the job file
            HandlerSettings handlerSettings;
            var readScuccess = JobFileHandlerSettingsReader.ReadSettingsInOwnDomain(compileResult.OutputAssembly, out handlerSettings);
            if (!readScuccess)
            {
                Logger.Error("Initializer type not found");
                addResult.SetError(AddJobScriptError.HandlerInitializerMissing, "Handler initializer type not found");
                return addResult;
            }

            // Search for an already existing handler
            var currentFullName = Helpers.BuildFullName(jobScriptFile.PackageName, handlerSettings.HandlerName, handlerSettings.JobName);
            HandlerInstance handlerInstance = null;
            lock (_loadedHandlers.GetSyncRoot())
            {
                bool foundExisting = false;
                foreach (var handler in _loadedHandlers)
                {
                    if (handler.Value.FullName == currentFullName)
                    {
                        // Found an existing same handler
                        handlerInstance = handler.Value;
                        foundExisting = true;
                        break;
                    }
                }
                if (!foundExisting)
                {
                    // No handler instance found, create a new one
                    handlerInstance = new HandlerInstance(Logger, _packageManager);
                    _loadedHandlers.Add(handlerInstance.Id, handlerInstance);
                }
                // Initialize/update the values
                var replaced = handlerInstance.InitializeFromJobScript(jobScriptFile, compileResult.OutputAssembly, handlerSettings);

                if (foundExisting)
                {
                    if (replaced)
                    {
                        addResult.SetOk(handlerInstance.Id, AddJobScriptStatus.JobScriptReplaced);
                        Logger.Info(entry => entry.SetHandlerId(handlerInstance.Id), "Updated jobscript");
                    }
                    else
                    {
                        addResult.SetOk(handlerInstance.Id, AddJobScriptStatus.NoUpdateNeeded);
                        Logger.Info(entry => entry.SetHandlerId(handlerInstance.Id), "No jobscript update needed");
                    }
                    return addResult;
                }
            }

            // Autostart if wanted
            if (handlerInstance.HandlerSettings.AutoStart)
            {
                Start(handlerInstance.Id);
            }

            // Fill the info object from the result
            addResult.SetOk(handlerInstance.Id, AddJobScriptStatus.Ok);
            Logger.Info(entry => entry.SetHandlerId(handlerInstance.Id), "Added", handlerInstance.Id);
            return addResult;
        }

        /// <summary>
        /// Completely stops and removes the handler with the given id
        /// </summary>
        public bool Remove(Guid handlerId)
        {
            // Search and remove the object from the loaded handlers
            HandlerInstance removedItem = null;
            lock (_loadedHandlers.GetSyncRoot())
            {
                if (_loadedHandlers.ContainsKey(handlerId))
                {
                    removedItem = _loadedHandlers[handlerId];
                    _loadedHandlers.Remove(handlerId);
                }
            }
            if (removedItem != null)
            {
                var handlerName = removedItem.FullName;
                removedItem.Stop();
                Logger.Info(entry => entry.SetHandlerId(handlerId), "Removed", handlerName);
                return true;
            }
            return false;
        }

        public bool Start(Guid handlerId)
        {
            return ExecuteOnHandler(handlerId, handler =>
            {
                Logger.Info(entry => entry.SetHandlerId(handlerId), "Starting '{0}'", handler.FullName);
                return handler.Start();
            });
        }

        public bool Stop(Guid handlerId)
        {
            return ExecuteOnHandler(handlerId, handler =>
            {
                Logger.Info(entry => entry.SetHandlerId(handlerId), "Stopping '{0}'", handler.FullName);
                return handler.Stop();
            });
        }

        public bool Pause(Guid handlerId)
        {
            return ExecuteOnHandler(handlerId, handler =>
            {
                Logger.Info(entry => entry.SetHandlerId(handlerId), "Pausing '{0}'", handler.FullName);
                return handler.Pause();
            });
        }

        public bool Disable(Guid handlerId)
        {
            return ExecuteOnHandler(handlerId, handler =>
            {
                Logger.Info(entry => entry.SetHandlerId(handlerId), "Disabling '{0}'", handler.FullName);
                return handler.Disable();
            });
        }

        public bool Enable(Guid handlerId)
        {
            return ExecuteOnHandler(handlerId, handler =>
            {
                Logger.Info(entry => entry.SetHandlerId(handlerId), "Enabling '{0}'", handler.FullName);
                return handler.Enable();
            });
        }

        public Job GetJob(Guid clientId)
        {
            for (var i = 0; i < 10; i++)
            {
                var handlersWithJobs = _loadedHandlers.Where(x => x.Value.CanDeliverJob).ToArray();
                if (handlersWithJobs.Length == 0)
                {
                    // No handler with available jobs at all
                    return null;
                }
                // Randomly choose a handler
                var nextRandNumber = RandomGenerator.Instance.Next(handlersWithJobs.Length);
                var randomHandler = handlersWithJobs[nextRandNumber];
                var nextJob = randomHandler.Value.GetJob(clientId);
                if (nextJob == null)
                {
                    // Can happen if the queue was empty between the "HasAvailableJobs" check and now
                    // just retry a few times
                    Logger.Debug(entry => entry.SetClientId(clientId), "Job queue was suddenly empty, try again");
                    continue;
                }
                Logger.Info(entry => entry.SetClientId(clientId), "Got job '{0}' for handler '{1}'", nextJob.Id, randomHandler.Value.FullName);
                return nextJob;
            }
            Logger.Warn(entry => entry.SetClientId(clientId), "Gave up getting a job");
            return null;
        }

        /// <summary>
        /// Get relevant information for the client for this handler
        /// </summary>
        public HandlerJobInfo GetHandlerJobInfo(Guid handlerId)
        {
            var handler = GetHandler(handlerId);
            if (handler != null)
            {
                var info = new HandlerJobInfo
                {
                    HandlerName = handler.FullName,
                    JobAssemblyName = Path.GetFileName(handler.JobScriptAssembly),
                    Depdendencies = handler.JobScript.Dependencies
                };
                return info;
            }
            return null;
        }

        public bool ProcessResult(JobResult result)
        {
            if (result == null)
            {
                Logger.Error("Received invalid result");
                return false;
            }
            var handler = GetHandler(result.HandlerId);
            if (handler == null)
            {
                Logger.Error(entry => entry.SetHandlerId(result.HandlerId), "Got result for unknown handler");
                return false;
            }
            // Forward the result to the handler (also failed ones)
            var success = handler.ReceivedResult(result);
            return success;
        }

        /// <summary>
        /// Helper method to execute an action on a handler (if it exists)
        /// </summary>
        private bool ExecuteOnHandler(Guid handlerId, Func<HandlerInstance, bool> successAction)
        {
            lock (_loadedHandlers.GetSyncRoot())
            {
                // Try getting the handler
                var handler = GetHandler(handlerId);
                if (handler != null)
                {
                    // Execute the action
                    var retValue = successAction(handler);
                    return retValue;
                }
            }
            // Handler not found
            Logger.Warn(entry => entry.SetHandlerId(handlerId), "Handler not found to execute action", handlerId);
            return false;
        }

        /// <summary>
        /// Try getting the handler for the given id
        /// </summary>
        private HandlerInstance GetHandler(Guid handlerId)
        {
            HandlerInstance handlerInstance;
            var hasHandler = _loadedHandlers.TryGetValue(handlerId, out handlerInstance);
            return hasHandler ? handlerInstance : null;
        }

        /// <summary>
        /// Thread which checks for scheduled starts
        /// </summary>
        private void SchedulerThread()
        {
            while (!_schedulerTaskCancelToken.IsCancellationRequested)
            {
                foreach (var handler in _loadedHandlers)
                {
                    handler.Value.CheckIdle();
                    handler.Value.ScheduledStartOrReschedule();
                }
                Thread.Sleep(10000);
            }
        }
    }
}
