
namespace NetDist.Jobs
{
    public interface IHandlerInitializer
    {
        HandlerSettings GetHandlerSettings();
        IHandlerCustomSettings GetCustomHandlerSettings();
    }
}
