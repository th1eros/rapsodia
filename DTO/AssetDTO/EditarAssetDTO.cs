using System.ComponentModel.DataAnnotations;

namespace API_SVsharp.DTO.Asset
{
    public class EditarAssetDTO
    {
        [MaxLength(150)]
        public string? Nome { get; set; }

        [MaxLength(100)]
        public string? Type { get; set; }

        [MaxLength(10)]
        public string? Environment { get; set; }

        public bool? Active { get; set; }
    }
}