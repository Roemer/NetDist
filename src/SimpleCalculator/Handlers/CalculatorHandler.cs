using NetDist.Handlers;
using SimpleCalculator.Shared;
using System;

namespace SimpleCalculator.Handlers
{
    [HandlerNameAttribute("Calculator")]
    public class CalculatorHandler : HandlerBase<CalculatorHandlerSettings, CalculatorJobInput, CalculatorJobOutput>
    {
        public override void Initialize()
        {
            Console.WriteLine("NegateResult: {0}", Settings.NegateResult);
        }

        public override void CreateMoreJobs()
        {
            Logger.Info("Adding 50 more jobs");
            var random = new Random();
            for (var i = 0; i < 50; i++)
            {
                EnqueueJob(new CalculatorJobInput(random.Next(10), random.Next(100)));
            }
        }

        public override void ProcessResult(CalculatorJobInput jobInput, CalculatorJobOutput jobResult)
        {
            Console.WriteLine("Result: {0}", jobResult.Result * (Settings.NegateResult ? -1 : 1));
        }
    }
}
