using System.ComponentModel.DataAnnotations;

namespace Rapsodia.Models.Entity
{
    public class User : BaseEntity
    {
        [Required] public string Username     { get; set; } = string.Empty;
        [Required] public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "Analyst";
    }
}
