using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Soqet.Models;
using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace Soqet.Controllers
{
    [Route("ws")]
    [ApiController]
    public class WebSocketController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly List<Client> _clients;

        private readonly long _messagesSize;

        public WebSocketController(ILogger<WebSocketController> logger, IConfiguration configuration, List<Client> clients)
        {
            _logger = logger;
            _configuration = configuration;
            _clients = clients;

            _messagesSize = _configuration.GetValue<long>("WebSocketMaxMessageSize");
        }

        [HttpGet]
        [Route("connect")]
        [Route("connect/{nonce?}")]
        public async Task Index(CancellationToken cancellationToken)
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                var client = new Client();
                _clients.Add(client);

                var buffer = new byte[_messagesSize];
                while(webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    if (result.CloseStatus.HasValue)
                        break;
                    var payload = Encoding.UTF8.GetString(buffer, 0, result.Count);
                }
            }
            else
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                HttpContext.Response.ContentType = "plain/text";
                await HttpContext.Response.WriteAsync("HTTP WebSocket Upgrade is required on this endpoint.\nTry /api/connect for HTTP Long Polling.", cancellationToken: cancellationToken);
            }
        }
    }
}
