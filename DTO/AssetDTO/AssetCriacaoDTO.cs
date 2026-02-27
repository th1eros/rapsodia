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
        public string Type { get; set; } = null!;

        [Required]
        [MaxLength(10)]
        public string Environment { get; set; } = null!;

        public bool Active { get; set; } = true;
    }
}