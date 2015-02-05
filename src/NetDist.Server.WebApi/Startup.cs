using Newtonsoft.Json.Serialization;
using Owin;
using System;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Dispatcher;

namespace NetDist.Server.WebApi
{
    /// <summary>
    /// Initializer class for Web-Api
    /// </summary>
    public class Startup
    {
        private readonly WebApiServer _server;

        public Startup(WebApiServer server)
        {
            _server = server;
        }

        /// <summary>
        /// Do not delete this code! This makes sure that the dll "Microsoft.Owin.Host.HttpListener" is copied
        /// correctly to the output directory.
        /// </summary>
        private static void DummyFunctionToMakeSureReferencesGetCopiedProperly()
        {
            Microsoft.Owin.Host.HttpListener.OwinServerFactory.Initialize(null);
        }

        public void Configuration(IAppBuilder appBuilder)
        {
            var config = new HttpConfiguration();
            // Use attribute based routing
            config.MapHttpAttributeRoutes();
            // Set JSON as default formatter for text/html
            config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));
            config.Formatters.JsonFormatter.MediaTypeMappings.Add(new RequestHeaderMapping("Accept", "text/html", StringComparison.InvariantCultureIgnoreCase, true, "application/json"));
            // Tell the serializer to ignore the serializable attribute to get rid of the "k__BackingField"
            var serializerSettings = config.Formatters.JsonFormatter.SerializerSettings;
            var contractResolver = (DefaultContractResolver)serializerSettings.ContractResolver;
            contractResolver.IgnoreSerializableAttribute = true;
            // Replace the controller activator
            config.Services.Replace(typeof(IHttpControllerActivator), new ServerControllerActivator(_server));
            // Add the configuration
            appBuilder.UseWebApi(config);
        }
    }
}
