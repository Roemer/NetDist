using System.Windows;
using WpfServerAdmin.ViewModels;

namespace WpfServerAdmin.Views
{
    /// <summary>
    /// Interaction logic for PackageUploadWindow.xaml
    /// </summary>
    public partial class PackageUploadWindow : Window
    {
        private readonly PackageUploadViewModel _dialogViewModel;

        public PackageUploadWindow(PackageUploadViewModel dialogViewModel)
        {
            _dialogViewModel = dialogViewModel;
            InitializeComponent();

            DataContext = _dialogViewModel;

            HandlerAssembliesList.AllowDrop = true;
            HandlerAssembliesList.DragEnter += HandlerAssembliesList_DragEnter;
            HandlerAssembliesList.Drop += HandlerAssembliesList_Drop;
        }

        private void HandlerAssembliesList_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.All;
            else
                e.Effects = DragDropEffects.None;
        }

        private void HandlerAssembliesList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    _dialogViewModel.AddFile(file, _dialogViewModel.HandlerAssemblies);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
