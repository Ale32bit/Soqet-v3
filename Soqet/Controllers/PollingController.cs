using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Soqet.Controllers
{
    [Route("api")]
    [ApiController]
    public class PollingController : ControllerBase
    {
        [HttpGet]
        [Route("connect")]
        public async Task<IActionResult> Index()
        {
            return Ok();
        }
    }
}
