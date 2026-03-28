namespace Rapsodia.Models.Entity
{
    // Tabela de junÃ§Ã£o N:N entre Asset e Vuln.
    // NÃ£o herda BaseEntity: vÃ­nculos sÃ£o criados ou removidos (hard delete), nunca auditados individualmente.
    // âš ï¸ Rode uma migration apÃ³s esta alteraÃ§Ã£o:
    public class AssetVuln
    {
        public int   AssetId   { get; set; }
        public int   VulnId    { get; set; }
        public Asset Asset     { get; set; } = null!;
        public Vuln  Vuln      { get; set; } = null!;

        // MantÃ©m rastreabilidade de quando o vÃ­nculo foi criado.
        public DateTime CreatedAt { get; set; }
    }
}
