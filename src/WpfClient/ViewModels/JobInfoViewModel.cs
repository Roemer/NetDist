using NetDist.Core.Utilities;
using NetDist.Jobs.DataContracts;
using System;

namespace WpfClient.ViewModels
{
    public class JobInfoViewModel : ObservableObject
    {
        public Guid JobId { get { return _job.Id; } }
        public Guid HandlerId { get { return _job.HandlerId; } }
        public string JobInput { get; private set; }
        public DateTime StartDate { get; set; }

        public TimeSpan Duration
        {
            get { return DateTime.Now - StartDate; }
        }

        private readonly Job _job;

        public JobInfoViewModel(Job job)
        {
            _job = job;
            StartDate = DateTime.Now;
            JobInput = job.GetInput();
        }

        public void RefreshDuration()
        {
            OnPropertyChanged(() => Duration);
        }
    }
}
