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

        public override AddJobScriptResult AddJobScript(JobScriptInfo jobScriptInfo)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_settings.ServerUri);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // HTTP POST
                var response = client.PostAsJsonAsync("api/admin/addjobscript", jobScriptInfo).Result;
                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    var addHandlerResult = JsonConvert.DeserializeObject<AddJobScriptResult>(content);
                    return addHandlerResult;
                }
            }
            return null;
        }

        public override void RemoveJobScript(Guid handlerId)
        {
            CallJobScriptAction(handlerId, "removejobscript");
        }

        public override void StartJobScript(Guid handlerId)
        {
            CallJobScriptAction(handlerId, "startjobscript");
        }

        public override void StopJobScript(Guid handlerId)
        {
            CallJobScriptAction(handlerId, "stopjobscript");
        }

        public override void PauseJobScript(Guid handlerId)
        {
            CallJobScriptAction(handlerId, "pausejobscript");
        }

        public override void DisableJobScript(Guid handlerId)
        {
            CallJobScriptAction(handlerId, "disablejobscript");
        }

        public override void EnableJobScript(Guid handlerId)
        {
            CallJobScriptAction(handlerId, "enablejobscript");
        }

        private void CallJobScriptAction(Guid handlerId, string method)
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
            GetStatistics();
        }

        public override void RemoveClient(Guid clientId)
        {
            CallClientAction(clientId, "removeclient");
        }

        private void CallClientAction(Guid clientId, string method)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_settings.ServerUri);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // HTTP GET
                var response = client.GetAsync(String.Concat("api/admin/", method, "/", clientId)).Result;
                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                }
            }
            GetStatistics();
        }
    }
}
