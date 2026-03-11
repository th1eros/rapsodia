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
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Asset> Assets { get; set; }
        public DbSet<Vuln> Vulns { get; set; }
        public DbSet<AssetVuln> AssetVulns { get; set; }
        public DbSet<User> Users { get; set; } // [CISO] Base de Identity

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // [CISO] HARDENING - Proteção de Auditoria e Unicidade
            modelBuilder.Entity<Asset>().Property(a => a.CreatedAt).ValueGeneratedOnAdd()
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

            modelBuilder.Entity<Vuln>().Property(v => v.CreatedAt).ValueGeneratedOnAdd()
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

            modelBuilder.Entity<User>().Property(u => u.CreatedAt).ValueGeneratedOnAdd()
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

            // [CISO] SEGURANÇA - Username deve ser único para evitar ataques de colisão
            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();

            // [CTO] CONFIGURAÇÃO N:N e Tabelas
            modelBuilder.Entity<AssetVuln>().HasKey(av => new { av.AssetId, av.VulnId });
            modelBuilder.Entity<AssetVuln>().ToTable("assets_vulnerabilidades");

            modelBuilder.Entity<AssetVuln>()
                .HasOne(av => av.Asset).WithMany(a => a.AssetVulns)
                .HasForeignKey(av => av.AssetId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AssetVuln>()
                .HasOne(av => av.Vuln).WithMany(v => v.AssetVulns)
                .HasForeignKey(av => av.VulnId).OnDelete(DeleteBehavior.Restrict);

            // [CISO] SOFT DELETE - Filtros Globais (Não listar usuários deletados)
            modelBuilder.Entity<Asset>().HasQueryFilter(a => !a.DeletedAt.HasValue);
            modelBuilder.Entity<Vuln>().HasQueryFilter(v => !v.DeletedAt.HasValue);
            modelBuilder.Entity<User>().HasQueryFilter(u => !u.DeletedAt.HasValue);

            // [CTO] ENUMS COMO STRING
            modelBuilder.Entity<Vuln>().Property(v => v.Ambiente).HasConversion<string>();
            modelBuilder.Entity<Vuln>().Property(v => v.Nivel).HasConversion<string>();
            modelBuilder.Entity<Vuln>().Property(v => v.Status).HasConversion<string>();
        }

        public override int SaveChanges() { ApplyAudit(); return base.SaveChanges(); }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAudit();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyAudit()
        {
            var entries = ChangeTracker.Entries<BaseEntity>();
            var now = DateTime.UtcNow;

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added) entry.Entity.CreatedAt = now;
                if (entry.State == EntityState.Modified)
                {
                    entry.Property(nameof(BaseEntity.CreatedAt)).IsModified = false;
                    entry.Entity.UpdatedAt = now;
                }
                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    entry.Entity.DeletedAt = now;
                }
            }
        }
    }
}