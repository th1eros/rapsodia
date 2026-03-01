using System;

namespace API_SVsharp.Models.Entity
{
    public class AssetVuln : BaseEntity
    {
        public int AssetId { get; set; }
        public int VulnId { get; set; }

        public Asset Asset { get; set; } = null!;
        public Vuln Vuln { get; set; } = null!;
    }
}