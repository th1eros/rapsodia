using System.ComponentModel.DataAnnotations;

namespace API_SVsharp.DTO.Asset
{
    public class EditarAssetDTO
    {
        [MaxLength(150)]
        public string? Nome { get; set; }

        [MaxLength(100)]
        public string? Tipo { get; set; }

        [MaxLength(10)]
        public string? Ambiente { get; set; }

        public bool? Habilitado { get; set; }
    }
}