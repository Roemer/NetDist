#if COMPILERSETTINGS
{
    Libraries: [
        'System.dll'
        , 'NetDist.Jobs.dll'
    ],
    LibrariesToLoad: [
        'SimpleCalculator.dll'
    ]
}
#endif

#if HANDLERSETTINGS
{
    "PluginName": "SimpleCalculator",
    "HandlerName": "Calculator",
    "JobName": "Calculator - Add",
}
#endif

#if HANDLERCUSTOMSETTINGS
{
    "NegateResult": true
}
#endif

#if EXAMPLEINPUT
{
    Number1: 10
    , Number2: 20
}
#endif

using NetDist.Jobs;
using SimpleCalculator.Shared;

namespace SimpleCalculator.Jobs
{
    public class CalculatorJobLogicAdd : JobLogicBase<CalculatorJobInput, CalculatorJobOutput>
    {
        public override CalculatorJobOutput Process(CalculatorJobInput input)
        {
            var result = new CalculatorJobOutput { Result = input.Number1 + input.Number2 };
            return result;
        }
    }
}
