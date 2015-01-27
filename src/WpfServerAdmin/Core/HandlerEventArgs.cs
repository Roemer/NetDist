using System;

namespace WpfServerAdmin.Core
{
    public class HandlerEventArgs : EventArgs
    {
        public HandlerEventType EventType { get; private set; }
        public Guid HandlerId { get; private set; }

        public HandlerEventArgs(HandlerEventType eventType, Guid handlerId)
        {
            EventType = eventType;
            HandlerId = handlerId;
        }
    }
}
