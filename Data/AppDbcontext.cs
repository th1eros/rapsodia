using Microsoft.EntityFrameworkCore;
using API_SVsharp.Models.Entity;

namespace API_SVsharp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Asset> Assets { get; set; }
        public DbSet<Vuln> Vulns { get; set; }
        public DbSet<AssetVuln> AssetVulns { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==============================
            // CONFIGURAÇÃO N:N
            // ==============================

            modelBuilder.Entity<AssetVuln>()
                .HasKey(av => new { av.AssetId, av.VulnId });

            modelBuilder.Entity<AssetVuln>()
                .HasOne(av => av.Asset)
                .WithMany(a => a.AssetVulns)
                .HasForeignKey(av => av.AssetId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AssetVuln>()
                .HasOne(av => av.Vuln)
                .WithMany(v => v.AssetVulns)
                .HasForeignKey(av => av.VulnId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AssetVuln>()
                .ToTable("AssetsVulnerabilidades");

            // ==============================
            // SOFT DELETE - GLOBAL QUERY FILTER
            // ==============================

            modelBuilder.Entity<Asset>()
                .HasQueryFilter(a => !a.DeletedAt.HasValue);

            modelBuilder.Entity<Vuln>()
                .HasQueryFilter(v => !v.DeletedAt.HasValue);

            // ==============================
            // ENUMS COMO STRING (BOA PRÁTICA)
            // ==============================

            modelBuilder.Entity<Vuln>()
                .Property(v => v.Ambiente)
                .HasConversion(
                    v => v.ToString(),
                    v => (AmbienteVuln)Enum.Parse(typeof(AmbienteVuln), v)
                );

            modelBuilder.Entity<Vuln>()
                .Property(v => v.Nivel)
                .HasConversion(
                    v => v.ToString(),
                    v => (NivelVuln)Enum.Parse(typeof(NivelVuln), v)
                );

            modelBuilder.Entity<Vuln>()
                .Property(v => v.Status)
                .HasConversion(
                    v => v.ToString(),
                    v => (StatusVuln)Enum.Parse(typeof(StatusVuln), v)
                );
        }
    }
}