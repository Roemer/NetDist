using System.Windows;
using WpfServerAdmin.ViewModels;

namespace WpfServerAdmin.Views
{
    /// <summary>
    /// Interaction logic for ListPopupWindow.xaml
    /// </summary>
    public partial class ListPopupWindow : Window
    {
        private readonly ListPopupWindowViewModel _dialogViewModel;

        public ListPopupWindow(ListPopupWindowViewModel viewModel)
        {
            _dialogViewModel = viewModel;
            InitializeComponent();

            DataContext = _dialogViewModel;
        }
    }
}
