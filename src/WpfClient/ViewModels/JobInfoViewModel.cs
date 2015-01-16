using System;
using Wpf.Shared;

namespace WpfClient.ViewModels
{
    public class JobInfoViewModel : ObservableObject
    {
        public Guid JobId { get; set; }
        public Guid HandlerId { get; set; }
        public string JobInput { get; set; }
        public DateTime StartDate { get; set; }

        public TimeSpan Duration
        {
            get { return DateTime.Now - StartDate; }
        }

        public void RefreshDuration()
        {
            OnPropertyChanged(() => Duration);
        }
    }
}
