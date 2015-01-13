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
        [Route("addjoblogic")]
        public IHttpActionResult AddJobLogic(string jobLogic)
        {
            var success = WebApiServer.Instance.AddJobLogic(jobLogic);
            return success ? (IHttpActionResult)Ok() : BadRequest();
        }

        [HttpGet]
        [Route("removejoblogic")]
        public IHttpActionResult RemoveJobLogic(Guid id)
        {
            var success = WebApiServer.Instance.RemoveJobLogic(id);
            return success ? (IHttpActionResult)Ok() : BadRequest();
        }

        [HttpGet]
        [Route("startjobhandler")]
        public IHttpActionResult StartJobHandler(Guid id)
        {
            var success = WebApiServer.Instance.StartJobHandler(id);
            return success ? (IHttpActionResult)Ok() : BadRequest();
        }

        [HttpGet]
        [Route("stopjobhandler")]
        public IHttpActionResult StopJobHandler(Guid id)
        {
            var success = WebApiServer.Instance.StopJobHandler(id);
            return success ? (IHttpActionResult)Ok() : BadRequest();
        }
    }
}
