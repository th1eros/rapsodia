using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Rapsodia.Models.Entity;
using Rapsodia.Helpers; 

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
            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();

            // 2. CONFIGURAÇÃO N:N (ASSETS ↔ VULNS)
            modelBuilder.Entity<AssetVuln>().HasKey(av => new { av.AssetId, av.VulnId });
            modelBuilder.Entity<AssetVuln>().ToTable("assets_vulnerabilidades");
            modelBuilder.Entity<AssetVuln>()
                .Property(av => av.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

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

            // 3. FILTROS GLOBAIS (SOFT DELETE)
            modelBuilder.Entity<Asset>().HasQueryFilter(a => a.DeletedAt == null);
            modelBuilder.Entity<Vuln>().HasQueryFilter(v => v.DeletedAt == null);
            modelBuilder.Entity<User>().HasQueryFilter(u => u.DeletedAt == null);
            modelBuilder.Entity<Telemetry>().HasQueryFilter(t => t.DeletedAt == null);
            modelBuilder.Entity<AssetVuln>().HasQueryFilter(
                av => av.Asset.DeletedAt == null && av.Vuln.DeletedAt == null);

            // 4. CRIPTOGRAFIA REAL — AES-256-GCM
            var rawKey = Environment.GetEnvironmentVariable("FIELD_ENCRYPTION_KEY");
            if (!string.IsNullOrWhiteSpace(rawKey))
            {
                var keyBytes = Convert.FromBase64String(rawKey);
                if (keyBytes.Length != 32)
                    throw new InvalidOperationException("FIELD_ENCRYPTION_KEY deve ter 32 bytes.");

                var aesConverter = new ValueConverter<string, string>(
                    v => AesGcmHelper.Encrypt(v, keyBytes),
                    v => AesGcmHelper.Decrypt(v, keyBytes)
                );

                modelBuilder.Entity<Telemetry>()
                    .Property(t => t.TargetFilePath)
                    .HasConversion(aesConverter);
            }

            // 5. CONVERSÃO DE ENUMS PARA STRING
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
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = now;
                        break;
                    case EntityState.Modified:
                        entry.Property(nameof(BaseEntity.CreatedAt)).IsModified = false;
                        entry.Entity.UpdatedAt = now;
                        break;
                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.Entity.DeletedAt = now;
                        entry.Entity.UpdatedAt = now;
                        break;
                }
            }
        }
    }
}