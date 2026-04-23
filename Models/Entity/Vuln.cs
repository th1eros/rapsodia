using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Rapsodia.Models.Entity
{
    public enum NivelVuln   { Baixa, Media, Alta, Critica }
    public enum AmbienteVuln { DEV, HML, PROD }
    public enum StatusVuln  { Ativa, Resolvida, Arquivada }

    [Table("Vulnerabilidades")]
    public class Vuln : BaseEntity
    {
        [Required, MaxLength(150)]
        public string       Titulo    { get; set; } = null!;
        public AmbienteVuln Ambiente  { get; set; } = AmbienteVuln.DEV;
        public NivelVuln    Nivel     { get; set; } = NivelVuln.Media;
        public StatusVuln   Status    { get; set; } = StatusVuln.Ativa;
        public DateTime?    ArchivedAt { get; set; }

        [JsonIgnore]
        public ICollection<AssetVuln> AssetVulns { get; set; } = new List<AssetVuln>();
    }
}
