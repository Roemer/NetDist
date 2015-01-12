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
        [Route("startjoblogic")]
        public IHttpActionResult StartJobLogic(Guid id)
        {
            var success = WebApiServer.Instance.StartJobLogic(id);
            return success ? (IHttpActionResult)Ok() : BadRequest();
        }

        [HttpGet]
        [Route("stopjoblogic")]
        public IHttpActionResult StopJobLogic(Guid id)
        {
            var success = WebApiServer.Instance.StopJoblogic(id);
            return success ? (IHttpActionResult)Ok() : BadRequest();
        }
    }
}
