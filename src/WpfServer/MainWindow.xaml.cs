using NetDist.Logging;
using NetDist.Server.WebApi;
using System;
using System.Windows;

namespace WpfServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly WebApiServer _server;

        public MainWindow()
        {
            InitializeComponent();
            _server = new WebApiServer();

            _server.Logger.LogEvent += new ConsoleLogger(LogLevel.Debug).Log;
            _server.Logger.LogEvent += LoggerOnLogEvent;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            _server.Start();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _server.Stop();
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
