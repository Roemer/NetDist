using System;
using System.IO;
using NetDist.Core.Utilities;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Wpf.Shared;

namespace WpfServerAdmin.ViewModels
{
    public class PackageUploadViewModel : ObservableObject
    {
        public string PackageName
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public ObservableCollection<string> HandlerAssemblies { get; set; }
        public ObservableCollection<string> Dependencies { get; set; }

        public ICommand AddHandlerAssemblies { get; private set; }
        public ICommand RemoveHandlerAssemblies { get; private set; }
        public ICommand AddDependency { get; private set; }
        public ICommand RemoveDependency { get; private set; }

        public PackageUploadViewModel()
        {
            HandlerAssemblies = new ObservableCollection<string>();
            Dependencies = new ObservableCollection<string>();

            AddHandlerAssemblies = new RelayCommand(o => BrowserDialogs.BrowseForDll(null, true, s =>
            {
                if (String.IsNullOrWhiteSpace(PackageName) && s.Length > 0)
                {
                    PackageName = Path.GetFileNameWithoutExtension(s[0]);
                }
                s.ToList().ForEach(x => HandlerAssemblies.Add(x));
            }));

            AddDependency = new RelayCommand(o =>
            {
                BrowserDialogs.BrowseForAnyFile(null, true, s => s.ToList().ForEach(x => Dependencies.Add(x)));
            });

            RemoveHandlerAssemblies = new TypedRelayCommand<int>(o => HandlerAssemblies.RemoveAt(o), o => o >= 0);
            RemoveDependency = new TypedRelayCommand<int>(o => Dependencies.RemoveAt(o), o => o >= 0);
        }
    }
}
