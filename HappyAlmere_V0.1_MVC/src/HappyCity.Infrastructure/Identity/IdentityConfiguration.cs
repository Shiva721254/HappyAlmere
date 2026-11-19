using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace HappyCity.Infrastructure.Identity;

public static class IdentityConfiguration
{
    public static string RequireRuntimeConnection(string? connection)
    {
        var settings = new NpgsqlConnectionStringBuilder(DatabaseConfiguration.RequireVerifiedTls(connection));
        if (settings.Username != "happy_identity_app" || !string.IsNullOrEmpty(settings.Options))
            throw new InvalidOperationException("Identity foundation requires its dedicated restricted runtime identity without session overrides.");
        return settings.ConnectionString;
    }

    // This verifies actual authenticated server authority, not merely a connection-string username.
    internal sealed class RuntimeAuthorityInterceptor : DbConnectionInterceptor
    {
        private const string Sql = """
            SELECT current_user = 'happy_identity_app' AND session_user = 'happy_identity_app'
                AND ssl AND NOT rolsuper AND NOT rolcreatedb AND NOT rolcreaterole
                AND NOT rolreplication AND NOT rolbypassrls
                AND NOT EXISTS (SELECT 1 FROM pg_auth_members WHERE member = r.oid)
                AND NOT EXISTS (SELECT 1 FROM unnest(ARRAY['SELECT','INSERT','UPDATE','DELETE']) p
                    WHERE NOT has_table_privilege(current_user, 'identity.users', p))
                AND NOT has_schema_privilege(current_user, 'identity', 'CREATE')
                AND NOT has_table_privilege(current_user, 'identity."__EFMigrationsHistory"', 'SELECT')
            FROM pg_roles r CROSS JOIN pg_stat_ssl s
            WHERE r.rolname = current_user AND s.pid = pg_backend_pid()
            """;

        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            using var command = connection.CreateCommand();
            command.CommandText = Sql;
            if (!Equals(command.ExecuteScalar(), true))
            {
                connection.Close();
                throw new InvalidOperationException("Identity runtime database authority verification failed.");
            }
        }

        public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = Sql;
            if (!Equals(await command.ExecuteScalarAsync(cancellationToken), true))
            {
                await connection.CloseAsync();
                throw new InvalidOperationException("Identity runtime database authority verification failed.");
            }
        }
    }
}
