namespace Rapsodia.Models.Entity
{
    public class AssetVuln
    {
        public int   AssetId   { get; set; }
        public int   VulnId    { get; set; }
        public Asset Asset     { get; set; } = null!;
        public Vuln  Vuln      { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
