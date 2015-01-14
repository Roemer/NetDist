using System;
using System.IO;
using System.Text.RegularExpressions;

namespace NetDist.Server
{
    public static class JobScriptFileParser
    {
        // Prepare regex to parse the file
        private static readonly Regex RegCompilerSettings = new Regex(@"#if COMPILERSETTINGS\s*(.*?)\s*#endif", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex RegHandlerSettings = new Regex(@"#if HANDLERSETTINGS\s*(.*?)\s*#endif", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex RegHandlerCustomSettings = new Regex(@"#if HANDLERCUSTOMSETTINGS\s*(.*?)\s*#endif", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex RegExampleInput = new Regex(@"#if EXAMPLEINPUT\s*(.*?)\s*#endif", RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// Parse the file by path
        /// </summary>
        public static JobScriptFile ParseFile(string filePath)
        {
            // Check for empty file path
            if (String.IsNullOrWhiteSpace(filePath))
            {
                return CreateWithError("No file specified");
            }
            // Check that file exists
            if (!File.Exists(filePath))
            {
                return CreateWithError(String.Format("File '{0}' does not exist", filePath));
            }
            string fileContent;
            try
            {
                // Try to read the file's content
                fileContent = File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                return CreateWithError(String.Format("Failed to read file '{0}': {1}", filePath, ex.Message));
            }

            // Parse the content of the file
            return ParseJob(fileContent);
        }

        /// <summary>
        /// Parse the file by content
        /// </summary>
        public static JobScriptFile ParseJob(string jobContent)
        {
            // Check for empty content
            if (String.IsNullOrWhiteSpace(jobContent))
            {
                return CreateWithError("Content is empty");
            }

            // Search for settings for the handler
            var handlerSettings = GetValueWithRegex(jobContent, RegHandlerSettings);

            // Search for custom settings for the handler
            var handlerCustomSettings = GetValueWithRegex(jobContent, RegHandlerCustomSettings);

            // Search for compiler settings
            var compilerSettings = GetValueWithRegex(jobContent, RegCompilerSettings);

            // Search for example input
            var exampleInput = GetValueWithRegex(jobContent, RegExampleInput);

            // Parse out the job logic
            // TODO: Parse out the unnecessary stuff
            var jobLogic = jobContent;

            // Build the JobFile
            var jobFile = new JobScriptFile
            {
                HandlerSettingsString = handlerSettings,
                HandlerCustomSettingsString = handlerCustomSettings,
                CompilerSettings = compilerSettings,
                ExampleInput = exampleInput,
                JobLogic = jobLogic
            };
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
