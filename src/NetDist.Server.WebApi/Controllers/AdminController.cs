using System;
using System.Web.Http;

namespace NetDist.Server.WebApi.Controllers
{
    [RoutePrefix("api/admin")]
    public class AdminController : ApiController
    {
        [HttpGet]
        [Route("stats")]
        public IHttpActionResult Statistics()
        {
            var statistics = WebApiServer.Instance.GetStatistics();
            return Ok(statistics);
        }

        [HttpPost]
        [Route("addjobscript")]
        public IHttpActionResult AddJobScript()
        {
            var jobScript = Request.Content.ReadAsStringAsync().Result;
            var success = WebApiServer.Instance.AddJobScript(jobScript);
            return success ? (IHttpActionResult)Ok() : BadRequest();
        }

        [HttpGet]
        [Route("removejobscript")]
        public IHttpActionResult RemoveJobScript(Guid id)
        {
            var success = WebApiServer.Instance.RemoveJobScript(id);
            return success ? (IHttpActionResult)Ok() : BadRequest();
        }

        [HttpGet]
        [Route("startjobhandler/{id}")]
        public IHttpActionResult StartJobHandler(Guid id)
        {
            var success = WebApiServer.Instance.StartJobHandler(id);
            return success ? (IHttpActionResult)Ok() : BadRequest();
        }

        [HttpGet]
        [Route("stopjobhandler/{id}")]
        public IHttpActionResult StopJobHandler(Guid id)
        {
            var success = WebApiServer.Instance.StopJobHandler(id);
            return success ? (IHttpActionResult)Ok() : BadRequest();
        }
    }
}
