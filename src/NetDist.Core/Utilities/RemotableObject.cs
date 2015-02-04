using System;

namespace NetDist.Core.Utilities
{
    public abstract class RemotableObject<TArgs> : MarshalByRefObject where TArgs : EventArgs
    {
        public void CallbackMethod(object sender, TArgs args)
        {
            InternalCallbackMethod(sender, args);
        }

        protected abstract void InternalCallbackMethod(object sender, TArgs args);

        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
