using NetDist.Handlers;
using SimpleCalculator.Shared;
using System;
using System.Collections.Generic;

namespace SimpleCalculator.Handlers
{
    [HandlerNameAttribute("Calculator")]
    public class CalculatorHandler : HandlerBase<CalculatorHandlerSettings, CalculatorJobInput, CalculatorJobOutput>
    {
        public override List<CalculatorJobInput> GetJobs()
        {
            var jobList = new List<CalculatorJobInput>();
            var random = new Random();
            for (var i = 0; i < 50; i++)
            {
                jobList.Add(new CalculatorJobInput(random.Next(10), random.Next(100)));
            }
            return jobList;
        }

        public override void ProcessResult(CalculatorJobInput jobInput, CalculatorJobOutput jobResult)
        {
            Console.WriteLine("Result: {0}", jobResult.Result * (Settings.NegateResult ? -1 : 1));
        }
    }
}
