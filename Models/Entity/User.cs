using System.ComponentModel.DataAnnotations;

namespace Rapsodia.Models.Entity
{
    public class User : BaseEntity
    {
        [Required] public string Username     { get; set; } = string.Empty;
        [Required] public string PasswordHash { get; set; } = string.Empty;

        // RBAC: "Analyst" Ã© o papel padrÃ£o. Futuramente: "Admin", "Viewer".
        public string Role { get; set; } = "Analyst";
    }
}
