using NetDist.Core;
using NetDist.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Input;
using Wpf.Shared;
using WpfServerAdmin.Models;
using WpfServerAdmin.Views;

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
        public ObservableCollection<ClientInfoViewModel> Clients { get; set; }
        public ObservableCollection<PackageInfoViewModel> Packages { get; set; }

        public ICommand ShowSettingsCommand { get; private set; }
        public ICommand UploadJobScriptCommand { get; private set; }
        public ICommand UploadPackageCommand { get; private set; }

        public MainInfoViewModel()
        {
            Handlers = new ObservableCollection<HandlerInfoViewModel>();
            Handlers.CollectionChanged += Handlers_CollectionChanged;
            Clients = new ObservableCollection<ClientInfoViewModel>();
            Packages = new ObservableCollection<PackageInfoViewModel>();

            ShowSettingsCommand = new RelayCommand(o =>
            {
                const string settingsFile = "settings.json";
                Process.Start("notepad.exe", settingsFile);
            });

            UploadJobScriptCommand = new RelayCommand(o =>
            {
                string selectedFile;
                var fileSelected = BrowserDialogs.BrowseForScriptFile(String.Empty, out selectedFile);
                if (fileSelected)
                {
                    var fileContent = File.ReadAllText(selectedFile);
                    var result = ServerModel.Server.AddJobHandler(fileContent);
                    if (result.HasError)
                    {
                        var msg = String.Format("Reason: {0}", result.ErrorReason);
                        msg += Environment.NewLine;
                        msg += result.ErrorMessage;
                        MessageBox.Show(msg, "Error");
                    }
                    else
                    {
                        var msg = String.Format("{0}/{1}/{2}", result.PackageName, result.HandlerName, result.JobName);
                        MessageBox.Show(msg, "Success");
                    }
                }
            });

            UploadPackageCommand = new RelayCommand(o =>
            {
                var dialogViewModel = new PackageUploadViewModel();
                var dialog = new PackageUploadWindow();
                dialog.DataContext = dialogViewModel;
                var dialogResult = dialog.ShowDialog();
                if (dialogResult.HasValue && dialogResult.Value)
                {
                    var filesToAdd = new List<string>();
                    filesToAdd.Add(dialogViewModel.MainLibraryPath);
                    filesToAdd.AddRange(dialogViewModel.Dependencies);
                    // Create zip with files
                    var packageName = Path.GetFileNameWithoutExtension(dialogViewModel.MainLibraryPath);
                    var zipBytes = ZipUtility.ZipCompressFilesToBytes(filesToAdd, CompressionLevel.Optimal, packageName);
                    // Uplad package zip
                    ServerModel.Server.AddPackage(zipBytes);
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
                    item.HandlerDeleteEvent += model => ServerModel.Server.RemoveJobHandler(model.Id);
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
            // Clients
            Clients.Clear();
            foreach (var client in info.Clients)
            {
                Clients.Add(new ClientInfoViewModel
                {
                    Id = client.ClientInfo.Id,
                    Version = client.ClientInfo.Version,
                    Name = client.ClientInfo.Name,
                    Cpu = client.ClientInfo.CpuUsage,
                    TotalMemory = client.ClientInfo.TotalMemory,
                    UsedMemory = client.ClientInfo.UsedMemory,
                    JobsInProgress = client.JobsInProgress,
                    TotalJobsFailed = client.TotalJobsFailed,
                    TotalJobsProcessed = client.TotalJobsProcessed,
                    LastUpdate = client.LastCommunicationDate
                });
            }
        }
    }
}
