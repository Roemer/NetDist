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
    public class CalculatorSubJobHandlerInitializer : JobHandlerInitializerBase<CalculatorHandlerSettings>
    {
        public override void FillJobHandlerSettings(HandlerSettings handlerSettings)
        {
            handlerSettings.HandlerName = "Calculator";
            handlerSettings.JobName = "Calculator - Sub";
            handlerSettings.AutoStart = true;
        }

        public override void FillCustomSettings(CalculatorHandlerSettings customSettings)
        {
            customSettings.NegateResult = false;
        }
    }

    public class CalculatorJobScriptSub : JobScriptBase<CalculatorJobInput, CalculatorJobOutput>
    {
        public override CalculatorJobOutput Process(CalculatorJobInput input)
        {
            var result = new CalculatorJobOutput { Result = input.Number1 - input.Number2 };
            return result;
        }
    }
}
