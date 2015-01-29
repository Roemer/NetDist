
namespace NetDist.Jobs
{
    /// <summary>
    /// Class to initialize all the handler settings and the custom settings
    /// </summary>
    public abstract class HandlerInitializerBase<T> : IHandlerInitializer where T : IHandlerCustomSettings, new()
    {
        public HandlerSettings GetHandlerSettings()
        {
            var handlerSettings = new HandlerSettings();
            FillHandlerSettings(handlerSettings);
            return handlerSettings;
        }

        public IHandlerCustomSettings GetCustomHandlerSettings()
        {
            var customHandlerSettings = new T();
            FillCustomSettings(customHandlerSettings);
            return customHandlerSettings;
        }

        public abstract void FillHandlerSettings(HandlerSettings handlerSettings);
        public abstract void FillCustomSettings(T customSettings);
    }
}
