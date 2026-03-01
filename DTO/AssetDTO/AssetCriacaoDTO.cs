using System.ComponentModel.DataAnnotations;

namespace API_SVsharp.DTO.Asset
{
    public class AssetCriacaoDTO
    {
        [Required]
        [MaxLength(150)]
        public string Nome { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Tipo { get; set; } = null!;

        [Required]
        [MaxLength(10)]
        public string Ambiente { get; set; } = null!;

        public bool Habilitado { get; set; } = true;
    }
}  