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
                    GetStatistics();
                    Thread.Sleep(5000);
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
