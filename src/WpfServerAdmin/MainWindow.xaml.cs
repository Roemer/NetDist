using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WpfServerAdmin.Models;
using WpfServerAdmin.ViewModels;

namespace WpfServerAdmin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainInfoViewModel _model;
        private bool _isExiting;
        private DateTime _lastRefresh = DateTime.MinValue;

        private const int RefreshInterval = 5000;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize the model
            var serverModel = new ServerModel();
            // Initialize the view model
            _model = new MainInfoViewModel();
            _model.ServerModel = serverModel;

            // Assign the view model
            DataContext = _model;

            // Start thread for auto refresh
            Task.Factory.StartNew(() =>
            {
                while (!_isExiting)
                {
                    var passedSeconds = (DateTime.Now - _lastRefresh).TotalMilliseconds;
                    if (passedSeconds > RefreshInterval)
                    {
                        GetStatistics();
                        _model.RefreshProgress = 0.0;
                        Thread.Sleep(100);
                        _lastRefresh = DateTime.Now;
                        _model.RefreshProgress = 100;
                    }
                    else
                    {
                        var progress = 100.0 / RefreshInterval * (RefreshInterval - passedSeconds);
                        _model.RefreshProgress = progress > 2 ? progress : 0;
                        Thread.Sleep(100);
                    }
                }
            });
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            _isExiting = true;
        }

        private void GetStatistics()
        {
            try
            {
                var info = _model.ServerModel.Server.GetStatistics();
                Dispatcher.Invoke(() => _model.Update(info));
                _model.IsConnected = true;
            }
            catch (Exception exception)
            {
                // TODO catch only timeout exceptions
                _model.IsConnected = false;
            }
        }

        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
