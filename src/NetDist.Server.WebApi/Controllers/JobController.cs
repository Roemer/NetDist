using NetDist.Jobs;
using System;
using System.Web.Http;

namespace NetDist.Server.WebApi.Controllers
{
    [RoutePrefix("api/job")]
    public class JobController : ApiController
    {
        [HttpGet]
        [Route("get")]
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
