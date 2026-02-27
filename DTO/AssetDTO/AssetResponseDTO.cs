using System;

namespace API_SVsharp.DTO.Asset
{
    public class AssetResponseDTO
    {
        public int Id { get; set; }

        public string Nome { get; set; } = null!;

        public string? Descricao { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}