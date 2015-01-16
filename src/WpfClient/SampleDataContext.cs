using System;
using System.Linq;
using WpfClient.ViewModels;

namespace WpfClient
{
    public static class SampleDataContext
    {
        public static MainViewModel MainViewModel { get; private set; }

        static SampleDataContext()
        {
            MainViewModel = new MainViewModel
            {
                Version = "1.0.3.44"
            };
            MainViewModel.Jobs.Add(new JobInfoViewModel
            {
                HandlerId = Guid.NewGuid(),
                JobId = Guid.NewGuid(),
                JobInput = "{someinput}",
                StartDate = DateTime.Now.AddMinutes(-2)
            });
            MainViewModel.Jobs.Add(new JobInfoViewModel
            {
                HandlerId = Guid.NewGuid(),
                JobId = Guid.NewGuid(),
                JobInput = "{someinput2}",
                StartDate = DateTime.Now.AddMinutes(-3)
            });
            MainViewModel.SelectedItem = MainViewModel.Jobs.Last();
        }
    }
}
