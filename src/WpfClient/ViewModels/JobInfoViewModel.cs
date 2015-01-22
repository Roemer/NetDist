using NetDist.Client;
using NetDist.Core.Utilities;
using NetDist.Jobs.DataContracts;
using System;

namespace WpfClient.ViewModels
{
    public class JobInfoViewModel : ObservableObject
    {
        public Guid JobId { get { return _job.Id; } }
        public string HandlerName { get; private set; }
        public Guid HandlerId { get { return _job.HandlerId; } }
        public string JobInput { get; private set; }
        public DateTime StartDate { get; private set; }

        public TimeSpan Duration
        {
            get { return DateTime.Now - StartDate; }
        }

        private readonly Job _job;
        private PropertyChangedProxy<ClientJob, string> _statusPropertyChangedProxy;

        public JobInfoViewModel(ClientJob clientJob)
        {
            _job = clientJob.Job;
            StartDate = DateTime.Now;
            JobInput = _job.GetInput();

            _statusPropertyChangedProxy = new PropertyChangedProxy<ClientJob, string>(clientJob, m => m.HandlerName, newValue =>
            {
                HandlerName = newValue;
                OnPropertyChanged(() => HandlerName);
            });
        }

        public void RefreshDuration()
        {
            OnPropertyChanged(() => Duration);
        }
    }
}
