using System;
using System.Linq;
using NetDist.Jobs;
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
            mainModel.Client.Jobs.Add(new Job(Guid.NewGuid(), null));
            mainModel.Client.Jobs.Add(new Job(Guid.NewGuid(), null));
            MainViewModel.SelectedItem = MainViewModel.Jobs.Last();
        }
    }
}
