using System.ComponentModel.DataAnnotations.Schema;

namespace API_SVsharp.Models.Entity
{
    public class AssetVuln
    {
        public int AssetId { get; set; }
        public Asset Asset { get; set; } = null!;

        public int VulnId { get; set; }
        public Vuln Vuln { get; set; } = null!;
    }
}