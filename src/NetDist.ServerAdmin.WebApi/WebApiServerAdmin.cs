using NetDist.Core;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;

namespace NetDist.ServerAdmin.WebApi
{
    public class WebApiServerAdmin : ServerAdminBase
    {
        private readonly WebApiServerAdminSettings _settings;

        public WebApiServerAdmin(WebApiServerAdminSettings settings)
        {
            _settings = settings;
        }

        private static void DummyFunctionToMakeSureReferencesGetCopiedProperly()
        {
            System.Net.Http.Formatting.MediaTypeFormatter.GetDefaultValueForType(typeof(object));
        }

        public override ServerInfo GetStatistics()
        {
            using (var client = new HttpClient())
            {
                client.Timeout = new TimeSpan(0, 0, 2);
                client.BaseAddress = new Uri(_settings.ServerUri);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // HTTP GET
                var response = client.GetAsync("api/admin/stats").Result;
                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    var serverInfo = JsonConvert.DeserializeObject<ServerInfo>(content);
                    return serverInfo;
                }
            }
            return null;
        }

        public override PackageInfo GetPackages()
        {
            using (var client = new HttpClient())
            {
                client.Timeout = new TimeSpan(0, 0, 2);
                client.BaseAddress = new Uri(_settings.ServerUri);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // HTTP GET
                var response = client.GetAsync("api/admin/getpackages").Result;
                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    var serverInfo = JsonConvert.DeserializeObject<PackageInfo>(content);
                    return serverInfo;
                }
            }
            return null;
        }

        public override void AddPackage(PackageInfo packageInfo, byte[] packageZip)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_settings.ServerUri);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var multiContent = new MultipartContent();
                multiContent.Add(new ObjectContent<PackageInfo>(packageInfo, new JsonMediaTypeFormatter()));
                multiContent.Add(new ByteArrayContent(packageZip));
                // HTTP POST
                var response = client.PostAsync("api/admin/addpackage", multiContent).Result;
                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                }
            }
        }

        public override AddJobHandlerResult AddJobHandler(string jobScript)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_settings.ServerUri);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // HTTP POST
                var response = client.PostAsync("api/admin/addjobhandler", new StringContent(jobScript)).Result;
                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    var addHandlerResult = JsonConvert.DeserializeObject<AddJobHandlerResult>(content);
                    return addHandlerResult;
                }
            }
            return null;
        }

        public override void RemoveJobHandler(Guid handlerId)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_settings.ServerUri);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // HTTP GET
                var response = client.GetAsync(String.Concat("api/admin/removejobhandler", "/", handlerId)).Result;
                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                }
            }
        }

        public override void StartJobHandler(Guid handlerId)
        {
            CallJobHandlerAction(handlerId, "startjobhandler");
        }

        public override void StopJobHandler(Guid handlerId)
        {
            CallJobHandlerAction(handlerId, "stopjobhandler");
        }

        public override void PauseJobHandler(Guid handlerId)
        {
            CallJobHandlerAction(handlerId, "pausejobhandler");
        }

        public override void DisableJobHandler(Guid handlerId)
        {
            CallJobHandlerAction(handlerId, "disablejobhandler");
        }

        public override void EnableJobHandler(Guid handlerId)
        {
            CallJobHandlerAction(handlerId, "enablejobhandler");
        }

        private void CallJobHandlerAction(Guid handlerId, string method)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_settings.ServerUri);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // HTTP GET
                var response = client.GetAsync(String.Concat("api/admin/", method, "/", handlerId)).Result;
                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                }
            }
        }
    }
}
