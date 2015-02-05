using NetDist.Client.WebApi;
using NetDist.Core.Utilities;
using System;
using Wpf.Shared;
using WpfClient.Core;

namespace WpfClient.Models
{
    public class MainModel : ObservableObject
    {
        /// <summary>
        /// Version of the client
        /// </summary>
        public string Version
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }
        /// <summary>
        /// Analyzer for the network traffic
        /// </summary>
        public NetworkTrafficAnalyzer NetworkAnalyzer { get; private set; }
        /// <summary>
        /// Object with basic functionalty and to allow the communication with the server
        /// </summary>
        public WebApiClient Client { get; private set; }

        private readonly PortableConfiguration _conf = new PortableConfiguration(new JsonNetSerializer());
        private WpfClientSettings _settings;
        private AutoUpdateFromFtp _autoUpdater;

        /// <summary>
        /// Constructor
        /// </summary>
        public MainModel()
        {
            NetworkAnalyzer = new NetworkTrafficAnalyzer();
            _settings = _conf.Load<WpfClientSettings>("ClientSettings");
            Client = new WebApiClient(_settings);
        }

        public void InitializeAutoUpdater(Action terminateAction)
        {
            _autoUpdater = new AutoUpdateFromFtp(_settings.FtpUrl, _settings.FtpUser, _settings.FtpPassword, terminateAction, "settings.json");
            Version = _autoUpdater.GetCurrentVersion();
            Client.UpdateVersion(Version);
        }

        public void CheckAndUpdate()
        {
            if (!_settings.IsDebug)
            {
                _autoUpdater.CheckAndUpdate();
            }
        }
    }
}
