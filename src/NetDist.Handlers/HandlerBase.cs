using NetDist.Jobs;
using System.Collections.Generic;

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
        /// Converts the passed settings string to the generic settings object
        /// </summary>
        /// <param name="customSettings">Custom settings object</param>
        public void InitializeCustomSettings(object customSettings)
        {
            Settings = (TSet)customSettings;
        }

        /// <summary>
        /// Method to initialize the handler, can be overriden, does not do anything otherwise
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// Method to get the next batch of pending jobs
        /// </summary>
        /// <returns>A list of input parameters which should be processed as jobs</returns>
        public abstract List<TIn> GetJobs();

        /// <summary>
        /// Interface implementation to get a list of jobs
        /// </summary>
        /// <returns>A list of input parameters</returns>
        List<IJobInput> IHandler.GetJobs()
        {
            return GetJobs().ConvertAll(x => (IJobInput)x);
        }

        /// <summary>
        /// Converts the result to the generic result object and calls the abstract method
        /// to process the data
        /// </summary>
        public void ProcessResult(IJobInput jobInput, string jobResultString)
        {
            var output = JobObjectSerializer.Deserialize<TOut>(jobResultString);
            ProcessResult((TIn)jobInput, output);
        }

        /// <summary>
        /// Method to process the result of a finished job
        /// </summary>
        public abstract void ProcessResult(TIn jobInput, TOut jobResult);

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

        public virtual void OnStart() { }
        public virtual void OnStop() { }
        public virtual void OnFinished() { }
    }
}
