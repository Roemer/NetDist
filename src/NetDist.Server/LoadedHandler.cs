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
        /// <summary>
        /// Object which holds the handler settings
        /// </summary>
        public HandlerSettings HandlerSettings { get; private set; }

        /// <summary>
        /// Full name of the handler: PluginName/HandlerName/JobName
        /// </summary>
        public string FullName
        {
            get
            {
                return String.Format("{0}/{1}/{2}", HandlerSettings.PluginName, HandlerSettings.HandlerName, HandlerSettings.JobName);
            }
        }

        /// <summary>
        /// Instance of the effective handler
        /// </summary>
        private IHandler _handler;

        /// <summary>
        /// Constructor
        /// </summary>
        public LoadedHandler(string handlerSettingsString)
        {
            HandlerSettings = JobObjectSerializer.Deserialize<HandlerSettings>(handlerSettingsString);
        }

        /// <summary>
        /// Lifetime override of the proxy object
        /// </summary>
        public override object InitializeLifetimeService()
        {
            // Infinite lifetime
            return null;
        }

        /// <summary>
        /// Tries to initialize the appropriate handler
        /// </summary>
        public bool InitializeHandler()
        {
            var pluginName = HandlerSettings.PluginName;
            var handlerName = HandlerSettings.HandlerName;

            var pluginPath = String.Format(@"E:\Plugins\{0}.dll", pluginName);
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
            if (typeToLoad != null)
            {
                var handlerInstance = (IHandler)Activator.CreateInstance(typeToLoad);
                _handler = handlerInstance;
                HandlerId = _handler.Id;
                return true;
            }
            return false;
        }
    }
}
