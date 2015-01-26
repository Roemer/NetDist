#if NETDISTCOMPILERLIBRARIES
System.dll
NetDist.Jobs.dll
DependencySample.ClientDependency.dll
DependencySample.Shared.dll
#endif

#if NETDISTDEPENDENCIES
DependencySample.ClientDependency.dll
DependencySample.Shared.dll
#endif

#if NETDISTPACKAGE
DependencySample.Handlers
#endif

using DependencySample.ClientDependency;
using DependencySample.Shared;
using NetDist.Jobs;

namespace DependencySample.Jobs
{
    public class DependencyJob1HandlerInitializer : JobHandlerInitializerBase<DependencyHandlerSettings>
    {
        public override void FillJobHandlerSettings(HandlerSettings handlerSettings)
        {
            handlerSettings.HandlerName = "Dependency";
            handlerSettings.JobName = "Dependency - 1";
            handlerSettings.AutoStart = true;
        }

        public override void FillCustomSettings(DependencyHandlerSettings customSettings)
        {
            customSettings.ToUpper = true;
        }
    }
    public class DependencyJob1 : JobScriptBase<DependencyJobInput, DependencyJobOutput>
    {
        public override DependencyJobOutput Process(DependencyJobInput input)
        {
            var output = new DependencyJobOutput { Text = StringReverter.ReverseString(input.Text) };
            return output;
        }
    }
}
