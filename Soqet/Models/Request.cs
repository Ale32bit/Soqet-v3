using System.Text.Json;

namespace Soqet.Models
{
#pragma warning disable CS8618
    public class Request
    {
        public int Id { get; set; } = 0;
        public string Type { get; set; }
        public JsonElement Data { get; set; }
    }
#pragma warning restore CS8618
}
