using NetDist.Core;
using System;
using System.Windows.Input;
using Wpf.Shared;
using WpfServerAdmin.Core;

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
        public string HandlerMessage { get { return _handlerInfo.HandlerMessage; } }
        public DateTime? LastStartTime { get { return _handlerInfo.LastStartTime; } }
        public DateTime? NextStartTime { get { return _handlerInfo.NextStartTime; } }

        #region Calculated properties
        public bool IsFailed { get { return HandlerState == HandlerState.Failed; } }
        public bool IsStopped { get { return HandlerState == HandlerState.Stopped; } }
        public bool IsDisabled { get { return HandlerState == HandlerState.Disabled || HandlerState == HandlerState.Paused; } }
        public bool IsReady { get { return HandlerState == HandlerState.Finished || HandlerState == HandlerState.Idle; } }
        public bool IsRunning { get { return HandlerState == HandlerState.Running; } }
        #endregion

        public ICommand StartCommand { get; private set; }
        public ICommand StopCommand { get; private set; }
        public ICommand PauseCommand { get; private set; }
        public ICommand DisableCommand { get; private set; }
        public ICommand EnableCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }

        public event EventHandler<HandlerEventArgs> HandlerEvent;

        private readonly HandlerInfo _handlerInfo;

        public HandlerInfoViewModel(HandlerInfo handlerInfo)
        {
            _handlerInfo = handlerInfo;
            Name = String.Format("{0}/{1}/{2}", _handlerInfo.PackageName, _handlerInfo.HandlerName, _handlerInfo.JobName);
            StartCommand = new RelayCommand(param => OnHandlerEvent(new HandlerEventArgs(HandlerEventType.Start, _handlerInfo.Id))
                , o => HandlerState == HandlerState.Stopped || HandlerState == HandlerState.Paused || HandlerState == HandlerState.Idle || HandlerState == HandlerState.Finished);
            StopCommand = new RelayCommand(param => OnHandlerEvent(new HandlerEventArgs(HandlerEventType.Stop, _handlerInfo.Id))
                , o => HandlerState == HandlerState.Running);
            PauseCommand = new RelayCommand(param => OnHandlerEvent(new HandlerEventArgs(HandlerEventType.Pause, _handlerInfo.Id))
                , o => HandlerState == HandlerState.Running);
            DisableCommand = new RelayCommand(param => OnHandlerEvent(new HandlerEventArgs(HandlerEventType.Disable, _handlerInfo.Id))
                , o => HandlerState != HandlerState.Disabled);
            EnableCommand = new RelayCommand(param => OnHandlerEvent(new HandlerEventArgs(HandlerEventType.Enable, _handlerInfo.Id))
                , o => HandlerState == HandlerState.Disabled);
            DeleteCommand = new RelayCommand(param => OnHandlerEvent(new HandlerEventArgs(HandlerEventType.Delete, _handlerInfo.Id)));
        }

        protected virtual void OnHandlerEvent(HandlerEventArgs e)
        {
            var handler = HandlerEvent;
            if (handler != null) handler(this, e);
        }
    }
}
