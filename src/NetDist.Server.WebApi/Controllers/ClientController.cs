using NetDist.Core;
using NetDist.Jobs;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace NetDist.Server.WebApi.Controllers
{
    [RoutePrefix("api/client")]
    public class ClientController : ApiController
    {
        [HttpGet]
        [Route("getjob/{id}")]
        public IHttpActionResult GetJob(Guid id)
        {
            var job = WebApiServer.Instance.GetJob(id);
            return Ok(job);
        }

        [HttpGet]
        [Route("gethandlerjobinfo/{id}")]
        public IHttpActionResult GetHandlerClientInfo(Guid id)
        {
            var handlerClientInfo = WebApiServer.Instance.GetHandlerJobInfo(id);
            return Ok(handlerClientInfo);
        }

        [HttpGet]
        [Route("getfile/{id}/{file}")]
        public HttpResponseMessage GetFile(Guid id, string file)
        {
            var fileContent = WebApiServer.Instance.GetFile(id, file);
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
            WebApiServer.Instance.ReceiveResult(result);
            return Ok();
        }

        [HttpPost]
        [Route("info")]
        public IHttpActionResult Info()
        {
            var info = Request.Content.ReadAsAsync<ClientInfo>().Result;
            WebApiServer.Instance.ReceivedClientInfo(info);
            return Ok();
        }
    }
}
