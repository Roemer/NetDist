using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Wpf.Shared
{
    /// <summary>
    /// Easy portable configuration in json format
    /// Last updated: 21.01.2015
    /// </summary>
    public class PortableConfiguration
    {
        private class ConfigurationItem
        {
            public string Section { get; set; }
            public string Content { get; set; }
        }

        public interface ISerializer
        {
            T Deserialize<T>(string jsonString);
            string Serialize(object settings);
        }

        private const string DefaultFilename = "settings";
        private readonly object _lockObject = new Object();
        private static readonly Regex SectionRegex = new Regex(@"\s*###CONFSECTION\s*(.*?)\s*###\s*(.*?)\s*###ENDCONFSECTION###\s*", RegexOptions.Compiled | RegexOptions.Singleline);
        private readonly ISerializer _serializer;

        public PortableConfiguration(ISerializer serializer)
        {
            _serializer = serializer;
        }

        public void Save(string section, object settings, string fileName = DefaultFilename)
        {
            var items = LoadInternal(fileName);
            var foundItem = Find(section, items);
            if (foundItem != null)
            {
                // Replace
                foundItem.Content = ConvertToString(settings);
            }
            else
            {
                // Add
                items.Add(CreateItem(section, settings));
            }
            SafeInternal(items, fileName);
        }

        public T Load<T>(string section, string fileName = DefaultFilename, bool createIfNotExists = true) where T : new()
        {
            var items = LoadInternal(fileName);
            var foundItem = Find(section, items);
            if (foundItem != null)
            {
                // Item found
                return _serializer.Deserialize<T>(foundItem.Content);
            }

            var settings = new T();
            if (createIfNotExists)
            {
                // Add new item
                items.Add(CreateItem(section, settings));
                SafeInternal(items, fileName);
            }
            return settings;
        }

        private ConfigurationItem CreateItem(string section, object settings)
        {
            return new ConfigurationItem { Section = section, Content = ConvertToString(settings) };
        }

        private string ConvertToString(object settings)
        {
            return _serializer.Serialize(settings);
        }

        private ConfigurationItem Find(string section, List<ConfigurationItem> items)
        {
            var foundItem = items.Find(x => x.Section.Equals(section, StringComparison.CurrentCultureIgnoreCase));
            return foundItem;
        }

        private string CreateRealFileName(string fileName)
        {
            return fileName + ".json";
        }

        private void SafeInternal(IEnumerable<ConfigurationItem> items, string fileName)
        {
            var sb = new StringBuilder();
            foreach (var item in items)
            {
                sb.AppendFormat(@"###CONFSECTION {0}###", item.Section).AppendLine();
                sb.AppendLine(item.Content);
                sb.AppendLine(@"###ENDCONFSECTION###").AppendLine();
            }

            var tempFile = fileName + ".temp";
            var realFile = CreateRealFileName(fileName);
            var bakFile = fileName + ".bak";
            lock (_lockObject)
            {
                File.WriteAllText(tempFile, sb.ToString());
                if (File.Exists(realFile))
                {
                    if (File.Exists(bakFile))
                    {
                        File.Delete(bakFile);
                    }
                    File.Replace(tempFile, realFile, bakFile);
                }
                else
                {
                    File.Move(tempFile, realFile);
                }
            }
        }

        private List<ConfigurationItem> LoadInternal(string fileName)
        {
            var settingsList = new List<ConfigurationItem>();
            var realFile = CreateRealFileName(fileName);
            lock (_lockObject)
            {
                if (File.Exists(realFile))
                {
                    var fileContent = File.ReadAllText(realFile);
                    var matches = SectionRegex.Matches(fileContent);
                    foreach (Match match in matches)
                    {
                        var matchSection = match.Groups[1].Captures[0].Value;
                        var matchContent = match.Groups[2].Captures[0].Value;
                        settingsList.Add(new ConfigurationItem { Section = matchSection, Content = matchContent });
                    }
                }
            }
            return settingsList;
        }
    }
}
