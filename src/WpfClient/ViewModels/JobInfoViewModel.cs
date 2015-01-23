using NetDist.Client;
using NetDist.Core.Utilities;
using System;

namespace WpfClient.ViewModels
{
    public class JobInfoViewModel : ObservableObject, IEquatable<JobInfoViewModel>
    {
        private readonly ClientJob _clientJob;

        public Guid JobId { get { return _clientJob.Job.Id; } }
        public string HandlerName { get { return _clientJob.HandlerName; } }
        public Guid HandlerId { get { return _clientJob.Job.HandlerId; } }
        public string JobInput { get; private set; }
        public DateTime StartDate { get; private set; }

        public TimeSpan Duration
        {
            get { return DateTime.Now - StartDate; }
        }

        private PropertyChangedProxy<ClientJob, string> _statusPropertyChangedProxy;

        public JobInfoViewModel(ClientJob clientJob)
        {
            _clientJob = clientJob;

            StartDate = DateTime.Now;
            JobInput = clientJob.Job.GetInput();

            _statusPropertyChangedProxy = new PropertyChangedProxy<ClientJob, string>(clientJob, m => m.HandlerName, newValue =>
            {
                OnPropertyChanged(() => HandlerName);
            });
        }

        public void RefreshDuration()
        {
            OnPropertyChanged(() => Duration);
        }

        public bool Equals(JobInfoViewModel other)
        {
            return JobId == other.JobId;
        }
    }
}
