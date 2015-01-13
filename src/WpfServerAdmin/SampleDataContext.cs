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
                AvailableJobs = 145,
                PendingJobs = 10,
                TotalJobs = 15123
            });

            MainInfoViewModel.Handlers.Add(new HandlerInfoViewModel
            {
                Id = Guid.NewGuid(),
                Name = "Handler 2",
                HandlerState = HandlerState.Stopped,
                AvailableJobs = 0,
                PendingJobs = 0,
                TotalJobs = 0
            });

            MainInfoViewModel.Handlers.Add(new HandlerInfoViewModel
            {
                Id = Guid.NewGuid(),
                Name = "Handler 3",
                HandlerState = HandlerState.Finished,
                AvailableJobs = 0,
                PendingJobs = 0,
                TotalJobs = 0
            });
        }
    }
}
