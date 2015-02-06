using NetDist.Client;
using NetDist.Core.Utilities;
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
        private PropertyChangedProxy<ClientBase, ClientStatusType> _statusPropertyChangedProxy;
        private PropertyChangedProxy<ClientBase, bool> _connectionPropertyChangedProxy;

        public ClientStatusType Status { get { return _model.Client.Status; } }

        public bool IsConnected { get { return _model.Client.IsServerReachable; } }

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

        public ObservableCollection<JobInfoViewModel> Jobs { get; set; }

        #region Commands
        public ICommand ShowSettingsCommand { get; private set; }
        public ICommand CheckForUpdateCommand { get; private set; }
        public ICommand AddSingleJobCommand { get; private set; }
        public ICommand StartCommand { get; private set; }
        public ICommand StopCommand { get; private set; }
        #endregion

        public MainViewModel(MainModel model)
        {
            _model = model;
            Jobs = new ObservableCollection<JobInfoViewModel>();

            var dispatcher = Dispatcher.CurrentDispatcher;
            model.Client.JobAddedEvent += (sender, args) =>
            {
                dispatcher.Invoke(DispatcherPriority.DataBind, new Action(
                    () => Jobs.Add(new JobInfoViewModel(args.ClientJob))));
            };
            model.Client.JobRemovedEvent += (sender, args) =>
            {
                dispatcher.Invoke(DispatcherPriority.DataBind, new Action(
                    () => Jobs.Remove(new JobInfoViewModel(args.ClientJob))));
            };

            _statusPropertyChangedProxy = new PropertyChangedProxy<ClientBase, ClientStatusType>(_model.Client, m => m.Status, newValue =>
            {
                OnPropertyChanged(() => Status);
                OnPropertyChanged(() => IsStarted);
                OnPropertyChanged(() => IsStopped);
            });

            _connectionPropertyChangedProxy = new PropertyChangedProxy<ClientBase, bool>(model.Client, m => m.IsServerReachable, b => OnPropertyChanged(() => IsConnected));

            // Initialize the network adapters
            NetworkAdapters = new ObservableCollection<string>(_model.NetworkAnalyzer.GetNetworkAdapters());
            SelectedNetworkAdapter = NetworkAdapters[0];

            // Initialize commands
            ShowSettingsCommand = new RelayCommand(o =>
            {
                const string settingsFile = "settings.json";
                Process.Start("notepad.exe", settingsFile);
            });
            CheckForUpdateCommand = new RelayCommand(o => _model.CheckAndUpdate());
            AddSingleJobCommand = new RelayCommand(o => model.Client.ManuallyStartJob());
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
