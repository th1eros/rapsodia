using System.ComponentModel.DataAnnotations;
using Rapsodia.Models.Entity;

namespace Rapsodia.DTO.Asset
{
    public class AssetCriacaoDTO
    {
        [Required, MaxLength(150)]
        public string Nome { get; set; } = null!;

        [Required]
        public TipoAsset Tipo { get; set; }

        [Required]
        public AmbienteVuln Ambiente { get; set; }

        public bool Habilitado { get; set; } = true;
    }
}
