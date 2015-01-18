
namespace NetDist.Jobs
{
    public interface IJobHandlerInitializer
    {
        HandlerSettings GetHandlerSettings();
        IHandlerCustomSettings GetCustomHandlerSettings();
    }
}
