using NetDist.Core.Utilities;
using NetDist.Jobs;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;
using Wpf.Shared;
using WpfClient.Models;

namespace WpfClient.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly MainModel _model;

        public string Version { get { return _model.Version; } }

        public int NumberOfParallelTasks
        {
            get { return _model.NumberOfParallelTasks; }
            set { _model.NumberOfParallelTasks = value; OnPropertyChanged(() => NumberOfParallelTasks); }
        }

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
                _model.NetworkAnalyzer.CurrentAdapter = value;
                OnPropertyChanged(() => TrafficIn);
                OnPropertyChanged(() => TrafficOut);
            }
        }

        public string TrafficIn
        {
            get { return SizeSuffix.AddSizeSuffix((ulong)_model.NetworkAnalyzer.TotalTrafficIn); }
        }

        public string TrafficOut
        {
            get { return SizeSuffix.AddSizeSuffix((ulong)_model.NetworkAnalyzer.TotalTrafficOut); }
        }

        private readonly ObservableViewModelCollection<JobInfoViewModel, Job> _jobs;
        public ObservableCollection<JobInfoViewModel> Jobs
        {
            get { return _jobs; }
        }

        public MainViewModel(MainModel model)
        {
            _model = model;
            _jobs = new ObservableViewModelCollection<JobInfoViewModel, Job>(model.Jobs, job => new JobInfoViewModel(job));

            NetworkAdapters = new ObservableCollection<string>(_model.NetworkAnalyzer.GetNetworkAdapters());
            SelectedNetworkAdapter = NetworkAdapters[0];

            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                // Dispatcher to update the display of some values
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
