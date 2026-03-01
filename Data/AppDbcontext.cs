using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using API_SVsharp.Models.Entity;

namespace API_SVsharp.Data
{
    public class AppDbContext : DbContext
    {
        // ==============================
        // CONSTRUTOR
        // ==============================

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        // ==============================
        // DBSETS
        // ==============================

        public DbSet<Asset> Assets { get; set; }
        public DbSet<Vuln> Vulns { get; set; }
        public DbSet<AssetVuln> AssetVulns { get; set; }

        // ==============================
        // MODEL CREATING
        // ==============================

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==============================
            // HARDENING - PROTEÇÃO DE AUDITORIA
            // Impede alteração manual de CreatedAt após inserção
            // ==============================

            modelBuilder.Entity<Asset>()
                .Property(a => a.CreatedAt)
                .ValueGeneratedOnAdd()
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

            modelBuilder.Entity<Vuln>()
                .Property(v => v.CreatedAt)
                .ValueGeneratedOnAdd()
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

            modelBuilder.Entity<AssetVuln>()
                .Property(av => av.CreatedAt)
                .ValueGeneratedOnAdd()
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

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

            modelBuilder.Entity<AssetVuln>()
                .HasQueryFilter(av =>
                    !av.Asset.DeletedAt.HasValue &&
                    !av.Vuln.DeletedAt.HasValue);

            // ==============================
            // ENUMS COMO STRING
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

        // ==============================
        // SAVE CHANGES - AUDITORIA AUTOMÁTICA
        // ==============================

        public override int SaveChanges()
        {
            ApplyAudit();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAudit();
            return await base.SaveChangesAsync(cancellationToken);
        }

        // ==============================
        // APPLY AUDIT
        // Centraliza:
        // - CreatedAt automático
        // - UpdatedAt automático
        // - Soft delete automático
        // - Bloqueio de alteração de CreatedAt
        // ==============================

        private void ApplyAudit()
        {
            var entries = ChangeTracker.Entries<BaseEntity>();
            var now = DateTime.UtcNow;

            foreach (var entry in entries)
            {
                // INSERT
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = now;
                }

                // UPDATE
                if (entry.State == EntityState.Modified)
                {
                    entry.Property(nameof(BaseEntity.CreatedAt)).IsModified = false;
                    entry.Entity.UpdatedAt = now;
                }

                // DELETE → SOFT DELETE
                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    entry.Entity.DeletedAt = now;
                }
            }
        }
    }
}