using System;
using System.Windows.Forms;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace Wpf.Shared
{
    public static class BrowserDialogs
    {
        public static bool BrowseForScriptFile(string initialDir, out string fileName)
        {
            var tempFileName = default(String);
            var fileSelected = BrowseForScriptFile(initialDir, file => tempFileName = file[0]);
            fileName = tempFileName;
            return fileSelected;
        }

        public static bool BrowseForScriptFile(string initialDir = null, Action<string[]> successAction = null)
        {
            return FileBrowser(".cs", "C# Script (.cs)|*.cs", initialDir, false, successAction);
        }

        public static bool BrowseForAnyFile(string initialDir = null, bool multiselect = false, Action<string[]> successAction = null)
        {
            return FileBrowser("", "All files (*.*)|*.*", initialDir, multiselect, successAction);
        }

        public static bool BrowseForDll(string initialDir = null, bool multiselect = false, Action<string[]> successAction = null)
        {
            return FileBrowser(".dll", "Assembly (.dll)|*.dll", initialDir, multiselect, successAction);
        }

        public static bool FileBrowser(string defaultExt, string filter, string initialDir = null, bool multiselect = false, Action<string[]> successAction = null)
        {
            // Create the dialog
            var dlg = new OpenFileDialog
            {
                // Setup the dialog
                InitialDirectory = initialDir,
                DefaultExt = defaultExt,
                Filter = filter,
                Multiselect = multiselect
            };

            // Show the dialog
            var result = dlg.ShowDialog();
            if (result == true && dlg.FileNames != null && dlg.FileNames.Length > 0)
            {
                if (successAction != null) successAction(dlg.FileNames);
                return true;
            }
            return false;
        }

        public static bool BrowseForPackageFolder(out string folderPath)
        {
            folderPath = default(String);
            using (var dialog = new FolderBrowserDialog())
            {
                var result = dialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    folderPath = dialog.SelectedPath;
                    return true;
                }
                return false;
            }
        }
    }
}
