using NetDist.Core;
using System;
using System.Net.Http;
using System.Web.Http;

namespace NetDist.Server.WebApi.Controllers
{
    [RoutePrefix("api/admin")]
    public class AdminController : ControllerBase
    {
        public AdminController(WebApiServer server)
            : base(server) { }

        [HttpGet]
        [Route("stats")]
        public IHttpActionResult Statistics()
        {
            var statistics = Server.GetStatistics();
            return Ok(statistics);
        }

        [HttpGet]
        [Route("joblog/{id}")]
        public IHttpActionResult GetJobLog(Guid id)
        {
            var log = Server.GetJobLog(id);
            return Ok(log);
        }

        [HttpPost]
        [Route("addjobscript")]
        public IHttpActionResult AddJobScript()
        {
            var jobScriptInfo = Request.Content.ReadAsAsync<JobScriptInfo>().Result;
            var addHandlerResult = Server.AddJobScript(jobScriptInfo);
            return Ok(addHandlerResult);
        }

        [HttpGet]
        [Route("removejobscript/{id}")]
        public IHttpActionResult RemoveJobScript(Guid id)
        {
            var success = Server.RemoveJobScript(id);
            return success ? (IHttpActionResult)Ok() : BadRequest();
        }

        [HttpGet]
        [Route("startjobscript/{id}")]
        public IHttpActionResult StartJobScript(Guid id)
        {
            var success = Server.StartJobScript(id);
            return success ? (IHttpActionResult)Ok() : BadRequest();
        }

        [HttpGet]
        [Route("stopjobscript/{id}")]
        public IHttpActionResult StopJobScript(Guid id)
        {
            var success = Server.StopJobScript(id);
            return success ? (IHttpActionResult)Ok() : BadRequest();
        }

        [HttpGet]
        [Route("pausejobscript/{id}")]
        public IHttpActionResult PauseJobScript(Guid id)
        {
            var success = Server.PauseJobScript(id);
            return success ? (IHttpActionResult)Ok() : BadRequest();
        }

        [HttpGet]
        [Route("disablejobscript/{id}")]
        public IHttpActionResult DisableJobScript(Guid id)
        {
            var success = Server.DisableJobScript(id);
            return success ? (IHttpActionResult)Ok() : BadRequest();
        }

        [HttpGet]
        [Route("enablejobscript/{id}")]
        public IHttpActionResult EnableJobScript(Guid id)
        {
            var success = Server.EnableJobScript(id);
            return success ? (IHttpActionResult)Ok() : BadRequest();
        }

        [HttpGet]
        [Route("removeclient/{id}")]
        public IHttpActionResult RemoveClient(Guid id)
        {
            var success = Server.RemoveClient(id);
            return success ? (IHttpActionResult)Ok() : BadRequest();
        }

        [HttpPost]
        [Route("addpackage")]
        public IHttpActionResult AddPackage()
        {
            var multiPart = Request.Content.ReadAsMultipartAsync().Result;
            var packageInfo = multiPart.Contents[0].ReadAsAsync<PackageInfo>().Result;
            var zipFile = multiPart.Contents[1].ReadAsByteArrayAsync().Result;
            var success = Server.RegisterPackage(packageInfo, zipFile);
            return success ? (IHttpActionResult)Ok() : BadRequest();
        }

        [HttpGet]
        [Route("getpackages")]
        public IHttpActionResult GetPackages()
        {
            var packages = Server.GetRegisteredPackages();
            return Ok(packages);
        }
    }
}
