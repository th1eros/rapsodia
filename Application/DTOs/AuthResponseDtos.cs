using System.Text.Json.Serialization;

namespace Rapsodia.Application.DTOs
{
    public class AuthUserDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;
    }

    public class AuthResponseDataDto
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("expiraEm")]
        public string ExpiraEm { get; set; } = string.Empty;

        [JsonPropertyName("usuario")]
        public AuthUserDto Usuario { get; set; } = new();
    }
}
