using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Soqet.Models;
using System.Collections.Concurrent;

namespace Soqet.Controllers
{
    /// <summary>
    /// Connect to the Soqet Network by using HTTP Long Polling API
    /// </summary>
    [Route("api")]
    [ApiController]
    [Produces("application/json", Type = typeof(Response))]
    public class PollingController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly HashSet<Client> _clients;
        private readonly ServiceLogic _logic;
        private static ConcurrentDictionary<Guid, ConcurrentQueue<object>> messages = new();
        private static ConcurrentDictionary<Guid, DateTime> updateTime = new();

        private static TimeSpan ExpireTime = TimeSpan.FromMinutes(5);

        public PollingController(ILogger<PollingController> logger, IConfiguration configuration, HashSet<Client> clients, ServiceLogic logic)
        {
            _logger = logger;
            _configuration = configuration;
            _clients = clients;
            _logic = logic;

            DestroyExpiredClients();
        }

        /// <summary>
        /// Instatiate a HTTP Long Polling client
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("connect")]
        public async Task<IActionResult> Index()
        {
            var client = new Client();

            client.Send = (data) =>
            {
                messages[client.UUID].Enqueue(data);

                DestroyExpiredClients();
            };

            _clients.Add(client);
            messages.TryAdd(client.UUID, new());
            updateTime.TryAdd(client.UUID, DateTime.UtcNow);

            return Ok(new Response
            {
                ClientID = client.Id,
                Data = client.UUID.ToString(),
            });
        }

        /// <summary>
        /// Send a message to a channel
        /// </summary>
        /// <param name="key"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("message/{key:guid}")]
        public async Task<IActionResult> Message(Guid key, [FromBody] Message message)
        {
            var client = _clients.FirstOrDefault(m => m.UUID == key);
            if (client == null)
            {
                return NotFound();
            }

            var response = _logic.ProcessRequest(client, new Request
            {
                Type = "message",
                Data = message,
            });

            return new JsonResult(response);
        }


        /// <summary>
        /// Open or close a channel
        /// </summary>
        /// <param name="key"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("channel/{key:guid}")]
        public async Task<IActionResult> Channel(Guid key, [FromBody] ChannelActionRequest channel)
        {
            var client = _clients.FirstOrDefault(m => m.UUID == key);
            if (client == null)
            {
                return NotFound();
            }

            var result = _logic.ProcessRequest(client, new Request
            {
                Type = "channel",
                Data = channel,
            });

            return new JsonResult(result);
        }

        /// <summary>
        /// Authenticate the client
        /// </summary>
        /// <param name="key"></param>
        /// <param name="authentication"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("authenticate/{key}")]
        public async Task<IActionResult> Authenticate(Guid key, [FromBody] AuthenticationRequest authentication)
        {
            var client = _clients.FirstOrDefault(m => m.UUID == key);
            if (client == null)
            {
                return NotFound();
            }

            var result = _logic.ProcessRequest(client, new Request
            {
                Type = "authenticate",
                Data = authentication,
            });

            return new JsonResult(result);
        }

        /// <summary>
        /// Update and get the received messages
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("update/{key}")]
        public async Task<IActionResult> Update(Guid key)
        {
            var client = _clients.FirstOrDefault(m => m.UUID == key);
            if (client == null)
            {
                return NotFound();
            }

            var result = new JsonResult(new Response
            {
                ClientID = client.Id,
                Data = messages[client.UUID],
            });

            messages[client.UUID].Clear();
            updateTime[client.UUID] = DateTime.UtcNow;

            return result;
        }

        /// <summary>
        /// Get the message of the day
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("motd")]
        public async Task<IActionResult> Motd()
        {
            return new JsonResult(new Response
            {
                Data = _configuration["MOTD"],
            });
        }

        private void DestroyExpiredClients()
        {
            foreach (var client in updateTime.Where(m => m.Value + ExpireTime < DateTime.UtcNow))
            {
                _clients.Remove(_clients.First(m => m.UUID == client.Key));
                messages.TryRemove(client.Key, out _);
                updateTime.TryRemove(client.Key, out _);
            }
        }
    }
}
