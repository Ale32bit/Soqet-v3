namespace Soqet.Models
{
#pragma warning disable CS8618
    public class ChannelActionRequest : IRequest
    {
        public string Channel { get; set; }
        public string Action { get; set; }
    }
#pragma warning restore CS8618
}
