using System;

namespace WpfServerAdmin.Core
{
    public class ClientEventArgs : EventArgs
    {
        public ClientEventType EventType { get; private set; }
        public Guid ClientId { get; private set; }

        public ClientEventArgs(ClientEventType eventType, Guid clientId)
        {
            EventType = eventType;
            ClientId = clientId;
        }
    }
}
