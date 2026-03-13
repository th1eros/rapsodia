using API_SVsharp.Models.Entity;

namespace API_SVsharp.DTO.Vuln
{
    public class VulnResponseDTO
    {
        public int         Id        { get; set; }
        public string      Titulo    { get; set; } = null!;
        public AmbienteVuln Ambiente { get; set; }
        public NivelVuln   Nivel     { get; set; }
        public StatusVuln  Status    { get; set; }
        public DateTime    CreatedAt { get; set; }
    }
}
