using NetDist.Core.Utilities;
using NetDist.Jobs.DataContracts;

namespace NetDist.Client
{
    public class ClientJob : ObservableObject
    {
        public Job Job { get; private set; }
        public string HandlerName
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public ClientJob(Job job)
        {
            Job = job;
            HandlerName = job.HandlerId.ToString();
        }
    }
}
