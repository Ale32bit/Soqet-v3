namespace Soqet.Models
{
#pragma warning disable CS8618
    public class Message : IRequest
    {
        public string Channel { get; set; }
        public object Content { get; set; }
        public Dictionary<string, object> Meta { get; set; }
    }
#pragma warning restore CS8618
}
