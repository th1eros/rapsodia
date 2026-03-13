using System.ComponentModel.DataAnnotations;

namespace API_SVsharp.Models.Entity
{
    public class User : BaseEntity
    {
        [Required] public string Username     { get; set; } = string.Empty;
        [Required] public string PasswordHash { get; set; } = string.Empty;

        // RBAC: "Analyst" é o papel padrão. Futuramente: "Admin", "Viewer".
        public string Role { get; set; } = "Analyst";
    }
}
