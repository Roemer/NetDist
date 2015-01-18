using NetDist.Client.WebApi;
using NetDist.Core.Utilities;
using NetDist.Jobs;
using System.Collections.ObjectModel;

namespace WpfClient.Models
{
    public class MainModel
    {
        public string Version { get; set; }
        public int NumberOfParallelTasks { get; set; }
        public NetworkTrafficAnalyzer NetworkAnalyzer { get; private set; }
        public WebApiClient Client { get; private set; }

        public ObservableCollection<Job> Jobs { get; set; }

        public MainModel()
        {
            NetworkAnalyzer = new NetworkTrafficAnalyzer();
            Client = new WebApiClient();
            Jobs = new ObservableCollection<Job>();
            NumberOfParallelTasks = 3;
        }
    }
}
