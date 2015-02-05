using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace NetDist.Core.Utilities
{
    /// <summary>
    /// Base class for a self updating executable
    /// Last updated: 05.02.2015
    /// </summary>
    public abstract class AutoUpdateBase
    {
        private readonly List<string> _excludeFileNames;

        protected AutoUpdateBase(params string[] excludeFileNames)
        {
            _excludeFileNames = new List<string>();
            if (excludeFileNames != null && excludeFileNames.Length > 0)
            {
                _excludeFileNames.AddRange(excludeFileNames);
            }
        }

        public bool CheckAndUpdate()
        {
            // Wait until other instances of this process are closed
            var currentProcess = Process.GetCurrentProcess();
            var processes = Process.GetProcessesByName(currentProcess.ProcessName);
            if (processes.Length > 1)
            {
                // Small delay so that the other process have a chance to exit on their own
                Thread.Sleep(2000);
                // Get the processes again
                processes = Process.GetProcessesByName(currentProcess.ProcessName);
                if (processes.Length > 1)
                {
                    // Loop through processes with the same name 
                    foreach (var process in processes)
                    {
                        // Ignore the current process
                        if (process.Id == currentProcess.Id) continue;
                        // Refresh the information about the process
                        process.Refresh();
                        // Skip finished ones
                        if (process.HasExited) { continue; }
                        // Close or kill other instances
                        process.CloseMainWindow();
                        process.Close();
                        process.WaitForExit(1000);
                        // Check if it closed correctly
                        if (process.HasExited) { continue; }
                        // Kill it otherwise
                        process.Kill();
                        process.WaitForExit(1000);
                    }
                }
            }

            // Update routine
            const string updateDirName = "AutoUpdateTemp";
            string exeName = AppDomain.CurrentDomain.FriendlyName;
            string currDir = Directory.GetCurrentDirectory();
            if (currDir.EndsWith(updateDirName))
            {
                // We're running in temp dir
                var mainDir = Directory.GetParent(currDir).FullName;
                var tempDir = currDir;
                // Copy to main
                foreach (var file in new DirectoryInfo(tempDir).GetFiles())
                {
                    if (!_excludeFileNames.Contains(file.Name))
                    {
                        file.CopyTo(Path.Combine(mainDir, file.Name), true);
                    }
                }
                // Run from main
                var startInfo = new ProcessStartInfo(Path.Combine(mainDir, exeName));
                startInfo.WorkingDirectory = mainDir;
                Process.Start(startInfo);
                Terminate();
                return true;
            }
            else
            {
                // We're running in main directory
                var tempDir = Path.Combine(currDir, updateDirName);
                // Delete possibly existing temp
                if (Directory.Exists(tempDir)) { Directory.Delete(tempDir, true); }

                // Get versions
                var currentVersion = GetCurrentVersion();
                var newestVersion = GetNewestVersion();

                // Compare versions
                if (newestVersion != "0" && currentVersion != newestVersion)
                {
                    // Version changed, confirm the update
                    if (UpdateConfirmation(currentVersion, newestVersion))
                    {
                        // Prepare new version
                        PrepareNewVersion(tempDir);
                        // Run from temp
                        var startInfo = new ProcessStartInfo(Path.Combine(tempDir, exeName));
                        startInfo.WorkingDirectory = tempDir;
                        Process.Start(startInfo);
                        Terminate();
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Get the current version
        /// </summary>
        public abstract string GetCurrentVersion();

        /// <summary>
        /// Get the newest version
        /// </summary>
        protected abstract string GetNewestVersion();

        /// <summary>
        /// Terminate the application
        /// </summary>
        protected abstract void Terminate();

        /// <summary>
        /// Prepare the new version of the application in the given directory
        /// </summary>
        /// <param name="preparationDirectoryPath">The directory where the new version must be placed</param>
        protected abstract void PrepareNewVersion(string preparationDirectoryPath);

        /// <summary>
        /// Optional method to confirm the update
        /// </summary>
        /// <param name="currentVersion">The current version-string</param>
        /// <param name="newestVersion">The newest version-string</param>
        /// <returns>true if it should update, false otherwise</returns>
        protected virtual bool UpdateConfirmation(string currentVersion, string newestVersion)
        {
            return true;
        }
    }
}