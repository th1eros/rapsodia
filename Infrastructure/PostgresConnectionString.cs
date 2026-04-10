using System;

namespace Rapsodia.Infrastructure;

public static class PostgresConnectionString
{
    public static string? Normalize(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return connectionString;

        if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
            return connectionString;

        if (!connectionString.StartsWith("postgres", StringComparison.OrdinalIgnoreCase))
            return connectionString;

        var databaseUri = new Uri(connectionString);
        var userInfo = databaseUri.UserInfo.Split(':', 2);
        var dbPort = databaseUri.Port == -1 ? 5432 : databaseUri.Port;

        var host = databaseUri.Host.Replace("tcp://", "").Replace("/", "");

        var dbName = databaseUri.AbsolutePath.TrimStart('/');
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = Uri.UnescapeDataString(userInfo[1]);

        return $"Host={host};Port={dbPort};Database={dbName};Username={username};Password={password};Ssl Mode=Require;Trust Server Certificate=true;";
    }
}