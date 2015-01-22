using NetDist.Client;
using NetDist.Core.Utilities;
using NetDist.Jobs.DataContracts;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;
using Wpf.Shared;
using WpfClient.Models;

namespace WpfClient.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly MainModel _model;
        private readonly ObservableViewModelCollection<JobInfoViewModel, Job> _jobs;
        private PropertyChangedProxy<ClientBase, ClientStatusType> _statusPropertyChangedProxy;

        public ClientStatusType Status { get { return _model.Client.Status; } }

        public string Version { get { return _model.Version; } }

        public int NumberOfParallelTasks
        {
            get { return _model.Client.NumberOfParallelJobs; }
            set { _model.Client.NumberOfParallelJobs = value; OnPropertyChanged(() => NumberOfParallelTasks); }
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
        public ICommand ShowSettingsCommand { get; private set; }
        public ICommand AddSingleJobCommand { get; private set; }
        public ICommand StartCommand { get; private set; }
        public ICommand StopCommand { get; private set; }
        #endregion

        public MainViewModel(MainModel model)
        {
            _model = model;
            _jobs = new ObservableViewModelCollection<JobInfoViewModel, Job>(Dispatcher.CurrentDispatcher, model.Client.Jobs, job => new JobInfoViewModel(job));
            _statusPropertyChangedProxy = new PropertyChangedProxy<ClientBase, ClientStatusType>(_model.Client, m => m.Status, newValue =>
            {
                OnPropertyChanged(() => Status);
                OnPropertyChanged(() => IsStarted);
                OnPropertyChanged(() => IsStopped);
            });

            // Initialize the network adapters
            NetworkAdapters = new ObservableCollection<string>(_model.NetworkAnalyzer.GetNetworkAdapters());
            SelectedNetworkAdapter = NetworkAdapters[0];

            // Initialize commands
            ShowSettingsCommand = new RelayCommand(o =>
            {
                const string settingsFile = "settings.json";
                Process.Start("notepad.exe", settingsFile);
            });
            AddSingleJobCommand = new RelayCommand(o => model.Client.GetAndStartJob());
            StartCommand = new RelayCommand(o => model.Client.StartProcessing());
            StopCommand = new RelayCommand(o => model.Client.StopProcessing());

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
