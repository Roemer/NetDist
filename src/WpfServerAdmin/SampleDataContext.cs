using NetDist.Core;
using NetDist.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
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
                PackageName = "Plugin 1",
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
                PackageName = "Plugin 2",
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
                PackageName = "Plugin 3",
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

            MainInfoViewModel.Handlers.Add(new HandlerInfoViewModel(new HandlerInfo
            {
                Id = Guid.NewGuid(),
                PackageName = "Plugin 3",
                HandlerName = "Handler 3",
                JobName = "Job 1",
                HandlerState = HandlerState.Idle,
                TotalJobsAvailable = 0,
                JobsPending = 0,
                JobsAvailable = 0,
                TotalJobsProcessed = 77744,
                TotalJobsFailed = 1,
                LastStartTime = DateTime.Now,
                NextStartTime = DateTime.Now.AddMinutes(4)
            }));

            MainInfoViewModel.Handlers.Add(new HandlerInfoViewModel(new HandlerInfo
            {
                Id = Guid.NewGuid(),
                PackageName = "Plugin 4",
                HandlerName = "Handler 4",
                JobName = "Job 1",
                HandlerState = HandlerState.Disabled,
                TotalJobsAvailable = 0,
                JobsPending = 0,
                JobsAvailable = 0,
                TotalJobsProcessed = 53,
                TotalJobsFailed = 0,
                LastStartTime = DateTime.Now,
                NextStartTime = DateTime.Now.AddMinutes(4)
            }));

            MainInfoViewModel.Handlers.Add(new HandlerInfoViewModel(new HandlerInfo
            {
                Id = Guid.NewGuid(),
                PackageName = "Plugin 5",
                HandlerName = "Handler 5",
                JobName = "Job 1",
                HandlerState = HandlerState.Failed,
                TotalJobsAvailable = 0,
                JobsPending = 0,
                JobsAvailable = 0,
                TotalJobsProcessed = 0,
                TotalJobsFailed = 1337,
                LastStartTime = DateTime.Now,
                NextStartTime = DateTime.Now.AddMinutes(4)
            }));

            // Clients
            for (int i = 0; i < 5; i++)
            {
                MainInfoViewModel.Clients.Add(GenerateClientInfoViewModel());
            }
            MainInfoViewModel.ActiveClientsCount = MainInfoViewModel.Clients.Count(i => !i.IsOffline);

            PackageUploadViewModel = new PackageUploadViewModel
            {
                PackageName = @"ExamplePackage"
            };
            PackageUploadViewModel.HandlerAssemblies.Add("handler1.dll");
            PackageUploadViewModel.HandlerAssemblies.Add("handler2.dll");
            PackageUploadViewModel.Dependencies.Add("somefile.dll");
            PackageUploadViewModel.Dependencies.Add("someotherfile.dll");
        }

        private static ClientInfoViewModel GenerateClientInfoViewModel()
        {
            var minutesSinceStart = RandomGenerator.Instance.Next(120);
            var minutesSinceLastCommunication = Math.Min(minutesSinceStart, RandomGenerator.Instance.Next(20));
            var extendedClientInfo = new ExtendedClientInfo
            {
                JobsInProgress = RandomGenerator.Instance.Next(0, 10),
                TotalJobsFailed = RandomGenerator.Instance.Next(0, 100),
                TotalJobsProcessed = RandomGenerator.Instance.Next(0, 10000),
                LastCommunicationDate = DateTime.Now.AddMinutes(minutesSinceLastCommunication * -1),
                ClientInfo = new ClientInfo
                {
                    Id = Guid.NewGuid(),
                    Name = String.Format("Client {0}", RandomGenerator.Instance.Next(50)),
                    Version = "1.0.0.60",
                    TotalMemory = 8589934592,
                    UsedMemory = RandomGenerator.Instance.NextUInt64(0, 8589934592),
                    StartDate = DateTime.Now.AddMinutes(minutesSinceStart * -1),
                    DiskInformations = new List<DiskInformation> {
                        new DiskInformation
                        {
                            Label = "C",
                            TotalDiskSpace = 83886080,
                            FreeDiskSpace = RandomGenerator.Instance.NextUInt64(0, 20971520),
                        }
                    },
                    CpuUsage = (float)(RandomGenerator.Instance.NextDouble() * 100),
                }
            };
            return new ClientInfoViewModel(extendedClientInfo);
        }
    }
}
