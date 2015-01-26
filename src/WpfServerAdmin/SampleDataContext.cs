using System;
using NetDist.Core;
using NetDist.Core.Utilities;
using WpfServerAdmin.ViewModels;

namespace WpfServerAdmin
{
    public static class SampleDataContext
    {
        public static MainInfoViewModel MainInfoViewModel { get; private set; }
        public static PackageUploadViewModel PackageUploadViewModel { get; private set; }

        static SampleDataContext()
        {
            MainInfoViewModel = new MainInfoViewModel
            {
                TotalMemory = 123456789,
                UsedMemory = 103456789,
                Cpu = 12.442112f,
                IsConnected = true
            };

            // Handlers
            MainInfoViewModel.Handlers.Add(new HandlerInfoViewModel(new HandlerInfo
            {
                Id = Guid.NewGuid(),
                PluginName = "Plugin 1",
                HandlerName = "Handler 1",
                JobName = "Job 1",
                HandlerState = HandlerState.Running,
                TotalJobsAvailable = 12323145,
                JobsPending = 10,
                JobsAvailable = 15123,
                TotalJobsProcessed = 63366,
                TotalJobsFailed = 12,
                LastStartTime = DateTime.Now,
                NextStartTime = DateTime.Now.AddMinutes(4)
            }));

            MainInfoViewModel.Handlers.Add(new HandlerInfoViewModel(new HandlerInfo
            {
                Id = Guid.NewGuid(),
                PluginName = "Plugin 2",
                HandlerName = "Handler 1",
                JobName = "Job 3",
                HandlerState = HandlerState.Stopped,
                TotalJobsAvailable = 445,
                JobsPending = 0,
                JobsAvailable = 0,
                TotalJobsProcessed = 744,
                TotalJobsFailed = 454,
                LastStartTime = DateTime.Now,
                NextStartTime = DateTime.Now.AddMinutes(4)
            }));

            MainInfoViewModel.Handlers.Add(new HandlerInfoViewModel(new HandlerInfo
            {
                Id = Guid.NewGuid(),
                PluginName = "Plugin 3",
                HandlerName = "Handler 2",
                JobName = "Job 1",
                HandlerState = HandlerState.Finished,
                TotalJobsAvailable = 0,
                JobsPending = 0,
                JobsAvailable = 0,
                TotalJobsProcessed = 77744,
                TotalJobsFailed = 1,
                LastStartTime = DateTime.Now,
                NextStartTime = DateTime.Now.AddMinutes(4)
            }));

            // Clients
            MainInfoViewModel.Clients.Add(new ClientInfoViewModel
            {
                Id = Guid.NewGuid(),
                Name = "Client 1",
                Version = "1.0.0.60",
                TotalMemory = 8589934592,
                UsedMemory = RandomGenerator.Instance.NextUInt64(0, 8589934592),
                Cpu = (float)(RandomGenerator.Instance.NextDouble() * 100),
                JobsInProgress = RandomGenerator.Instance.Next(0, 10),
                TotalJobsFailed = RandomGenerator.Instance.Next(0, 100),
                TotalJobsProcessed = RandomGenerator.Instance.Next(0, 10000),
                LastUpdate = DateTime.Now.AddMinutes(-1)
            });

            MainInfoViewModel.Clients.Add(new ClientInfoViewModel
            {
                Id = Guid.NewGuid(),
                Name = "Client 2",
                Version = "1.0.0.60",
                TotalMemory = 8589934592,
                UsedMemory = RandomGenerator.Instance.NextUInt64(0, 8589934592),
                Cpu = (float)(RandomGenerator.Instance.NextDouble() * 100),
                JobsInProgress = RandomGenerator.Instance.Next(0, 10),
                TotalJobsFailed = RandomGenerator.Instance.Next(0, 100),
                TotalJobsProcessed = RandomGenerator.Instance.Next(0, 10000),
                LastUpdate = DateTime.Now.AddMinutes(-2)
            });

            MainInfoViewModel.Clients.Add(new ClientInfoViewModel
            {
                Id = Guid.NewGuid(),
                Name = "Client 3",
                Version = "1.0.0.59",
                TotalMemory = 8589934592,
                UsedMemory = RandomGenerator.Instance.NextUInt64(0, 8589934592),
                Cpu = (float)(RandomGenerator.Instance.NextDouble() * 100),
                JobsInProgress = RandomGenerator.Instance.Next(0, 10),
                TotalJobsFailed = RandomGenerator.Instance.Next(0, 100),
                TotalJobsProcessed = RandomGenerator.Instance.Next(0, 10000),
                LastUpdate = DateTime.Now.AddMinutes(-3)
            });

            MainInfoViewModel.Clients.Add(new ClientInfoViewModel
            {
                Id = Guid.NewGuid(),
                Name = "Client 4",
                Version = "1.0.0.60",
                TotalMemory = 8589934592,
                UsedMemory = RandomGenerator.Instance.NextUInt64(0, 8589934592),
                Cpu = (float)(RandomGenerator.Instance.NextDouble() * 100),
                JobsInProgress = RandomGenerator.Instance.Next(0, 10),
                TotalJobsFailed = RandomGenerator.Instance.Next(0, 100),
                TotalJobsProcessed = RandomGenerator.Instance.Next(0, 10000),
                LastUpdate = DateTime.Now.AddMinutes(-5)
            });

            PackageUploadViewModel = new PackageUploadViewModel
            {
                PackageName = @"ExamplePackage"
            };
            PackageUploadViewModel.HandlerAssemblies.Add("handler1.dll");
            PackageUploadViewModel.HandlerAssemblies.Add("handler2.dll");
            PackageUploadViewModel.Dependencies.Add("somefile.dll");
            PackageUploadViewModel.Dependencies.Add("someotherfile.dll");
        }
    }
}
