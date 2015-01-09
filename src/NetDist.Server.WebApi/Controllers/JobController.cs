using System.Web.Http;

namespace NetDist.Server.WebApi.Controllers
{
    [RoutePrefix("api/job")]
    public class JobController : ApiController
    {
        [Route("get")]
        public IHttpActionResult GetJob()
        {
            object job = null;
            return Ok(job);
        }
    }
}
