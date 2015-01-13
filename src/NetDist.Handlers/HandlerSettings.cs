
namespace NetDist.Handlers
{
    public class HandlerSettings
    {
        public string PluginName { get; set; }
        public string HandlerName { get; set; }
        public string JobName { get; set; }

        /// <summary>
        /// Timeout (in seconds) when a job was assigned to a client until it is reassigned to another client
        /// Use 0 to deactivate the timeout
        /// </summary>
        public int JobTimeout { get; set; }
    }
}
