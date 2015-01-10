using NetDist.Handlers;
using System;
using System.Reflection;

namespace NetDist.Server
{
    /// <summary>
    /// Represents a handler which is loaded and active
    /// This object runs in it's own domain
    /// </summary>
    public class LoadedHandler : MarshalByRefObject
    {
        /// <summary>
        /// ID of the loaded handler
        /// </summary>
        public Guid HandlerId { get; private set; }

        private IHandler _handler;

        public LoadedHandler()
        {
            var pluginName = "SimpleCalculator";
            var handlerName = "Calculator";

            var pluginPath = String.Format(@"E:\Development\MyGitHub\NetDist\src\{0}\bin\Debug\{0}.dll", pluginName);
            var handlerAssembly = Assembly.LoadFile(pluginPath);

            Type typeToLoad = null;
            foreach (var type in handlerAssembly.GetTypes())
            {
                if (typeof(IHandler).IsAssignableFrom(type))
                {
                    var att = type.GetCustomAttribute<HandlerNameAttribute>(true);
                    if (att != null)
                    {
                        if (att.HandlerName == handlerName)
                        {
                            typeToLoad = type;
                            break;
                        }
                    }
                }
            }

            var handlerInstance = (IHandler)Activator.CreateInstance(typeToLoad);
            _handler = handlerInstance;
            HandlerId = _handler.Id;
        }

        public override object InitializeLifetimeService()
        {
            // Infinite lifetime
            return null;
        }
    }
}
