using DependencySample.ServerDependency;
using DependencySample.Shared;
using NetDist.Handlers;
using System;
using System.Collections.Generic;

namespace DependencySample.Handlers
{
    [HandlerNameAttribute("Dependency")]
    public class DependencyHandler : HandlerBase<DependencyHandlerSettings, DependencyJobInput, DependencyJobOutput>
    {
        public override List<DependencyJobInput> GetJobs()
        {
            var list = new List<DependencyJobInput>();
            list.Add(new DependencyJobInput { Text = StringGenerator.GetString() });
            return list;
        }

        public override void ProcessResult(DependencyJobInput jobInput, DependencyJobOutput jobResult)
        {
            Console.WriteLine("{0} -> {1}", jobInput.Text, jobResult.Text);
        }
    }
}
