using System.ComponentModel.DataAnnotations;

namespace Rapsodia.Application.DTOs
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "O nome de usuÃ¡rio Ã© obrigatÃ³rio.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "O nome de usuÃ¡rio deve ter entre 3 e 50 caracteres.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "A senha Ã© obrigatÃ³ria.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter no mÃ­nimo 8 caracteres.")]
        public string Password { get; set; } = string.Empty;
    }
}
