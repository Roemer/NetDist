
namespace NetDist.Jobs
{
    /// <summary>
    /// Class to initialize all the handler settings and the custom settings
    /// </summary>
    public abstract class JobHandlerInitializerBase<T> : IJobHandlerInitializer where T : IHandlerCustomSettings, new()
    {
        public HandlerSettings GetHandlerSettings()
        {
            var handlerSettings = new HandlerSettings();
            FillJobHandlerSettings(handlerSettings);
            return handlerSettings;
        }

        public IHandlerCustomSettings GetCustomHandlerSettings()
        {
            var customHandlerSettings = new T();
            FillCustomSettings(customHandlerSettings);
            return customHandlerSettings;
        }

        public abstract void FillJobHandlerSettings(HandlerSettings handlerSettings);
        public abstract void FillCustomSettings(T customSettings);
    }
}
