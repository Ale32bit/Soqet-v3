using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Soqet.Models;

namespace Soqet.Controllers
{
    [Route("api")]
    [ApiController]
    public class PollingController : ControllerBase
    {
        private readonly HashSet<Client> _clients;
        private readonly ServiceLogic _logic;


        public PollingController(HashSet<Client> clients, ServiceLogic logic)
        {
            _clients = clients;
            _logic = logic;
        }

        [HttpGet]
        [Route("connect")]
        public async Task<IActionResult> Index()
        {
            return Ok();
        }
    }
}
