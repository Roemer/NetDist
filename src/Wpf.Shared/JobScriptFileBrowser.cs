using Microsoft.Win32;
using System;

namespace Wpf.Shared
{
    public static class JobScriptFileBrowser
    {
        public static bool BrowseForScriptFile(string initialDir, out string fileName)
        {
            var tempFileName = default(String);
            var fileSelected = BrowseForScriptFile(initialDir, file => tempFileName = file);
            fileName = tempFileName;
            return fileSelected;
        }

        public static bool BrowseForScriptFile(string initialDir, Action<string> successAction)
        {
            // Create the dialog
            var dlg = new OpenFileDialog();
            // Setup the dialog
            dlg.InitialDirectory = initialDir;
            dlg.DefaultExt = ".cs";
            dlg.Filter = "C# Script (.cs)|*.cs";
            dlg.Multiselect = false;

            // Show the dialog
            var result = dlg.ShowDialog();

            if (result == true)
            {
                successAction(dlg.FileName);
                return true;
            }
            return false;
        }
    }
}
