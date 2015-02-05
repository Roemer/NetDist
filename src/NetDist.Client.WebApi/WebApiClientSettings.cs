using System;

namespace NetDist.Client.WebApi
{
    public class WebApiClientSettings : IClientSettings
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int NumberOfParallelJobs { get; set; }
        public bool AutoStart { get; set; }
        public string ServerUri { get; set; }

        public WebApiClientSettings()
        {
            Id = Guid.NewGuid();
            Name = Environment.MachineName.ToLower();
            AutoStart = false;
            NumberOfParallelJobs = 3;
            ServerUri = @"http://localhost:9000/";
        }
    }
}
