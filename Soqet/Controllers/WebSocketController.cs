using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Soqet.Models;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

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
        private string _motd = "Soqet v3";

        public WebSocketController(ILogger<WebSocketController> logger, IConfiguration configuration, List<Client> clients)
        {
            _logger = logger;
            _configuration = configuration;
            _clients = clients;

            _messagesSize = _configuration.GetValue<long>("WebSocketMaxMessageSize");
            _motd = _configuration["MOTD"];
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

                var motdBuffer = await Deserialize(new EventData()
                {
                    Event = "motd",
                    Data = new MOTDEvent
                    {
                        Message = _motd,
                    },
                    ClientID = client.Id,
                }, cancellationToken);

                await webSocket.SendAsync(new ArraySegment<byte>(motdBuffer), WebSocketMessageType.Text, true, cancellationToken);

                var buffer = new byte[_messagesSize];
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    if (result.CloseStatus.HasValue)
                        break;


                    // TODO: Move this logic to a more "central" place
                    Response response = new()
                    {
                        ClientID = client.Id,
                    };

                    try
                    {
                        var reqStream = new MemoryStream(buffer, 0, result.Count);
                        var request = await JsonSerializer.DeserializeAsync<Request>(
                            reqStream,
                            new JsonSerializerOptions
                            {

                            },
                            cancellationToken
                        );

                        response.Id = request != null ? request.Id : 0;
                    }
                    catch (JsonException e)
                    {
                        response.Error = e.Message;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e.ToString());
                        response.Error = "Internal server error";
                    }

                    var resBuffer = await Deserialize(response, cancellationToken);
                    await webSocket.SendAsync(new ArraySegment<byte>(resBuffer), WebSocketMessageType.Text, true, cancellationToken);

                }
            }
            else
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                HttpContext.Response.ContentType = "plain/text";
                await HttpContext.Response.WriteAsync("HTTP WebSocket Upgrade is required on this endpoint.\nTry /api/connect for HTTP Long Polling.", cancellationToken: cancellationToken);
            }
        }

        private static async Task<byte[]> Deserialize<T>(T obj, CancellationToken cancellationToken = default)
        {
            var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(
                stream,
                obj,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false,
                },
                cancellationToken
            );

            return stream.ToArray();
        }
    }
}
