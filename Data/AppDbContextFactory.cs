using System;
using System.IO;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Rapsodia.Infrastructure;

namespace Rapsodia.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
            if (File.Exists(envFile))
                Env.Load(envFile);

            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{env}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var cs = BuildFromEnvVars(migrations: true)
                  ?? PostgresConnectionString.Normalize(config.GetConnectionString("MigrationsConnection"))
                  ?? PostgresConnectionString.Normalize(config.GetConnectionString("DefaultConnection"));

            if (string.IsNullOrWhiteSpace(cs))
                throw new InvalidOperationException(
                    "Connection string não encontrada. " +
                    "Configure DB_HOST_MIGRATIONS, DB_PORT_MIGRATIONS, DB_NAME, DB_USER_MIGRATIONS e DB_PASSWORD no .env " +
                    "ou defina ConnectionStrings:MigrationsConnection no appsettings.json.");

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(cs, o =>
            {
                o.CommandTimeout(300);
                o.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            });

            return new AppDbContext(optionsBuilder.Options);
        }

        internal static string? BuildFromEnvVars(bool migrations)
        {
            var dbName = Environment.GetEnvironmentVariable("DB_NAME");
            var password = Environment.GetEnvironmentVariable("DB_PASSWORD");

            var host = migrations
                ? Environment.GetEnvironmentVariable("DB_HOST_MIGRATIONS")
                  ?? Environment.GetEnvironmentVariable("DB_HOST")
                : Environment.GetEnvironmentVariable("DB_HOST");

            var port = migrations
                ? Environment.GetEnvironmentVariable("DB_PORT_MIGRATIONS") ?? "5432"
                : Environment.GetEnvironmentVariable("DB_PORT") ?? "6543";

            var user = migrations
                ? Environment.GetEnvironmentVariable("DB_USER_MIGRATIONS")
                : Environment.GetEnvironmentVariable("DB_USER");

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(dbName) ||
                string.IsNullOrWhiteSpace(user) ||
                string.IsNullOrWhiteSpace(password))
                return null;

            var pooling = migrations ? "false" : "true";

            return $"Host={host};Port={port};Database={dbName};" +
                   $"Username={user};Password={password};" +
                   $"Pooling={pooling};Ssl Mode=Require;Trust Server Certificate=true;";
        }
    }
}