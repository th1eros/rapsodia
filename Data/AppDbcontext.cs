using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Rapsodia.Models.Entity;

namespace Rapsodia.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Asset> Assets { get; set; }
        public DbSet<Vuln> Vulns { get; set; }
        public DbSet<AssetVuln> AssetVulns { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Telemetry> Telemetries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Imutabilidade: Impede alteraÃ§Ã£o manual do timestamp de criaÃ§Ã£o
            modelBuilder.Entity<Asset>().Property(a => a.CreatedAt)
                .ValueGeneratedOnAdd().Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            modelBuilder.Entity<Vuln>().Property(v => v.CreatedAt)
                .ValueGeneratedOnAdd().Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            modelBuilder.Entity<User>().Property(u => u.CreatedAt)
                .ValueGeneratedOnAdd().Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            modelBuilder.Entity<Telemetry>().Property(t => t.CreatedAt)
                .ValueGeneratedOnAdd().Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();

            // ConfiguraÃ§Ã£o N:N
            modelBuilder.Entity<AssetVuln>().HasKey(av => new { av.AssetId, av.VulnId });
            modelBuilder.Entity<AssetVuln>().ToTable("assets_vulnerabilidades");
            modelBuilder.Entity<AssetVuln>().Property(av => av.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<AssetVuln>()
                .HasOne(av => av.Asset).WithMany(a => a.AssetVulns)
                .HasForeignKey(av => av.AssetId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AssetVuln>()
                .HasOne(av => av.Vuln).WithMany(v => v.AssetVulns)
                .HasForeignKey(av => av.VulnId).OnDelete(DeleteBehavior.Restrict);

            // Filtros Globais: ImplementaÃ§Ã£o de Soft Delete
            modelBuilder.Entity<Asset>().HasQueryFilter(a => !a.DeletedAt.HasValue);
            modelBuilder.Entity<Vuln>().HasQueryFilter(v => !v.DeletedAt.HasValue);
            modelBuilder.Entity<User>().HasQueryFilter(u => !u.DeletedAt.HasValue);
            modelBuilder.Entity<Telemetry>().HasQueryFilter(t => !t.DeletedAt.HasValue);

            // ConversÃ£o de Enums para String (Melhor integraÃ§Ã£o com Frontend/TS)
            modelBuilder.Entity<Vuln>().Property(v => v.Ambiente).HasConversion<string>();
            modelBuilder.Entity<Vuln>().Property(v => v.Nivel).HasConversion<string>();
            modelBuilder.Entity<Vuln>().Property(v => v.Status).HasConversion<string>();

            modelBuilder.Entity<Asset>().Property(a => a.Tipo).HasConversion<string>();
            modelBuilder.Entity<Asset>().Property(a => a.Ambiente).HasConversion<string>();
        }

        public override int SaveChanges()
        {
            ApplyAudit();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            ApplyAudit();
            return await base.SaveChangesAsync(ct);
        }

        private void ApplyAudit()
        {
            var entries = ChangeTracker.Entries<BaseEntity>();
            var now = DateTime.UtcNow;

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Property(nameof(BaseEntity.CreatedAt)).IsModified = false;
                    entry.Entity.UpdatedAt = now;
                }
                else if (entry.State == EntityState.Deleted)
                {
                    // InterceptaÃ§Ã£o: Converte Hard Delete em Soft Delete
                    entry.State = EntityState.Modified;
                    entry.Entity.DeletedAt = now;
                    entry.Entity.UpdatedAt = now;
                }
            }
        }
    }
}