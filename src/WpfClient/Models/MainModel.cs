using NetDist.Client.WebApi;
using NetDist.Core.Utilities;

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

        /// <summary>
        /// Constructor
        /// </summary>
        public MainModel()
        {
            NetworkAnalyzer = new NetworkTrafficAnalyzer();
            Client = new WebApiClient();
        }
    }
}
