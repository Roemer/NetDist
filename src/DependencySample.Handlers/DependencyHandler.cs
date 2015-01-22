using DependencySample.ServerDependency;
using DependencySample.Shared;
using NetDist.Handlers;
using System;

namespace DependencySample.Handlers
{
    [HandlerNameAttribute("Dependency")]
    public class DependencyHandler : HandlerBase<DependencyHandlerSettings, DependencyJobInput, DependencyJobOutput>
    {
        public override void CreateMoreJobs()
        {
            EnqueueJob(new DependencyJobInput { Text = StringGenerator.GetString() });
        }

        public override void ProcessResult(DependencyJobInput jobInput, DependencyJobOutput jobResult)
        {
            Console.WriteLine("{0} -> {1}", jobInput.Text, jobResult.Text);
        }
    }
}
