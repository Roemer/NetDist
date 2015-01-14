using NetDist.Core;
using NetDist.Core.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Windows.Input;
using Wpf.Shared;
using WpfServerAdmin.Models;

namespace WpfServerAdmin.ViewModels
{
    public class MainInfoViewModel : ObservableObject
    {
        public ServerModel ServerModel { get; set; }

        public ulong TotalMemory
        {
            get { return GetProperty<ulong>(); }
            set
            {
                SetProperty(value);
                OnPropertyChanged(() => FormattedTotalMemory);
                OnPropertyChanged(() => MemoryPercentage);
            }
        }

        public ulong UsedMemory
        {
            get { return GetProperty<ulong>(); }
            set
            {
                SetProperty(value);
                OnPropertyChanged(() => FormattedUsedMemory);
                OnPropertyChanged(() => MemoryPercentage);
            }
        }

        public float Cpu
        {
            get { return GetProperty<float>(); }
            set { SetProperty(value); }
        }

        public bool IsConnected
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public float MemoryPercentage { get { return UsedMemory / (float)TotalMemory * 100; } }

        public string FormattedTotalMemory
        {
            get { return SizeSuffix.AddSizeSuffix(TotalMemory); }
        }

        public string FormattedUsedMemory
        {
            get { return SizeSuffix.AddSizeSuffix(UsedMemory); }
        }

        public ObservableCollection<HandlerInfoViewModel> Handlers { get; set; }
        //public ObservableCollection<ClientInfoViewModel> Clients { get; set; }
        //public ObservableCollection<PackageInfoViewModel> Packages { get; set; }

        public ICommand UploadJobHandlerCommand { get; private set; }

        public MainInfoViewModel()
        {
            Handlers = new ObservableCollection<HandlerInfoViewModel>();
            Handlers.CollectionChanged += Handlers_CollectionChanged;

            UploadJobHandlerCommand = new RelayCommand(o =>
            {
                string selectedFile;
                var fileSelected = JobLogicFileBrowser.BrowseForScriptFile(String.Empty, out selectedFile);
                if (fileSelected)
                {
                    var fileContent = File.ReadAllText(selectedFile);
                    ServerModel.Server.AddJobLogic(fileContent);
                }
            });
        }

        private void Handlers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace)
            {
                foreach (HandlerInfoViewModel item in e.NewItems)
                {
                    item.HandlerStartEvent += model => ServerModel.Server.StartJobHandler(model.Id);
                    item.HandlerStopEvent += model => ServerModel.Server.StopJobHandler(model.Id);
                }
            }
        }

        public void Update(ServerInfo info)
        {
            TotalMemory = info.TotalMemory;
            UsedMemory = info.UsedMemory;
            Cpu = info.CpuUsage;
            // Handlers
            Handlers.Clear();
            foreach (var handler in info.Handlers)
            {
                Handlers.Add(new HandlerInfoViewModel
                {
                    Id = handler.Id,
                    Name = String.Format("{0}/{1}/{2}", handler.PluginName, handler.HandlerName, handler.JobName),
                    TotalJobsAvailable = handler.TotalJobsAvailable,
                    JobsAvailable = handler.JobsAvailable,
                    JobsPending = handler.JobsPending,
                    TotalJobsProcessed = handler.TotalJobsProcessed,
                    TotalJobsFailed = handler.TotalJobsFailed,
                    HandlerState = handler.HandlerState
                });
            }
        }
    }
}
