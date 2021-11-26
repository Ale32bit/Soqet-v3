using System.Text.Json.Serialization;

namespace Soqet.Models
{
    public class EventData
    {
        public readonly string Type = "event";
        public string Event { get; set; }
        [JsonPropertyName("client_id")]
        public string ClientID { get; set; }
        public object? Data { get; set; }

    }
}
