using DependencySample.ServerDependency;
using DependencySample.Shared;
using NetDist.Handlers;

namespace DependencySample.Handlers
{
    [HandlerNameAttribute("Dependency")]
    public class DependencyHandler : HandlerBase<DependencyHandlerSettings, DependencyJobInput, DependencyJobOutput>
    {
        public override void Initialize()
        {
            Logger.Debug("Settings.ToUpper: ", Settings.ToUpper);
        }

        public override void CreateMoreJobs()
        {
            var nextText = StringGenerator.GetString();
            if (Settings.ToUpper)
            {
                nextText = nextText.ToUpper();
            }
            EnqueueJob(new DependencyJobInput { Text = nextText });
        }

        public override void ProcessResult(DependencyJobInput jobInput, DependencyJobOutput jobResult)
        {
            Logger.Info("{0} -> {1}", jobInput.Text, jobResult.Text);
        }
    }
}
