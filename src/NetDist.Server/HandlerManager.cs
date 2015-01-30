using NetDist.Core;
using NetDist.Core.Extensions;
using NetDist.Core.Utilities;
using NetDist.Jobs;
using NetDist.Jobs.DataContracts;
using NetDist.Logging;
using NetDist.Server.XDomainObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NetDist.Server
{
    public class HandlerManager
    {
        /// <summary>
        /// Container object for a handler
        /// </summary>
        private class HandlerContainer
        {
            public AppDomain AppDomain { get; private set; }
            public LoadedHandlerProxy HandlerProxy { get; private set; }

            public HandlerContainer(AppDomain appDomain, LoadedHandlerProxy handlerProxy)
            {
                AppDomain = appDomain;
                HandlerProxy = handlerProxy;
            }
        }

        private readonly Dictionary<Guid, HandlerContainer> _loadedHandlers;
        private Logger Logger { get; set; }
        private readonly PackageManager _packageManager;

        /// <summary>
        /// Constructor
        /// </summary>
        public HandlerManager(Logger logger, PackageManager packageManager)
        {
            Logger = logger;
            _packageManager = packageManager;
            _loadedHandlers = new Dictionary<Guid, HandlerContainer>();
        }

        public HandlerInfo GetStatistics(Guid handlerId)
        {
            var handler = GetHandler(handlerId);
            return handler.GetInfo();
        }

        public List<HandlerInfo> GetStatistics()
        {
            var retList = new List<HandlerInfo>();
            foreach (var kvp in _loadedHandlers)
            {
                var loadedHandler = kvp.Value.HandlerProxy;
                retList.Add(loadedHandler.GetInfo());
            }
            return retList;
        }

        public void StopAll()
        {
            foreach (var handler in _loadedHandlers)
            {
                Stop(handler.Value.HandlerProxy.Id);
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
                addResult.SetError(AddJobScriptErrorReason.ParsingFailed, jobScriptFile.ErrorMessage);
                return addResult;
            }

            // Add now known package name
            addResult.PackageName = jobScriptFile.PackageName;

            // Compile the job file
            var compileResult = JobScriptCompiler.Compile(jobScriptFile, _packageManager.GetPackagePath(jobScriptFile.PackageName));
            // Check for compilation error
            if (compileResult.ResultType == CompileResultType.Failed)
            {
                Logger.Error("Failed to compile job script: {0}", compileResult.ErrorString);
                addResult.SetError(AddJobScriptErrorReason.CompilationFailed, compileResult.ErrorString);
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
                Logger.Error("Handler initializer type not found");
                addResult.SetError(AddJobScriptErrorReason.HandlerInitializerMissing, "Handler initializer type not found");
                return addResult;
            }

            // Search for an already existing job handler
            var currentFullName = Helpers.BuildFullName(jobScriptFile.PackageName, handlerSettings.HandlerName, handlerSettings.JobName);
            lock (_loadedHandlers.GetSyncRoot())
            {
                foreach (var handler in _loadedHandlers)
                {
                    if (handler.Value.HandlerProxy.FullName == currentFullName)
                    {
                        // Found an existing same handler
                        // Replace only the job file
                        var replaceSuccess = handler.Value.HandlerProxy.ReplaceJobScript(jobScriptFile, compileResult.OutputAssembly);
                        if (replaceSuccess)
                        {
                            addResult.SetUpdated(AddJobScriptUpdateType.JobScriptReplaced, handler.Value.HandlerProxy.Id);
                            Logger.Info("Updated jobscript");
                        }
                        else
                        {
                            addResult.SetUpdated(AddJobScriptUpdateType.NoUpdateNeeded, handler.Value.HandlerProxy.Id);
                            Logger.Info("No jobscript update needed");
                        }
                        return addResult;
                    }
                }
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
            // Create a proxy for the handler
            var loadedHandler = (LoadedHandlerProxy)domain.CreateInstanceAndUnwrap(typeof(LoadedHandlerProxy).Assembly.FullName, typeof(LoadedHandlerProxy).FullName, false, BindingFlags.Default, null, new object[] { _packageManager.PackageBaseFolder }, null, null);
            // Create a interchangeable event sink to register cross-domain events to catch logging events
            var sink = new EventSink<LogEventArgs>();
            loadedHandler.RegisterLogEventSink(sink);
            sink.NotificationFired += (sender, args) => Logger.Log(args.LogLevel, args.Exception, args.Message);
            // Initialize the handler
            var initParams = new LoadedHandlerInitializeParams
            {
                HandlerSettings = handlerSettings,
                JobScriptFile = jobScriptFile,
                JobAssemblyPath = compileResult.OutputAssembly
            };
            var initResult = loadedHandler.Initialize(initParams);
            if (initResult.HasError)
            {
                AppDomain.Unload(domain);
                Logger.Warn("Failed to initialize handler for package: '{0}'", addResult.PackageName);
                addResult.SetError(initResult.ErrorReason, initResult.ErrorMessage);
                return addResult;
            }
            // Fill the info object from the result
            addResult.HandlerId = initResult.HandlerId;
            addResult.HandlerName = initResult.HandlerName;
            addResult.JobName = initResult.JobName;
            // Add the loaded handler to the dictionary
            _loadedHandlers[loadedHandler.Id] = new HandlerContainer(domain, loadedHandler);
            Logger.Info("Added handler: '{0}' ('{1}')", loadedHandler.FullName, loadedHandler.Id);
            return addResult;
        }

        /// <summary>
        /// Completely stops and removes the handler with the given id
        /// </summary>
        public bool Remove(Guid handlerId)
        {
            // Search and remove the object from the loaded handlers
            HandlerContainer removedItem = null;
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
                var handlerName = removedItem.HandlerProxy.FullName;
                // Stop and cleanup the handler
                removedItem.HandlerProxy.TearDown();
                // Unload the domain
                AppDomain.Unload(removedItem.AppDomain);
                Logger.Info("Removed handler: '{0}' ('{1}')", handlerName, handlerId);
                return true;
            }
            return false;
        }

        public bool Start(Guid handlerId)
        {
            return ExecuteOnHandler(handlerId, handler =>
            {
                Logger.Info("Starting handler '{0}'", handler.FullName);
                return handler.Start();
            });
        }

        public bool Stop(Guid handlerId)
        {
            return ExecuteOnHandler(handlerId, handler =>
            {
                Logger.Info("Stopping handler '{0}'", handler.FullName);
                return handler.Stop();
            });
        }

        public bool Pause(Guid handlerId)
        {
            return ExecuteOnHandler(handlerId, handler =>
            {
                Logger.Info("Pausing handler '{0}'", handler.FullName);
                return handler.Pause();
            });
        }

        public bool Disable(Guid handlerId)
        {
            return ExecuteOnHandler(handlerId, handler =>
            {
                Logger.Info("Disabling handler '{0}'", handler.FullName);
                return handler.Disable();
            });
        }

        public bool Enable(Guid handlerId)
        {
            return ExecuteOnHandler(handlerId, handler =>
            {
                Logger.Info("Enabling handler '{0}'", handler.FullName);
                return handler.Enable();
            });
        }

        public Job GetJob(Guid clientId)
        {
            for (var i = 0; i < 10; i++)
            {
                var handlersWithJobs = _loadedHandlers.Where(x => x.Value.HandlerProxy.HasAvailableJobs).ToArray();
                if (handlersWithJobs.Length == 0)
                {
                    // No handler with available jobs at all
                    return null;
                }
                // Randomly choose a handler
                var nextRandNumber = RandomGenerator.Instance.Next(handlersWithJobs.Length);
                var randomHandler = handlersWithJobs[nextRandNumber];
                var nextJob = randomHandler.Value.HandlerProxy.GetJob(clientId);
                if (nextJob == null)
                {
                    // Can happen if the queue was empty between the "HasAvailableJobs" check and now
                    // just retry a few times
                    Logger.Debug("Job queue was suddenly empty, try again");
                    continue;
                }
                Logger.Info("Client '{0}' got job '{1}' for handler '{2}'", clientId, nextJob.Id, randomHandler.Value.HandlerProxy.FullName);
                return nextJob;
            }
            Logger.Warn("Gave up getting a job");
            return null;
        }

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
                Logger.Error("Got result for unknown handler: '{0}'", result.HandlerId);
                return false;
            }
            // Forward the result to the handler (also failed ones)
            var success = handler.ReceivedResult(result);
            return success;
        }

        /// <summary>
        /// Helper method to execute an action on a handler (if it exists)
        /// </summary>
        private bool ExecuteOnHandler(Guid handlerId, Func<LoadedHandlerProxy, bool> successAction)
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
            Logger.Warn("Handler '{0}' not found to execute action", handlerId);
            return false;
        }

        /// <summary>
        /// Try getting the handler for the given id
        /// </summary>
        private LoadedHandlerProxy GetHandler(Guid handlerId)
        {
            HandlerContainer handlerContainer;
            var hasHandler = _loadedHandlers.TryGetValue(handlerId, out handlerContainer);
            return hasHandler ? handlerContainer.HandlerProxy : null;
        }
    }
}
