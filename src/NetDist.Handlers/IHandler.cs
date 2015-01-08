using NetDist.Jobs;
using System;
using System.Collections.Generic;

namespace NetDist.Handlers
{
    /// <summary>
    /// Interface for all handlers
    /// </summary>
    public interface IHandler
    {
        Guid Id { get; }
        void InitializeCustomSettings(string customSettingsString);
        void Initialize();
        List<Job> GetJobs();
        void ProcessResult(Job originalJob, string jobResultString);
        bool IsSameAs(IHandler otherHandler);
    }
}
