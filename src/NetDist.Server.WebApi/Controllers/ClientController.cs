using NetDist.Jobs;
using System;
using System.Web.Http;

namespace NetDist.Server.WebApi.Controllers
{
    [RoutePrefix("api/client")]
    public class ClientController : ApiController
    {
        [HttpGet]
        [Route("getjob/{id}")]
        public IHttpActionResult GetJob(Guid clientId)
        {
            var job = WebApiServer.Instance.GetJob(clientId);
            return Ok(job);
        }

        [HttpPost]
        [Route("result")]
        public IHttpActionResult Result(JobResult result)
        {
            WebApiServer.Instance.ReceiveResult(result);
            return Ok();
        }
    }
}
