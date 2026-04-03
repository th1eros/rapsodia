using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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

            // 1. IMUTABILIDADE DE CRIAÇÃO
            var entitiesWithCreatedAt = new[] { typeof(Asset), typeof(Vuln), typeof(User), typeof(Telemetry) };
            foreach (var type in entitiesWithCreatedAt)
            {
                modelBuilder.Entity(type).Property("CreatedAt")
                    .ValueGeneratedOnAdd().Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            }

            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();

            // 2. CONFIGURAÇÃO N:N (ASSETS-VULNS)
            modelBuilder.Entity<AssetVuln>().HasKey(av => new { av.AssetId, av.VulnId });
            modelBuilder.Entity<AssetVuln>().ToTable("assets_vulnerabilidades");
            modelBuilder.Entity<AssetVuln>().Property(av => av.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<AssetVuln>()
                .HasOne(av => av.Asset).WithMany(a => a.AssetVulns)
                .HasForeignKey(av => av.AssetId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AssetVuln>()
                .HasOne(av => av.Vuln).WithMany(v => v.AssetVulns)
                .HasForeignKey(av => av.VulnId).OnDelete(DeleteBehavior.Restrict);

            // 3. FILTROS GLOBAIS (SOFT DELETE) - Garantindo que registros novos apareçam
            modelBuilder.Entity<Asset>().HasQueryFilter(a => a.DeletedAt == null);
            modelBuilder.Entity<Vuln>().HasQueryFilter(v => v.DeletedAt == null);
            modelBuilder.Entity<User>().HasQueryFilter(u => u.DeletedAt == null);
            modelBuilder.Entity<Telemetry>().HasQueryFilter(t => t.DeletedAt == null);

            // 4. CRIPTOGRAFIA DE CAMADA (CISO STRATEGY)
            // Transforma o dado em Base64 no banco (ilegitível para curiosos) e volta ao normal no App
            var secureConverter = new ValueConverter<string, string>(
                v => Convert.ToBase64String(Encoding.UTF8.GetBytes(v)), // Para o Banco
                v => Encoding.UTF8.GetString(Convert.FromBase64String(v)) // Para o Admin
            );

            modelBuilder.Entity<Telemetry>()
                .Property(t => t.TargetFilePath)
                .HasConversion(secureConverter);

            // 5. CONVERSÃO DE ENUMS
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
                    entry.State = EntityState.Modified;
                    entry.Entity.DeletedAt = now;
                    entry.Entity.UpdatedAt = now;
                }
            }
        }
    }
}