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
        [Route("addjobhandler")]
        public IHttpActionResult AddJobHandler()
        {
            var jobScript = Request.Content.ReadAsStringAsync().Result;
            var addHandlerResult = WebApiServer.Instance.AddJobHandler(jobScript);
            return Ok(addHandlerResult);
        }

        [HttpGet]
        [Route("removejobhandler/{id}")]
        public IHttpActionResult RemoveJobHandler(Guid id)
        {
            var success = WebApiServer.Instance.RemoveJobHandler(id);
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

        [HttpPost]
        [Route("addpackage")]
        public IHttpActionResult AddPackage()
        {
            var bytes = Request.Content.ReadAsByteArrayAsync().Result;
            var success = WebApiServer.Instance.RegisterPackage(bytes);
            return success ? (IHttpActionResult)Ok() : BadRequest();
        }

        [HttpGet]
        [Route("getpackages")]
        public IHttpActionResult GetPackages()
        {
            var packages = WebApiServer.Instance.GetRegisteredPackages();
            return Ok(packages);
        }
    }
}
