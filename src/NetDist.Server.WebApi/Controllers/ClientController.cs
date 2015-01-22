using NetDist.Core;
using NetDist.Jobs.DataContracts;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace NetDist.Server.WebApi.Controllers
{
    [RoutePrefix("api/client")]
    public class ClientController : ControllerBase
    {
        public ClientController(ServerBase server)
            : base(server) { }

        [HttpGet]
        [Route("getjob/{id}")]
        public IHttpActionResult GetJob(Guid id)
        {
            var job = Server.GetJob(id);
            return Ok(job);
        }

        [HttpGet]
        [Route("gethandlerjobinfo/{id}")]
        public IHttpActionResult GetHandlerClientInfo(Guid id)
        {
            var handlerClientInfo = Server.GetHandlerJobInfo(id);
            return Ok(handlerClientInfo);
        }

        [HttpGet]
        [Route("getfile/{id}/{file}")]
        public HttpResponseMessage GetFile(Guid id, string file)
        {
            var fileContent = Server.GetFile(id, file);
            var result = new HttpResponseMessage(HttpStatusCode.OK);
            var stream = new MemoryStream(fileContent);
            result.Content = new StreamContent(stream);
            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
            result.Content.Headers.ContentDisposition.FileName = Path.GetFileName(file);
            result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            result.Content.Headers.ContentLength = stream.Length;
            return result;
        }

        [HttpPost]
        [Route("result")]
        public IHttpActionResult Result()
        {
            var result = Request.Content.ReadAsAsync<JobResult>().Result;
            Server.ReceiveResult(result);
            return Ok();
        }

        [HttpPost]
        [Route("info")]
        public IHttpActionResult Info()
        {
            var info = Request.Content.ReadAsAsync<ClientInfo>().Result;
            Server.ReceivedClientInfo(info);
            return Ok();
        }
    }
}
