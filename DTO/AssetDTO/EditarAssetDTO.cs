using System.ComponentModel.DataAnnotations;
using Rapsodia.Models.Entity;

namespace Rapsodia.DTO.Asset
{
    public class EditarAssetDTO
    {
        [MaxLength(150)]
        public string? Nome { get; set; }

        public TipoAsset?   Tipo      { get; set; }
        public AmbienteVuln? Ambiente  { get; set; }
        public bool?         Habilitado { get; set; }
    }
}
