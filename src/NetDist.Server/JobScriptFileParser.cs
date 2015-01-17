using System;
using System.Text.RegularExpressions;

namespace NetDist.Server
{
    public static class JobScriptFileParser
    {
        // Prepare regex to parse the file
        private static readonly Regex RegCompilerLibraries = new Regex(@"#if NETDISTCOMPILERLIBRARIES\s*(.*?)\s*#endif", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex RegDependencies = new Regex(@"#if NETDISTDEPENDENCIES\s*(.*?)\s*#endif", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex RegPackageName = new Regex(@"#if NETDISTPACKAGE\s*(.*?)\s*#endif", RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// Parse by string
        /// </summary>
        public static JobScriptFile Parse(string jobFileContent)
        {
            // Check for empty content
            if (String.IsNullOrWhiteSpace(jobFileContent))
            {
                return CreateWithError("Content is empty");
            }

            // Build the file object
            var jobFile = new JobScriptFile();

            // Search for compiler libraries
            var compilerLibrariesString = GetValueWithRegex(jobFileContent, RegCompilerLibraries);
            foreach (var compilerLibrary in compilerLibrariesString.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                jobFile.CompilerLibraries.Add(compilerLibrary);
            }

            // Search for dependencies
            var dependenciesString = GetValueWithRegex(jobFileContent, RegDependencies);
            foreach (var dependency in dependenciesString.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                jobFile.Dependencies.Add(dependency);
            }

            // Parse out the package
            var packageString = GetValueWithRegex(jobFileContent, RegPackageName);
            jobFile.PackageName = packageString.Trim();

            // Parse out the job logic
            // TODO: Parse out the unnecessary stuff
            var jobscript = jobFileContent;
            jobFile.JobScript = jobscript;

            return jobFile;
        }

        /// <summary>
        /// Return a job file with the given error
        /// </summary>
        private static JobScriptFile CreateWithError(string errorMessage)
        {
            var jobFile = new JobScriptFile();
            jobFile.SetError(errorMessage);
            return jobFile;
        }

        /// <summary>
        /// Helper method which reads out a value with the given regex and returns it or the default value if none found
        /// </summary>
        private static string GetValueWithRegex(string content, Regex regex, string defaultValue = "")
        {
            var value = defaultValue;
            var match = regex.Match(content);
            if (match.Success)
            {
                // Value found
                value = match.Groups[1].Value;
            }
            return value;
        }
    }
}
