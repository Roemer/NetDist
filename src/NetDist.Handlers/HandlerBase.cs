using System;
using System.Collections.Generic;
using NetDist.Jobs;

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
        /// The ID of the instance of this handler
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Instance of the settings for this handler
        /// </summary>
        public TSet Settings { get; private set; }

        /// <summary>
        /// Constructor
        /// WARNING: Custom settings are not yet initialized here
        /// </summary>
        protected HandlerBase()
        {
            Id = Guid.NewGuid();
        }

        /// <summary>
        /// Converts the passed settings string to the generic settings object
        /// </summary>
        /// <param name="settingsString">String representation of the settings</param>
        public void InitializeCustomSettings(string settingsString)
        {
            var settings = JobObjectSerializer.Deserialize<TSet>(settingsString, false);
            Settings = settings;
        }

        /// <summary>
        /// Method to initialize the handler, can be overriden, does not do anything otherwise
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// Method to create a new job iten
        /// </summary>
        /// <param name="input">The input object to pass into the job</param>
        /// <returns>A job item</returns>
        public Job CreateJob(TIn input)
        {
            return new Job(Id, input);
        }

        /// <summary>
        /// Method to get the next batch of pending jobs
        /// </summary>
        /// <returns>A list of jobs which should be processed</returns>
        public abstract List<Job> GetJobs();

        /// <summary>
        /// Converts the result to the generic result object and calls the abstract method
        /// to process the data
        /// </summary>
        public void ProcessResult(Job originalJob, IJobOutput jobOutput)
        {
            ProcessResult(originalJob, (TOut)jobOutput);
        }

        /// <summary>
        /// Method to process the result of a finished job
        /// </summary>
        public abstract void ProcessResult(Job originalJob, TOut jobResult);

        public bool IsSameAs(IHandler otherHandler)
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
            return IsSameSpecific(otherHandler);
        }

        /// <summary>
        /// Optional custom function for further equality checks
        /// </summary>
        /// <param name="otherHandler"></param>
        /// <returns></returns>
        public virtual bool IsSameSpecific(IHandler otherHandler)
        {
            return true;
        }
    }
}
