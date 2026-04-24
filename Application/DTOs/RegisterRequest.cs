using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Rapsodia.Application.DTOs
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(120, MinimumLength = 1)]
        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome de usuário é obrigatório.")]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [StringLength(100, MinimumLength = 8)]
        [JsonPropertyName("senha")]
        public string Senha { get; set; } = string.Empty;
    }
}
