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
        private IHandler _handler;

        public LoadedHandler()
        {
            var pathToDll = @"E:\Development\MyGitHub\NetDist\src\SimpleCalculator\bin\Debug\SimpleCalculator.dll";
            var handlerAssembly = Assembly.LoadFile(pathToDll);
            var handlerToLoad = "Calculator";

            Type typeToLoad = null;
            foreach (var type in handlerAssembly.GetTypes())
            {
                if (typeof(IHandler).IsAssignableFrom(type))
                {
                    var att = type.GetCustomAttribute<HandlerNameAttribute>(true);
                    if (att != null)
                    {
                        if (att.HandlerName == handlerToLoad)
                        {
                            typeToLoad = type;
                            break;
                        }
                    }
                }
            }

            var handlerInstance = (IHandler)Activator.CreateInstance(typeToLoad);
            _handler = handlerInstance;
            Console.WriteLine(_handler.Id);
        }

        public override object InitializeLifetimeService()
        {
            // Infinite lifetime
            return null;
        }
    }
}
