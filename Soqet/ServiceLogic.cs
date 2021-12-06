using Soqet.Models;
using System.Text.Json;

namespace Soqet
{
    public class ServiceLogic
    {
        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };

        private readonly HashSet<Client> _clients;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly string _wildcardChannelName;

        public ServiceLogic(HashSet<Client> clients, IConfiguration configuration, ILogger<ServiceLogic> logger)
        {
            _clients = clients;
            _configuration = configuration;
            _logger = logger;
            _wildcardChannelName = _configuration["WildcardChannelName"];
        }

        public Response ProcessRequest(Client client, Request? request)
        {
            var response = new Response()
            {
                ClientID = client.Id,
            };
            response.Id = request != null ? request.Id : 0;
            if (request != null && !string.IsNullOrEmpty(request.Type))
            {
                switch (request.Type)
                {
                    case "message":
                        Message? messageData = null;
                        if (request.Data is JsonElement)
                        {
                            if (((JsonElement)request.Data).ValueKind != JsonValueKind.Undefined)
                            {
                                messageData = ((JsonElement)request.Data).Deserialize<Message>(JsonOptions);
                            }
                        }
                        else if (request.Data is Message)
                        {
                            messageData = (Message)request.Data;
                        }

                        if (messageData == null || string.IsNullOrEmpty(messageData.Channel))
                        {
                            response.Error = "Invalid message model";
                            break;
                        }

                        messageData.Channel = SanitizeChannelName(messageData.Channel);

                        if (messageData.Channel == _wildcardChannelName)
                        {
                            response.Error = _wildcardChannelName + " is a read-only channel!";
                            break;
                        }

                        messageData.Meta = BuildMeta(client, messageData.Channel, messageData.Meta);

                        var cls = _clients.Where(m => m.Channels.Contains(messageData.Channel) && m.UUID != client.UUID);
                        foreach (var cl in cls)
                        {
                            try
                            {
                                cl.Send(new EventData
                                {
                                    ClientID = cl.Id,
                                    Event = "message",
                                    Data = messageData,
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex.ToString());
                            }
                        }

                        messageData.Channel = _wildcardChannelName;
                        var clsWildcard = _clients.Where(m => m.Channels.Contains(_wildcardChannelName) && m.UUID != client.UUID);
                        foreach (var cl in cls)
                        {
                            try
                            {
                                cl.Send(new EventData
                                {
                                    ClientID = cl.Id,
                                    Event = "message",
                                    Data = messageData,
                                });
                            } catch(Exception ex)
                            {
                                _logger.LogError(ex.ToString());
                            }
                        }

                        break;

                    case "channel":
                        ChannelActionRequest? channelData = null;
                        if (request.Data is JsonElement)
                        {
                            if (((JsonElement)request.Data).ValueKind != JsonValueKind.Undefined)
                            {
                                channelData = ((JsonElement)request.Data).Deserialize<ChannelActionRequest>(JsonOptions);
                            }
                        }
                        else if (request.Data is ChannelActionRequest)
                        {
                            channelData = (ChannelActionRequest)request.Data;
                        }

                        if (channelData == null || string.IsNullOrEmpty(channelData.Channel) || string.IsNullOrEmpty(channelData.Action))
                        {
                            response.Error = "Invalid channel request model";
                            break;
                        }

                        channelData.Channel = SanitizeChannelName(channelData.Channel);

                        if (channelData.Action == "open")
                        {
                            response.Data = client.Channels.Add(channelData.Channel);
                        }
                        else if (channelData.Action == "close")
                        {
                            response.Data = client.Channels.Remove(channelData.Channel);
                        }
                        else
                        {
                            response.Error = "Invalid channel action value";
                        }

                        break;

                    case "authenticate":
                        AuthenticationRequest? authenticationData = null;
                        if (request.Data is JsonElement)
                        {
                            if (((JsonElement)request.Data).ValueKind != JsonValueKind.Undefined)
                            {
                                authenticationData = ((JsonElement)request.Data).Deserialize<AuthenticationRequest>(JsonOptions);
                            }
                        }
                        else if (request.Data is AuthenticationRequest)
                        {
                            authenticationData = (AuthenticationRequest)request.Data;
                        }

                        if (authenticationData == null || string.IsNullOrEmpty(authenticationData.Token))
                        {
                            response.Error = "Invalid token request model";
                            break;
                        }

                        client.Authenticate(authenticationData.Token);
                        response.Data = true;
                        response.ClientID = client.Id;

                        break;
                    case "ping":
                        response.Data = "pong";
                        break;
                    default:
                        response.Error = "Invalid request type";
                        break;
                }
            }
            else
            {
                response.Error = "Invalid request model";
            }

            return response;
        }

        public static async Task<byte[]> SerializeAsync<T>(T obj, CancellationToken cancellationToken = default)
        {
            var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(
                stream,
                obj,
                JsonOptions,
                cancellationToken
            );

            return stream.ToArray();
        }

        public static string SanitizeChannelName(string channel)
        {
            return channel[0..Math.Min(channel.Length, 128)];
        }

        public static Dictionary<string, object> BuildMeta(Client sender, string channel, Dictionary<string, object>? meta = null)
        {
            if (meta == null)
                meta = new Dictionary<string, object>();

            meta["client_id"] = sender.Id;
            meta["client_authenticated"] = sender.IsAuthenticated;
            meta["channel_name"] = channel;
            meta["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            return meta;
        }
    }
}
