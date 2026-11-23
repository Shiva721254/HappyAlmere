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

    // RTA-01..16: one bounded read-only statement on every physical/logical EF open.
    // PostgreSQL remains the enforcement boundary after this check; no successful-result cache.
    internal sealed class RuntimeAuthorityInterceptor : DbConnectionInterceptor
    {
        private const string Failure = "Identity runtime database authority verification failed.";
        private const string Sql = """
            WITH tables AS MATERIALIZED (
                SELECT c.oid,c.relname FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                WHERE n.nspname='identity' AND c.relkind='r'
                  AND c.relname IN ('users','roles','user_roles','user_claims','role_claims','user_logins','user_tokens')
            ), sequences AS MATERIALIZED (
                SELECT c.oid FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                WHERE n.nspname='identity' AND c.relkind='S'
                  AND c.relname IN ('user_claims_id_seq','role_claims_id_seq')
            ), history AS MATERIALIZED (
                SELECT c.oid FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                WHERE n.nspname='identity' AND c.relkind='r' AND c.relname='__EFMigrationsHistory'
            )
            SELECT current_user='happy_identity_app' AND session_user='happy_identity_app'
                AND s.ssl AND NOT r.rolsuper AND NOT r.rolcreatedb AND NOT r.rolcreaterole
                AND NOT r.rolreplication AND NOT r.rolbypassrls AND r.rolcanlogin
                AND NOT EXISTS (SELECT 1 FROM pg_auth_members WHERE member=r.oid)
                AND NOT EXISTS (SELECT 1 FROM pg_roles target WHERE target.oid<>r.oid
                    AND (pg_has_role(r.oid,target.oid,'MEMBER') OR pg_has_role(r.oid,target.oid,'SET')))
                AND (SELECT count(*)=7 FROM tables)
                AND NOT EXISTS (SELECT 1 FROM tables t CROSS JOIN unnest(ARRAY['SELECT','INSERT','UPDATE','DELETE']) p
                    WHERE NOT has_table_privilege(r.oid,t.oid,p))
                AND NOT EXISTS (SELECT 1 FROM tables t WHERE has_table_privilege(r.oid,t.oid,'TRUNCATE,REFERENCES,TRIGGER,MAINTAIN')
                    OR has_any_column_privilege(r.oid,t.oid,'REFERENCES'))
                AND NOT EXISTS (SELECT 1 FROM tables t CROSS JOIN unnest(ARRAY['SELECT','INSERT','UPDATE','DELETE','TRUNCATE','REFERENCES','TRIGGER','MAINTAIN']) p
                    WHERE has_table_privilege(r.oid,t.oid,p||' WITH GRANT OPTION'))
                AND NOT EXISTS (SELECT 1 FROM tables t CROSS JOIN unnest(ARRAY['SELECT','INSERT','UPDATE','REFERENCES']) p
                    WHERE has_any_column_privilege(r.oid,t.oid,p||' WITH GRANT OPTION'))
                AND (SELECT count(*)=1 FROM history)
                AND NOT EXISTS (SELECT 1 FROM history h WHERE has_table_privilege(r.oid,h.oid,'SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER,MAINTAIN')
                    OR has_any_column_privilege(r.oid,h.oid,'SELECT,INSERT,UPDATE,REFERENCES'))
                AND (SELECT count(*)=2 FROM sequences)
                AND NOT EXISTS (SELECT 1 FROM sequences q WHERE NOT has_sequence_privilege(r.oid,q.oid,'USAGE')
                    OR has_sequence_privilege(r.oid,q.oid,'SELECT,UPDATE,USAGE WITH GRANT OPTION'))
                AND has_schema_privilege(r.oid,'identity','USAGE')
                AND NOT has_schema_privilege(r.oid,'identity','CREATE,USAGE WITH GRANT OPTION')
                AND has_database_privilege(r.oid,current_database(),'CONNECT')
                AND NOT has_database_privilege(r.oid,current_database(),'CREATE,TEMP,CONNECT WITH GRANT OPTION')
                AND NOT EXISTS (SELECT 1 FROM pg_namespace n WHERE n.nspname IN ('identity','catalog','discovery','public') AND n.nspowner=r.oid)
                AND NOT EXISTS (SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                    WHERE n.nspname IN ('identity','catalog','discovery','public') AND c.relowner=r.oid)
                AND NOT EXISTS (SELECT 1 FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
                    WHERE n.nspname IN ('identity','catalog','discovery','public') AND p.proowner=r.oid)
                AND NOT has_schema_privilege(r.oid,'catalog','USAGE,CREATE')
                AND NOT has_schema_privilege(r.oid,'discovery','USAGE,CREATE')
            FROM pg_roles r CROSS JOIN pg_stat_ssl s
            WHERE r.rolname=current_user AND s.pid=pg_backend_pid()
            """;

        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            try
            {
                RequireRuntimeConnection(connection.ConnectionString);
                using var command = connection.CreateCommand();
                command.CommandTimeout = 5;
                command.CommandText = Sql;
                if (!Equals(command.ExecuteScalar(), true)) throw new InvalidOperationException(Failure);
            }
            catch
            {
                try { connection.Close(); }
                catch { connection.Dispose(); }
                // Do not expose server SQL, identifiers or connection details in caller diagnostics.
                throw new InvalidOperationException(Failure);
            }
        }

        public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                RequireRuntimeConnection(connection.ConnectionString);
                await using var command = connection.CreateCommand();
                command.CommandTimeout = 5;
                command.CommandText = Sql;
                if (!Equals(await command.ExecuteScalarAsync(cancellationToken), true))
                    throw new InvalidOperationException(Failure);
            }
            catch
            {
                // Cleanup must not be cancelled with the failed request.
                try { await connection.CloseAsync(); }
                catch { await connection.DisposeAsync(); }
                cancellationToken.ThrowIfCancellationRequested();
                throw new InvalidOperationException(Failure);
            }
        }
    }
}
