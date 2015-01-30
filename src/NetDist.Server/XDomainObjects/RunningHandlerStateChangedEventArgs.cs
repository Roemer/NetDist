using System;

namespace NetDist.Server.XDomainObjects
{
    [Serializable]
    public class RunningHandlerStateChangedEventArgs : EventArgs
    {
        public RunningHandlerState State { get; set; }

        public RunningHandlerStateChangedEventArgs(RunningHandlerState state)
        {
            State = state;
        }
    }
}
