using Microsoft.EntityFrameworkCore;
using Rapsodia.Data;

public static class MigrationRunner
{
    public static void ApplyMigrations(IServiceProvider serviceProvider, string connectionString, string dbPassword)
    {
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
        if (string.IsNullOrEmpty(dbPassword)) throw new ArgumentNullException(nameof(dbPassword));

        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            logger.LogInformation("🔄 Iniciando migração do banco de dados...");

            if (string.IsNullOrEmpty(dbContext.Database.GetConnectionString()))
                dbContext.Database.SetConnectionString(connectionString);

            var migrationHost = Environment.GetEnvironmentVariable("DB_HOST_MIGRATIONS");
            var dbName = Environment.GetEnvironmentVariable("DB_NAME");
            var dbUserMig = Environment.GetEnvironmentVariable("DB_USER_MIGRATIONS");

            if (!string.IsNullOrEmpty(migrationHost) && !string.IsNullOrEmpty(dbPassword))
            {
                var migrationConnString = $"Host={migrationHost};Port=5432;Database={dbName};Username={dbUserMig};Password={dbPassword};Ssl Mode=Require;Trust Server Certificate=true;";
                dbContext.Database.SetConnectionString(migrationConnString);
                logger.LogInformation("🔄 Migrando via conexão direta (porta 5432)...");
            }
            else
            {
                logger.LogWarning("⚠️ DB_HOST_MIGRATIONS não definido – migrando via connection string padrão.");
            }

            dbContext.Database.Migrate();
            logger.LogInformation("✅ Banco de dados sincronizado!");
            dbContext.Database.SetConnectionString(connectionString);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "❌ ERRO NA MIGRAÇÃO: {Message}", ex.Message);
            throw;
        }
    }
}