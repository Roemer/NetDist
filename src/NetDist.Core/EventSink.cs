using NetDist.Core.Utilities;
using System;

namespace NetDist.Core
{
    public class EventSink<TArgs> : RemotableObject<TArgs> where TArgs : EventArgs
    {
        public event EventHandler<TArgs> NotificationFired;

        protected override void InternalCallbackMethod(object sender, TArgs args)
        {
            var handler = NotificationFired;
            if (handler != null)
            {
                handler(sender, args);
            }
        }
    }
}
