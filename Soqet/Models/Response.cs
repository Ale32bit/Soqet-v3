using System.Text.Json.Serialization;

namespace Soqet.Models
{
#pragma warning disable CS8618
    public class Response
    {
        public readonly string Type = "response";
        public int Id { get; set; } = 0;
        public bool Ok { get => Error == null; }
        public string? Error { get; set; }

        [JsonPropertyName("client_id")]
        public string ClientID { get; set; }
        public object? Data { get; set; }
    }
#pragma warning restore CS8618
}
