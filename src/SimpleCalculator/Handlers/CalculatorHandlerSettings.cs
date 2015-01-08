using NetDist.Handlers;

namespace SimpleCalculator.Handlers
{
    public class CalculatorHandlerSettings : IHandlerCustomSettings
    {
        public bool NegateResult { get; set; }
    }
}
