using System;
using NetDist.Core;
using WpfServerAdmin.ViewModels;

namespace WpfServerAdmin
{
    public static class SampleDataContext
    {
        public static MainInfoViewModel MainInfoViewModel { get; private set; }

        static SampleDataContext()
        {
            MainInfoViewModel = new MainInfoViewModel
            {
                TotalMemory = 123456789,
                UsedMemory = 103456789,
                Cpu = 12.442112f,
                IsConnected = false
            };

            // Handlers
            MainInfoViewModel.Handlers.Add(new HandlerInfoViewModel
            {
                Id = Guid.NewGuid(),
                Name = "Handler 1",
                HandlerState = HandlerState.Running,
                TotalJobsAvailable = 12323145,
                JobsPending = 10,
                JobsAvailable = 15123,
                TotalJobsProcessed = 63366,
                TotalJobsFailed = 12
            });

            MainInfoViewModel.Handlers.Add(new HandlerInfoViewModel
            {
                Id = Guid.NewGuid(),
                Name = "Handler 2",
                HandlerState = HandlerState.Stopped,
                TotalJobsAvailable = 445,
                JobsPending = 0,
                JobsAvailable = 0,
                TotalJobsProcessed = 744,
                TotalJobsFailed = 454
            });

            MainInfoViewModel.Handlers.Add(new HandlerInfoViewModel
            {
                Id = Guid.NewGuid(),
                Name = "Handler 3",
                HandlerState = HandlerState.Finished,
                TotalJobsAvailable = 0,
                JobsPending = 0,
                JobsAvailable = 0,
                TotalJobsProcessed = 77744,
                TotalJobsFailed = 1
            });
        }
    }
}
