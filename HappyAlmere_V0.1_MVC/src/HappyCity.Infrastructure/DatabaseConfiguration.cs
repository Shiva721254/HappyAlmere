using Npgsql;

namespace HappyCity.Infrastructure;

public static class DatabaseConfiguration
{
    public static string RequireVerifiedTls(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Configure ConnectionStrings:HappyCity before starting the application.");
        var settings = new NpgsqlConnectionStringBuilder(connectionString);
        if (settings.SslMode != SslMode.VerifyFull)
            throw new InvalidOperationException("The database connection must use SSL Mode=VerifyFull.");
        if (settings.IncludeErrorDetail || settings.LogParameters)
            throw new InvalidOperationException("Database error detail and parameter logging must remain disabled.");
        return settings.ConnectionString;
    }
}
