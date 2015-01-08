using NetDist.Jobs;

namespace SimpleCalculator.Shared
{
    public class CalculatorJobInput : IJobInput
    {
        public int Number1 { get; set; }
        public int Number2 { get; set; }

        public CalculatorJobInput(int number1, int number2)
        {
            Number1 = number1;
            Number2 = number2;
        }
    }
}
