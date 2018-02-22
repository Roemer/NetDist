﻿using NetDist.Core;
using NetDist.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Wpf.Shared;
using WpfServerAdmin.Core;
using WpfServerAdmin.Models;
using WpfServerAdmin.Views;

namespace WpfServerAdmin.ViewModels
{
    public class MainInfoViewModel : ObservableObject
    {
        public ServerModel ServerModel { get; set; }

        public double RefreshProgress
        {
            get { return GetProperty<double>(); }
            set { SetProperty(value); }
        }

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

        public int ActiveClientsCount
        {
            get { return GetProperty<int>(); }
            set { SetProperty(value); }
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
            Clients.CollectionChanged += Clients_CollectionChanged;
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
                    var jsInfo = new JobScriptInfo { JobScript = fileContent };
                    var result = ServerModel.Server.AddJobScript(jsInfo);
                    if (result == null)
                    {
                        MessageBox.Show("Bad response from server.", "Error");
                    }
                    else if (result.Status == AddJobScriptStatus.Error)
                    {
                        var msg = String.Format("Reason: {0}", result.ErrorCode);
                        msg += Environment.NewLine;
                        msg += result.ErrorMessage;
                        MessageBox.Show(msg, "Error");
                    }
                    else
                    {
                        var msg = String.Format("{0}", result.HandlerId);
                        MessageBox.Show(msg, "Success");
                    }
                }
            });

            UploadPackageCommand = new RelayCommand(o =>
            {
                var dialogViewModel = new PackageUploadViewModel();
                var dialog = new PackageUploadWindow(dialogViewModel);
                var dialogResult = dialog.ShowDialog();
                if (dialogResult.HasValue && dialogResult.Value)
                {
                    // Create the package object
                    var packageInfo = new PackageInfo();
                    packageInfo.PackageName = dialogViewModel.PackageName;
                    packageInfo.HandlerAssemblies = new List<string>();
                    foreach (var ass in dialogViewModel.HandlerAssemblies)
                    {
                        packageInfo.HandlerAssemblies.Add(Path.GetFileName(ass));
                    }
                    packageInfo.Dependencies = new List<string>();
                    foreach (var dep in dialogViewModel.Dependencies)
                    {
                        packageInfo.Dependencies.Add(Path.GetFileName(dep));
                    }

                    // Prepare the infromation for the zip file
                    var packageName = packageInfo.PackageName;
                    var filesToAdd = new List<string>();
                    filesToAdd.AddRange(dialogViewModel.HandlerAssemblies);
                    filesToAdd.AddRange(dialogViewModel.Dependencies);
                    // Create zip with files
                    var zipBytes = ZipUtility.ZipCompressFilesToBytes(filesToAdd, CompressionLevel.Optimal, packageName);
                    // Uplad package zip
                    ServerModel.Server.AddPackage(packageInfo, zipBytes);
                }
            });
        }

        private void Handlers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace)
            {
                foreach (HandlerInfoViewModel item in e.NewItems)
                {
                    item.HandlerEvent += (o, args) =>
                    {
                        switch (args.EventType)
                        {
                            case HandlerEventType.Start:
                                Task.Run(() => ServerModel.Server.StartJobScript(args.HandlerId));
                                break;
                            case HandlerEventType.Stop:
                                Task.Run(() => ServerModel.Server.StopJobScript(args.HandlerId));
                                break;
                            case HandlerEventType.Pause:
                                Task.Run(() => ServerModel.Server.PauseJobScript(args.HandlerId));
                                break;
                            case HandlerEventType.Disable:
                                Task.Run(() => ServerModel.Server.DisableJobScript(args.HandlerId));
                                break;
                            case HandlerEventType.Enable:
                                Task.Run(() => ServerModel.Server.EnableJobScript(args.HandlerId));
                                break;
                            case HandlerEventType.Delete:
                                Task.Run(() => ServerModel.Server.RemoveJobScript(args.HandlerId));
                                break;
                            case HandlerEventType.ShowLog:
                                var dialogViewModel = new ListPopupWindowViewModel();
                                var t = Task.Run(() =>
                                {
                                    var jobLog = ServerModel.Server.GetJobLog(args.HandlerId);
                                    dialogViewModel.LogInfo = jobLog == null ? new List<LogEntryViewModel>() : jobLog.LogEntries.Select(x => new LogEntryViewModel(x)).ToList();
                                });
                                t.Wait();
                                var dialog = new ListPopupWindow(dialogViewModel);
                                dialog.ShowDialog();
                                break;
                        }
                    };
                }
            }
        }

        private void Clients_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace)
            {
                foreach (ClientInfoViewModel item in e.NewItems)
                {
                    item.ClientEvent += (o, args) =>
                    {
                        switch (args.EventType)
                        {
                            case ClientEventType.Delete:
                                ServerModel.Server.RemoveClient(args.ClientId);
                                break;
                        }
                    };
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
                Handlers.Add(new HandlerInfoViewModel(handler));
            }
            // Clients
            Clients.Clear();
            foreach (var client in info.Clients)
            {
                Clients.Add(new ClientInfoViewModel(client));
            }
            ActiveClientsCount = Clients.Count(i => !i.IsOffline);
        }
    }
}
