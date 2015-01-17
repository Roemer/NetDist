using NetDist.Jobs;

namespace SimpleCalculator.Shared
{
    public class CalculatorHandlerSettings : IHandlerCustomSettings
    {
        public bool NegateResult { get; set; }
    }
}
