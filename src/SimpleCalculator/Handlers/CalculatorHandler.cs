using System;
using System.Collections.Generic;
using NetDist.Handlers;
using NetDist.Jobs;
using SimpleCalculator.Shared;

namespace SimpleCalculator.Handlers
{
    public class CalculatorHandler : HandlerBase<CalculatorHandlerSettings, CalculatorJobInput, CalculatorJobOutput>
    {
        public override List<Job> GetJobs()
        {
            var jobList = new List<Job>();
            var random = new Random();
            for (var i = 0; i < 50; i++)
            {
                jobList.Add(CreateJob(new CalculatorJobInput(random.Next(10), random.Next(100))));
            }
            return jobList;
        }

        public override void ProcessResult(Job originalJob, CalculatorJobOutput jobResult)
        {
            Console.WriteLine("Result: {0}", jobResult.Result * (Settings.NegateResult ? -1 : 1));
        }
    }
}
