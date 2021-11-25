namespace Soqet.Models
{
#pragma warning disable CS8618
    public class Response
    {
        public int Id { get; set; } = 0;
        public bool Ok { get => Error == null; }
        public string? Error { get; set; }

        public string ClientID { get; set; }
        public object? Data { get; set; }
    }
#pragma warning restore CS8618
}
