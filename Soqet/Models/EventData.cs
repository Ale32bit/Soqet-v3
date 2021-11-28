using System.Text.Json.Serialization;

namespace Soqet.Models
{
    public class EventData
    {
        public string Type { get; set; } = "event";
        public string Event { get; set; }
        [JsonPropertyName("client_id")]
        public string ClientID { get; set; }
        public object? Data { get; set; }

    }
}
