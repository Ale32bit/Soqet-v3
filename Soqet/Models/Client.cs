using System.Security.Cryptography;
using System.Text;

namespace Soqet.Models
{
    public class Client
    {
        public readonly Guid UUID = Guid.NewGuid();
        public string Id { get; set; }
        public bool IsAuthenticated { get; set; } = false;
        public readonly HashSet<string> Channels = new();
        public Action<object> Send { get; set; } = data => { };

        public Client(string? token = null)
        {
            if (token == null)
            {
                var uuid = new byte[16];
                RandomNumberGenerator.Fill(uuid);
                Id = "G" + Convert.ToHexString(uuid).ToLower(); // G stands for guest
            } else
            {
                Id = GenerateAuthID(token);
                IsAuthenticated = true;
            }
        }

        public void Authenticate(string token)
        {
            Id = GenerateAuthID(token);
            IsAuthenticated = true;
        }

        // I wasted too much time on this
        private static string GenerateAuthID(string token)
        {
            var buff = Hash(token);
            var uuid = new StringBuilder();

            for (int i = 0; i < 16; i++)
            {
                buff = Hash(Convert.ToHexString(buff).ToLower());
                uuid.Append(buff[0].ToString("x"));
            }

            return "A" + uuid.ToString().ToLower(); // A stands for "authenticated"
        }

        private static byte[] Hash(string input)
        {
            return SHA256.HashData(Encoding.ASCII.GetBytes(input));
        }
    }
}
