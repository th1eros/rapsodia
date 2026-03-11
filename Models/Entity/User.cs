using System.ComponentModel.DataAnnotations;

namespace API_SVsharp.Models.Entity
{
    public class User : BaseEntity
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = "Analyst"; // [CISO] RBAC: Controle de Acesso Baseado em Função
    }
}