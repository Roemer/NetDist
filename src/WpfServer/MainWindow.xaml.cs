using System.Windows;
using WpfServer.Models;
using WpfServer.ViewModels;

namespace WpfServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var model = new ServerMainModel();
            var viewModel = new ServerMainViewModel(Dispatcher, model);
            model.Initialize();
            DataContext = viewModel;
        }
    }
}
