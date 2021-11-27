using System.Text.Json;
using System.Text.Json.Serialization;

namespace Soqet.Models
{
#pragma warning disable CS8618
    public class Message
    {
        public string Channel { get; set; }
        public object Content { get; set; }
        public Dictionary<string, object> Meta { get; set; }
    }
#pragma warning restore CS8618
}
