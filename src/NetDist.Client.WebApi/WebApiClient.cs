using NetDist.Core;
using NetDist.Jobs.DataContracts;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace NetDist.Client.WebApi
{
    public class WebApiClient : ClientBase
    {
        private readonly WebApiClientSettings _settings;

        public WebApiClient(WebApiClientSettings settings)
            : base(settings)
        {
            _settings = settings;
        }

        private static void DummyFunctionToMakeSureReferencesGetCopiedProperly()
        {
            System.Net.Http.Formatting.MediaTypeFormatter.GetDefaultValueForType(typeof(object));
        }

        public override Job GetJob(Guid clientId)
        {
            var response = PerformWebClientAction(client => client.GetAsync(String.Concat("api/client/getjob", "/", clientId)).Result);
            if (response == null) { return null; }
            var content = response.ReadAsStringAsync().Result;
            var job = JsonConvert.DeserializeObject<Job>(content);
            return job;
        }

        public override void SendResult(JobResult jobResult)
        {
            var response = PerformWebClientAction(client => client.PostAsJsonAsync("api/client/result", jobResult).Result);
        }

        public override HandlerJobInfo GetHandlerJobInfo(Guid handlerId)
        {
            var response = PerformWebClientAction(client => client.GetAsync(String.Concat("api/client/gethandlerjobinfo", "/", handlerId)).Result);
            if (response == null) { return null; }
            var content = response.ReadAsStringAsync().Result;
            var handlerClientInfo = JsonConvert.DeserializeObject<HandlerJobInfo>(content);
            return handlerClientInfo;
        }

        public override byte[] GetFile(Guid handlerId, string fileName)
        {
            var response = PerformWebClientAction(client => client.GetAsync(String.Concat("api/client/getfile", "/", handlerId, "/", fileName)).Result);
            if (response == null) { return null; }
            var content = response.ReadAsByteArrayAsync().Result;
            return content;
        }

        public override void SendInfo(ClientInfo clientInfo)
        {
            var response = PerformWebClientAction(client => client.PostAsJsonAsync("api/client/info", clientInfo).Result);
        }

        private HttpContent PerformWebClientAction(Func<HttpClient, HttpResponseMessage> action)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_settings.ServerUri);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                try
                {
                    var response = action(client);
                    IsServerReachable = true;
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine(response.IsSuccessStatusCode);
                        return null;
                    }
                    return response.Content;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    IsServerReachable = false;
                    return null;
                }
            }
        }
    }
}
