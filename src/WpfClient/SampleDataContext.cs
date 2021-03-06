﻿using NetDist.Client;
using NetDist.Jobs.DataContracts;
using System;
using System.Linq;
using WpfClient.Models;
using WpfClient.ViewModels;

namespace WpfClient
{
    public static class SampleDataContext
    {
        public static MainViewModel MainViewModel { get; private set; }

        static SampleDataContext()
        {
            var mainModel = new MainModel
            {
                Version = "1.0.3.44"
            };
            MainViewModel = new MainViewModel(mainModel);
            MainViewModel.Jobs.Add(new JobInfoViewModel(new ClientJob(new Job(Guid.NewGuid(), Guid.NewGuid(), "", ""))));
            MainViewModel.Jobs.Add(new JobInfoViewModel(new ClientJob(new Job(Guid.NewGuid(), Guid.NewGuid(), "", ""))));
            MainViewModel.Jobs.Add(new JobInfoViewModel(new ClientJob(new Job(Guid.NewGuid(), Guid.NewGuid(), "", ""))));
            MainViewModel.SelectedItem = MainViewModel.Jobs.Last();
        }
    }
}
