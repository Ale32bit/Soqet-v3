using Microsoft.AspNetCore.Mvc;
using Soqet.Models;
using System.Net;
using System.Net.WebSockets;
using System.Text.Json;

namespace Soqet.Controllers
{
    [Route("ws")]
    [ApiController]
    public class WebSocketController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly HashSet<Client> _clients;
        private readonly ServiceLogic _logic;

        private readonly long _messagesSize;
        private readonly string _motd = "Soqet v3";

        public WebSocketController(ILogger<WebSocketController> logger, IConfiguration configuration, HashSet<Client> clients, ServiceLogic logic)
        {
            _logger = logger;
            _configuration = configuration;
            _clients = clients;
            _logic = logic;

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
                var client = new Client
                {
                    Send = async (data) =>
                    {
                        if (webSocket.State == WebSocketState.Open)
                        {
                            try
                            {
                                var serialized = await ServiceLogic.SerializeAsync(data);
                                await webSocket.SendAsync(new ArraySegment<byte>(serialized), WebSocketMessageType.Text, true, cancellationToken);
                            }
                            catch (WebSocketException)
                            {
                                // we ignore it
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, nameof(Client.Send));
                            }
                        }
                    }
                };

                _clients.Add(client);

                client.Send(new EventData()
                {
                    Event = "motd",
                    Data = new MOTDEvent
                    {
                        Message = _motd,
                    },
                    ClientID = client.Id,
                });

                var buffer = new byte[_messagesSize];
                while (webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result;
                    try
                    {
                        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                        if (result.CloseStatus.HasValue)
                            break;
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    Response response = new()
                    {
                        ClientID = client.Id,
                    };

                    try
                    {
                        var reqStream = new MemoryStream(buffer, 0, result.Count);
                        var request = await JsonSerializer.DeserializeAsync<Request>(
                            reqStream,
                            ServiceLogic.JsonOptions,
                            cancellationToken
                        );

                        response = _logic.ProcessRequest(client, request);
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

                    var resBuffer = await ServiceLogic.SerializeAsync(response, cancellationToken);
                    await webSocket.SendAsync(new ArraySegment<byte>(resBuffer), WebSocketMessageType.Text, true, cancellationToken);
                }

                _clients.Remove(client);
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
