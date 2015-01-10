using NetDist.Logging;
using System;

namespace NetDist.Server
{
    /// <summary>
    /// Abstract class for server implementations
    /// </summary>
    /// <typeparam name="TSer">Type of the serialized values</typeparam>
    public abstract class ServerBase<TSer>
    {
        /// <summary>
        /// Logger object
        /// </summary>
        public Logger Logger { get; private set; }

        /// <summary>
        /// Abstract method to start the server
        /// </summary>
        protected abstract bool InternalStart();

        /// <summary>
        /// Abstract method to stop the server
        /// </summary>
        protected abstract bool InternalStop();

        protected ServerBase()
        {
            Logger = new Logger();
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
            // TODO: Stop all handlers
        }

        /// <summary>
        /// Called when a new job-logic is added
        /// Initializes and starts the appropriate handler
        /// </summary>
        public bool AddJobLogic()
        {
            // TODO: Get handler
            var domain = AppDomain.CreateDomain(Guid.NewGuid().ToString(), null, new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
                ApplicationName = AppDomain.CurrentDomain.SetupInformation.ApplicationName,
                LoaderOptimization = LoaderOptimization.MultiDomainHost,
                ShadowCopyFiles = "true",
                AppDomainInitializerArguments = null
            });
            var loadedHandler = domain.CreateInstanceAndUnwrap(typeof(LoadedHandler).Assembly.FullName, typeof(LoadedHandler).FullName);

            // TODO: Init / start handler
            return true;
        }


        // TODO: This is in concept phase
        /*public abstract TSer Serialize(object obj);

        public virtual T Deserialize<T>(TSer serializedObject)
        {
            return (T)Deserialize(serializedObject, typeof(T));
        }

        public virtual object Deserialize(TSer serializedObject, Type type)
        {
            var method = GetType().GetMethod("Deserialize").MakeGenericMethod(new[] { type });
            return method.Invoke(this, new object[] { serializedObject });
        }

        public T Deserialize<T>(TSer serializedObject, T example)
        {
            return Deserialize<T>(serializedObject);
        }*/
    }
}
