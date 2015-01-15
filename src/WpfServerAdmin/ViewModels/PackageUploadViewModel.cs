using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Wpf.Shared;

namespace WpfServerAdmin.ViewModels
{
    public class PackageUploadViewModel : ObservableObject
    {
        public string MainLibraryPath
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public ObservableCollection<string> Dependencies { get; set; }

        public ICommand BrowseMainAssembly { get; private set; }
        public ICommand AddDependency { get; private set; }
        public ICommand RemoveDependency { get; private set; }

        public PackageUploadViewModel()
        {
            Dependencies = new ObservableCollection<string>();

            BrowseMainAssembly = new RelayCommand(o =>
            {
                BrowserDialogs.BrowseForDll(null, false, s => MainLibraryPath = s[0]);
            });

            AddDependency = new RelayCommand(o =>
            {
                BrowserDialogs.BrowseForAnyFile(null, true, s => s.ToList().ForEach(x => Dependencies.Add(x)));
            });

            RemoveDependency = new TypedRelayCommand<int>(o => Dependencies.RemoveAt(o), o => o >= 0);
        }
    }
}
