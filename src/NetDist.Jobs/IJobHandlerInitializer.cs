
namespace NetDist.Jobs
{
    public interface IJobHandlerInitializer
    {
        HandlerSettings GetHandlerSettings();
        object GetCustomHandlerSettings();
    }
}
