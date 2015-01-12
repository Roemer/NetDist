using NetDist.Jobs;
using System.Collections.Generic;

namespace NetDist.Handlers
{
    /// <summary>
    /// Interface for the handlers
    /// </summary>
    public interface IHandler
    {
        void InitializeCustomSettings(string customSettingsString);
        void Initialize();
        List<IJobInput> GetJobs();
        void ProcessResult(IJobInput jobInput, IJobOutput jobOutput);
        bool IsSameAs(IHandler otherHandler);
    }
}
