using Rapsodia.Models.Entity;

namespace Rapsodia.DTO.Vuln
{
    public class EditarVulnDTO
    {
        public string?      Titulo   { get; set; }
        public AmbienteVuln? Ambiente { get; set; }
        public NivelVuln?   Nivel    { get; set; }
        public StatusVuln?  Status   { get; set; }
    }
}
