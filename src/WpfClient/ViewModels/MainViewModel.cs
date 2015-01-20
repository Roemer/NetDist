using NetDist.Core.Utilities;
using NetDist.Jobs;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Security.Permissions;
using System.Windows.Input;
using System.Windows.Threading;
using Wpf.Shared;
using WpfClient.Common;
using WpfClient.Models;

namespace WpfClient.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly MainModel _model;
        private readonly ObservableViewModelCollection<JobInfoViewModel, Job> _jobs;
        private PropertyChangedProxy<MainModel, ClientStatusType> _statusPropertyChangedProxy;

        public ClientStatusType Status { get { return _model.Status; } }

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

        public bool IsStarted
        {
            get { return Status != ClientStatusType.Idle; }
        }

        public bool IsStopped
        {
            get { return Status == ClientStatusType.Idle; }
        }

        public string TrafficIn
        {
            get { return SizeSuffix.AddSizeSuffix((ulong)_model.NetworkAnalyzer.TotalTrafficIn); }
        }

        public string TrafficOut
        {
            get { return SizeSuffix.AddSizeSuffix((ulong)_model.NetworkAnalyzer.TotalTrafficOut); }
        }

        public ObservableCollection<JobInfoViewModel> Jobs
        {
            get { return _jobs; }
        }

        #region Commands
        public ICommand AddSingleJobCommand { get; private set; }
        public ICommand StartCommand { get; private set; }
        public ICommand StopCommand { get; private set; }
        #endregion

        public MainViewModel(MainModel model)
        {
            _model = model;
            _jobs = new ObservableViewModelCollection<JobInfoViewModel, Job>(Dispatcher.CurrentDispatcher, model.Jobs, job => new JobInfoViewModel(job));
            _statusPropertyChangedProxy = new PropertyChangedProxy<MainModel, ClientStatusType>(_model, m => m.Status, newValue =>
            {
                OnPropertyChanged(() => Status);
                OnPropertyChanged(() => IsStarted);
                OnPropertyChanged(() => IsStopped);
            });

            // Initialize the network adapters
            NetworkAdapters = new ObservableCollection<string>(_model.NetworkAnalyzer.GetNetworkAdapters());
            SelectedNetworkAdapter = NetworkAdapters[0];

            // Initialize commands
            AddSingleJobCommand = new RelayCommand(o => model.GetJob());
            StartCommand = new RelayCommand(o => model.Start());
            StopCommand = new RelayCommand(o => model.Stop());

            // Initialize run-time only timer to update the display of some values
            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
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
