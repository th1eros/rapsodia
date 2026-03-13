using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace API_SVsharp.Models.Entity
{
    // Tipos de ativo suportados pela plataforma.
    public enum TipoAsset
    {
        OperatingSystem,
        WebApplication,
        Database,
        API,
        Network,
        Other
    }

    public class Asset : BaseEntity
    {
        public string     Nome      { get; set; } = null!;
        public TipoAsset  Tipo      { get; set; }

        // Reutiliza o mesmo enum de Vuln — garante consistência no banco e no frontend.
        public AmbienteVuln Ambiente { get; set; }

        public bool      Habilitado { get; set; } = true;
        public DateTime? ArchivedAt { get; set; }

        [JsonIgnore]
        public ICollection<AssetVuln> AssetVulns { get; set; } = new List<AssetVuln>();
    }
}
