using NetDist.Handlers;
using NetDist.Logging;
using SimpleCalculator.Shared;
using System;

namespace SimpleCalculator.Handlers
{
    [HandlerNameAttribute("Calculator")]
    public class CalculatorHandler : HandlerBase<CalculatorHandlerSettings, CalculatorJobInput, CalculatorJobOutput>
    {
        public override void Initialize()
        {
            // Append a new logger
            Logger.LogEvent += new FileLogger("Calculator", LogLevel.Debug).Log;
            Logger.Debug("NegateResult: {0}", Settings.NegateResult);
        }

        public override void CreateMoreJobs()
        {
            Logger.Debug("Adding 50 more jobs");
            var random = new Random();
            for (var i = 0; i < 50; i++)
            {
                EnqueueJob(new CalculatorJobInput(random.Next(10), random.Next(100)));
            }
        }

        public override void ProcessResult(CalculatorJobInput jobInput, CalculatorJobOutput jobResult)
        {
            Logger.Info("Result: {0}", jobResult.Result * (Settings.NegateResult ? -1 : 1));
        }
    }
}
