using System.Linq;
using NetDist.Core.Utilities;
using NetDist.Logging;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;
using Wpf.Shared;
using WpfServer.Models;

namespace WpfServer.ViewModels
{
    public class ServerMainViewModel : ObservableObject
    {
        private readonly Dispatcher _dispatcher;
        private readonly ServerMainModel _model;

        public ObservableCollection<LogLevel> LogLevels { get; set; }

        public LogLevel SelectedMinLogLevel
        {
            get { return GetProperty<LogLevel>(); }
            set { SetProperty(value); }
        }

        public ObservableCollection<LogEntryViewModel> LogEntries { get; set; }

        public ICommand StartCommand { get; private set; }
        public ICommand StopCommand { get; private set; }
        public ICommand SettingsCommand { get; private set; }

        public ServerMainViewModel(Dispatcher dispatcher, ServerMainModel model)
        {
            _dispatcher = dispatcher;
            _model = model;
            LogEntries = new ObservableCollection<LogEntryViewModel>();
            LogLevels = new ObservableCollection<LogLevel>();
            foreach (LogLevel logLevel in Enum.GetValues(typeof(LogLevel)))
            {
                LogLevels.Add(logLevel);
            }
            SelectedMinLogLevel = LogLevel.Info;

            StartCommand = new RelayCommand(o => _model.Server.Start());
            StopCommand = new RelayCommand(o => _model.Server.Stop());
            SettingsCommand = new RelayCommand(o =>
            {
                const string settingsFile = "settings.json";
                Process.Start("notepad.exe", settingsFile);
            });

            _model.LogEvent += LogEventHandler;
        }

        private void LogEventHandler(LogEntry logEntry)
        {
            // Skip irrelvant ones
            if (logEntry.LogLevel < SelectedMinLogLevel)
            {
                return;
            }
            // Check for dispatcher and invoke if needed
            if (!_dispatcher.CheckAccess())
            {
                _dispatcher.Invoke(new Action<LogEntry>(LogEventHandler), logEntry);
                return;
            }
            LogEntries.Insert(0, new LogEntryViewModel(logEntry));
            if (LogEntries.Count > 1000)
            {
                LogEntries.RemoveAt(LogEntries.Count - 1);
            }
        }
    }
}
