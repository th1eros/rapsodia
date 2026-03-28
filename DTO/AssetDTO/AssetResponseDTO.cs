using Rapsodia.Models.Entity;
using Rapsodia.DTO.Vuln;
using System.Collections.Generic;

namespace Rapsodia.DTO.Asset
{
    // O JsonStringEnumConverter (registrado no Program.cs) serializa os enums como string.
    // Frontend TypeScript recebe: { "tipo": "WebApplication", "ambiente": "PROD" }
    public class AssetResponseDTO
    {
        public int         Id         { get; set; }
        public string      Nome       { get; set; } = null!;
        public TipoAsset   Tipo       { get; set; }
        public AmbienteVuln Ambiente  { get; set; }
        public bool        Habilitado { get; set; }
        public DateTime    CreatedAt  { get; set; }
        
        // Relacionamento para o Frontend
        public List<VulnResponseDTO>? Vulnerabilities { get; set; }
    }
}
