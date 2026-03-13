using System.ComponentModel.DataAnnotations;
using API_SVsharp.Models.Entity;

namespace API_SVsharp.DTO.Vuln
{
    public class VulnCriacaoDTO
    {
        [Required, MaxLength(150)]
        public string Titulo { get; set; } = null!;

        [Required] public AmbienteVuln Ambiente { get; set; }
        [Required] public NivelVuln    Nivel    { get; set; }
        [Required] public StatusVuln   Status   { get; set; }
    }
}
