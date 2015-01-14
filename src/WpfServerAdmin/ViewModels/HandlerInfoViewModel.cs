using NetDist.Core;
using System;
using System.Windows.Input;
using Wpf.Shared;

namespace WpfServerAdmin.ViewModels
{
    public class HandlerInfoViewModel : ObservableObject
    {
        public string Name { get; set; }
        public Guid Id { get; set; }
        public long TotalJobsAvailable { get; set; }
        public int JobsAvailable { get; set; }
        public int JobsPending { get; set; }
        public long TotalJobsProcessed { get; set; }
        public long TotalJobsFailed { get; set; }
        public HandlerState HandlerState { get; set; }

        public ICommand StartCommand { get; private set; }
        public ICommand StopCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }

        public event Action<HandlerInfoViewModel> HandlerStartEvent;
        public event Action<HandlerInfoViewModel> HandlerStopEvent;
        public event Action<HandlerInfoViewModel> HandlerDeleteEvent;

        public HandlerInfoViewModel()
        {
            StartCommand = new RelayCommand(param => { OnHandlerStartEvent(this); }, o => HandlerState == HandlerState.Stopped);
            StopCommand = new RelayCommand(param => { OnHandlerStopEvent(this); }, o => HandlerState == HandlerState.Running);
            DeleteCommand = new RelayCommand(param => { OnHandlerDeleteEvent(this); });
        }

        private void OnHandlerStartEvent(HandlerInfoViewModel obj)
        {
            var handler = HandlerStartEvent;
            if (handler != null) handler(obj);
        }

        private void OnHandlerStopEvent(HandlerInfoViewModel obj)
        {
            var handler = HandlerStopEvent;
            if (handler != null) handler(obj);
        }

        private void OnHandlerDeleteEvent(HandlerInfoViewModel obj)
        {
            var handler = HandlerDeleteEvent;
            if (handler != null) handler(obj);
        }
    }
}
