using NetDist.Logging;
using NetDist.Server.WebApi;
using System;
using System.Diagnostics;
using System.Windows;
using Wpf.Shared;

namespace WpfServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly WebApiServer _server;
        private readonly PortableConfiguration _conf = new PortableConfiguration(new JsonNetSerializer());

        public MainWindow()
        {
            InitializeComponent();
            var settings = _conf.Load<WebApiServerSettings>("ServerSettings");
            _server = new WebApiServer(settings, new ConsoleLogger(LogLevel.Debug).Log, LoggerOnLogEvent);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            _server.Start();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _server.Stop();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            const string settingsFile = "settings.json";
            Process.Start("notepad.exe", settingsFile);
        }

        private void LoggerOnLogEvent(object sender, LogEventArgs logEventArgs)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new EventHandler<LogEventArgs>(LoggerOnLogEvent), sender, logEventArgs);
                return;
            }
            var logEntry = logEventArgs.LogEntry;
            var message = logEntry.Message;
            if (logEntry.Exceptions.Count > 0)
            {
                var exceptionString = logEntry.Exceptions[0].ToString();
                message = String.Format("{0}\r\n    {1}", message, exceptionString);
            }
            if (logEntry.HandlerId.HasValue)
            {
                message = String.Format("Handler: {0} - {1}", logEntry.HandlerId.Value, message);
            }
            else if (logEntry.ClientId.HasValue)
            {
                message = String.Format("Client: {0} - {1}", logEntry.ClientId.Value, message);
            }
            else
            {
                message = String.Format("Server - {0}", message);
            }
            var content = String.Format("[{0:yyyy-MM-dd HH:mm:ss}] [{1}] {2}", logEntry.LogDate, logEntry.LogLevel, message);
            LogList.Items.Insert(0, content);
        }
    }
}
