using Rapsodia.Models.Entity;
using Rapsodia.DTO.Vuln;
using System.Collections.Generic;

namespace Rapsodia.DTO.Asset
{
    public class AssetResponseDTO
    {
        public int         Id         { get; set; }
        public string      Nome       { get; set; } = null!;
        public TipoAsset   Tipo       { get; set; }
        public AmbienteVuln Ambiente  { get; set; }
        public bool        Habilitado { get; set; }
        public DateTime    CreatedAt  { get; set; }
        public List<VulnResponseDTO>? Vulnerabilities { get; set; }
    }
}
