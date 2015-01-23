using NetDist.Core;
using NetDist.Jobs;
using NetDist.Logging;
using System;
using System.Threading;

namespace NetDist.Handlers
{
    /// <summary>
    /// Base class for the handlers for the different job types
    /// </summary>
    /// <typeparam name="TSet">Type of the custom handler settings object</typeparam>
    /// <typeparam name="TIn">Type of job input object</typeparam>
    /// <typeparam name="TOut">Type of job output object</typeparam>
    public abstract class HandlerBase<TSet, TIn, TOut> : IHandler
        where TSet : IHandlerCustomSettings
        where TIn : IJobInput
        where TOut : IJobOutput
    {
        /// <summary>
        /// Instance of the settings for this handler
        /// </summary>
        public TSet Settings { get; private set; }

        /// <summary>
        /// Flag to indicate if the handler is finished or not
        /// </summary>
        public bool IsFinished { get; protected set; }

        /// <summary>
        /// A count to show how many jobs are still being processed
        /// </summary>
        public long JobCount
        {
            get { return Interlocked.Read(ref _jobCount); }
        }
        private long _jobCount;

        /// <summary>
        /// Converts the passed settings string to the generic settings object
        /// </summary>
        /// <param name="customSettings">Custom settings object</param>
        void IHandler.InitializeCustomSettings(object customSettings)
        {
            Settings = (TSet)customSettings;
        }

        /// <summary>
        /// Method to initialize the handler, can be overriden, does not do anything otherwise
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// Gets the total number of jobs which can be added
        /// </summary>
        public virtual long GetTotalJobCount()
        {
            return -1;
        }

        /// <summary>
        /// Method to get the next batch of pending jobs
        /// </summary>
        public virtual void CreateMoreJobs() { }

        /// <summary>
        /// Event when a new job is enqueued
        /// </summary>
        private Action<IJobInput, object> _enqueueJobHandler;
        event Action<IJobInput, object> IHandler.EnqueueJobEvent
        {
            add { _enqueueJobHandler += value; }
            // ReSharper disable once DelegateSubtraction
            remove { _enqueueJobHandler -= value; }
        }

        /// <summary>
        /// The logger object
        /// </summary>
        public Logger Logger { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        protected HandlerBase()
        {
            // Initialize the logger
            Logger = new Logger();
        }

        /// <summary>
        /// Enqueues a new job
        /// </summary>
        public void EnqueueJob(TIn jobInput, object additionalData = null)
        {
            Interlocked.Increment(ref _jobCount);
            OnEnqueueJob(jobInput, additionalData);
        }

        /// <summary>
        /// Handler when the enqueue job is fired
        /// </summary>
        private void OnEnqueueJob(IJobInput jobInput, object additionalData)
        {
            var handler = _enqueueJobHandler;
            if (handler != null) handler(jobInput, additionalData);
        }

        /// <summary>
        /// Converts the result to the generic result object and calls the abstract method
        /// to process the data
        /// </summary>
        public void ProcessResult(IJobInput jobInput, string jobResultString)
        {
            var output = JobObjectSerializer.Deserialize<TOut>(jobResultString);
            ProcessResult((TIn)jobInput, output);
            Interlocked.Decrement(ref _jobCount);
        }

        /// <summary>
        /// Method to process the result of a finished job
        /// </summary>
        public abstract void ProcessResult(TIn jobInput, TOut jobResult);

        bool IHandler.IsSameAs(IHandler otherHandler)
        {
            // If parameter is null, return false
            if (ReferenceEquals(otherHandler, null))
            {
                return false;
            }

            // Optimization for a common success case
            if (ReferenceEquals(this, otherHandler))
            {
                return true;
            }

            // If run-time types are not exactly the same, return false
            if (GetType() != otherHandler.GetType())
            {
                return false;
            }
            return IsSameSpecific((HandlerBase<TSet, TIn, TOut>)otherHandler);
        }

        /// <summary>
        /// Optional custom function for further equality checks
        /// </summary>
        /// <param name="otherHandler"></param>
        /// <returns></returns>
        public virtual bool IsSameSpecific(HandlerBase<TSet, TIn, TOut> otherHandler)
        {
            return true;
        }

        public virtual void OnStart() { }
        public virtual void OnStop() { }
        public virtual void OnFinished() { }
    }
}
