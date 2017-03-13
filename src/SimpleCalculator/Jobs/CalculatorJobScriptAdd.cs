#if NETDISTCOMPILERLIBRARIES
System.dll
NetDist.Jobs.dll
SimpleCalculator.dll
#endif

#if NETDISTDEPENDENCIES
SimpleCalculator.dll
#endif

#if NETDISTPACKAGE
SimpleCalculator
#endif

using NetDist.Jobs;
using SimpleCalculator.Shared;

namespace SimpleCalculator.Jobs
{
    public class CalculatorAddHandlerInitializer : HandlerInitializerBase<CalculatorHandlerSettings>
    {
        public override void FillHandlerSettings(HandlerSettings handlerSettings)
        {
            handlerSettings.HandlerName = "Calculator";
            handlerSettings.JobName = "Calculator - Add";
            handlerSettings.AutoStart = true;
            handlerSettings.Schedule = "* * * * *";
            handlerSettings.MaxParallelJobs = 2;
        }

        public override void FillCustomSettings(CalculatorHandlerSettings customSettings)
        {
            customSettings.NegateResult = true;
        }
    }

    public class CalculatorJobScriptAdd : JobScriptBase<CalculatorJobInput, CalculatorJobOutput>
    {
        public override CalculatorJobOutput Process(CalculatorJobInput input)
        {
            var result = new CalculatorJobOutput { Result = input.Number1 + input.Number2 };
            return result;
        }
    }
}
