using NetDist.Jobs;

namespace SimpleCalculator.Shared
{
    public class CalculatorJobOutput : IJobOutput
    {
        public int Result { get; set; }
    }
}
