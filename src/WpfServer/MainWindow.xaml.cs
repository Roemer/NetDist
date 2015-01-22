using NetDist.Logging;
using NetDist.Server.WebApi;
using System;
using System.Diagnostics;
using System.IO;
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
            LogList.Items.Insert(0, logEventArgs.Message);
        }
    }
}
