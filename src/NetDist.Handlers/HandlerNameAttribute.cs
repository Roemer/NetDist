using System;

namespace NetDist.Handlers
{
    /// <summary>
    /// Attribute used to declare handler names
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class HandlerNameAttribute : Attribute
    {
        public readonly string HandlerName;

        public HandlerNameAttribute(string handlerName)
        {
            HandlerName = handlerName;
        }
    }
}
