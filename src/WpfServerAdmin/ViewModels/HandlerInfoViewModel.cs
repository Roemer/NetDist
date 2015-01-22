using NetDist.Core;
using System;
using System.Windows.Input;
using Wpf.Shared;

namespace WpfServerAdmin.ViewModels
{
    public class HandlerInfoViewModel
    {
        public Guid Id { get { return _handlerInfo.Id; } }
        public string Name { get; private set; }
        public long TotalJobsAvailable { get { return _handlerInfo.TotalJobsAvailable; } }
        public int JobsAvailable { get { return _handlerInfo.JobsAvailable; } }
        public int JobsPending { get { return _handlerInfo.JobsPending; } }
        public long TotalJobsProcessed { get { return _handlerInfo.TotalJobsProcessed; } }
        public long TotalJobsFailed { get { return _handlerInfo.TotalJobsFailed; } }
        public HandlerState HandlerState { get { return _handlerInfo.HandlerState; } }
        public DateTime? LastStartTime { get { return _handlerInfo.LastStartTime; } }
        public DateTime? NextStartTime { get { return _handlerInfo.NextStartTime; } }

        public ICommand StartCommand { get; private set; }
        public ICommand StopCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }

        public event Action<HandlerInfoViewModel> HandlerStartEvent;
        public event Action<HandlerInfoViewModel> HandlerStopEvent;
        public event Action<HandlerInfoViewModel> HandlerDeleteEvent;

        private readonly HandlerInfo _handlerInfo;

        public HandlerInfoViewModel(HandlerInfo handlerInfo)
        {
            _handlerInfo = handlerInfo;
            Name = String.Format("{0}/{1}/{2}", _handlerInfo.PluginName, _handlerInfo.HandlerName, _handlerInfo.JobName);
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
