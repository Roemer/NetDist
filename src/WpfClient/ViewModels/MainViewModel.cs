using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;
using NetDist.Core.Utilities;
using Wpf.Shared;

namespace WpfClient.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        public string Version { get; set; }

        public ObservableCollection<JobInfoViewModel> Jobs { get; set; }

        public ObservableCollection<string> NetworkAdapters { get; set; }

        public JobInfoViewModel SelectedItem
        {
            get { return GetProperty<JobInfoViewModel>(); }
            set { SetProperty(value); }
        }

        public string SelectedNetworkAdapter
        {
            get { return GetProperty<string>(); }
            set
            {
                SetProperty(value);
                _networkAnalyzer.CurrentAdapter = value;
                OnPropertyChanged(() => TrafficIn);
                OnPropertyChanged(() => TrafficOut);
            }
        }

        public string TrafficIn
        {
            get { return SizeSuffix.AddSizeSuffix((ulong)_networkAnalyzer.TotalTrafficIn); }
        }

        public string TrafficOut
        {
            get { return SizeSuffix.AddSizeSuffix((ulong)_networkAnalyzer.TotalTrafficOut); }
        }

        private readonly NetworkTrafficAnalyzer _networkAnalyzer = new NetworkTrafficAnalyzer();

        public MainViewModel()
        {
            Jobs = new ObservableCollection<JobInfoViewModel>();
            NetworkAdapters = new ObservableCollection<string>(_networkAnalyzer.GetNetworkAdapters());
            SelectedNetworkAdapter = NetworkAdapters[0];

            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                var timer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 1) };
                timer.Tick += (sender, args) =>
                {
                    foreach (var job in Jobs)
                    {
                        job.RefreshDuration();
                    }
                };
                timer.Tick += (sender, args) =>
                {
                    OnPropertyChanged(() => TrafficIn);
                    OnPropertyChanged(() => TrafficOut);
                };
                timer.Start();
            }
        }
    }
}
