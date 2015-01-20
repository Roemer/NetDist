using NetDist.Jobs;
using System;
using NetDist.Core.Utilities;
using Wpf.Shared;

namespace WpfClient.ViewModels
{
    public class JobInfoViewModel : ObservableObject
    {
        public Guid JobId { get { return _job.Id; } }
        public Guid HandlerId { get { return _job.HandlerId; } }
        public string JobInput { get { return _job.JobInputString; } }
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
        }

        public void RefreshDuration()
        {
            OnPropertyChanged(() => Duration);
        }
    }
}
