namespace API_SVsharp.Models.Entity
{
    // Tabela de junção N:N entre Asset e Vuln.
    // Não herda BaseEntity: vínculos são criados ou removidos (hard delete), nunca auditados individualmente.
    // ⚠️ Rode uma migration após esta alteração:
    public class AssetVuln
    {
        public int   AssetId   { get; set; }
        public int   VulnId    { get; set; }
        public Asset Asset     { get; set; } = null!;
        public Vuln  Vuln      { get; set; } = null!;

        // Mantém rastreabilidade de quando o vínculo foi criado.
        public DateTime CreatedAt { get; set; }
    }
}
