#!/usr/bin/env bash
set -euo pipefail

# Bootstrap least-privilege PostgreSQL roles, apply idempotent EF catalog migrations,
# and enforce exact happy_app / happy_backup read-only grants.
#
# Required environment (load from root-owned 0600 file outside Git):
#   POSTGRES_CONTAINER         Docker container name (default: happyalmere-r1-postgres-1)
#   MIGRATION_SQL_FILE         Path to generated EF Core idempotent migration SQL script
#   SCHEMA_MIGRATOR_SECRET     Strong secret for happy_schema_migrator
#   BACKUP_ROLE_SECRET         Strong secret for happy_backup

POSTGRES_CONTAINER="${POSTGRES_CONTAINER:-happyalmere-r1-postgres-1}"
MIGRATION_SQL_FILE="${MIGRATION_SQL_FILE:?Set MIGRATION_SQL_FILE to generated EF Core SQL script}"
SCHEMA_MIGRATOR_SECRET="${SCHEMA_MIGRATOR_SECRET:?Set SCHEMA_MIGRATOR_SECRET}"
BACKUP_ROLE_SECRET="${BACKUP_ROLE_SECRET:?Set BACKUP_ROLE_SECRET}"

docker exec -i "$POSTGRES_CONTAINER" psql -X -v ON_ERROR_STOP=1 -U happy_migrator -d happyalmere \
  --set=migrator_secret="$SCHEMA_MIGRATOR_SECRET" \
  --set=backup_secret="$BACKUP_ROLE_SECRET" <<'SQL'
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'happy_owner') THEN
    CREATE ROLE happy_owner NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
  END IF;
END $$;
SELECT format('CREATE ROLE happy_schema_migrator LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS', :'migrator_secret')
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'happy_schema_migrator') \gexec
SELECT format('CREATE ROLE happy_backup LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS', :'backup_secret')
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'happy_backup') \gexec
GRANT happy_owner TO happy_schema_migrator;
REVOKE ALL ON DATABASE happyalmere FROM PUBLIC;
GRANT CONNECT ON DATABASE happyalmere TO happy_owner, happy_schema_migrator, happy_app, happy_backup;
GRANT CREATE ON DATABASE happyalmere TO happy_owner;
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
GRANT USAGE, CREATE ON SCHEMA public TO happy_owner;
GRANT USAGE ON SCHEMA public TO happy_app, happy_backup;
ALTER DEFAULT PRIVILEGES FOR ROLE happy_owner IN SCHEMA public REVOKE ALL ON TABLES FROM happy_app, happy_backup;
SQL

export PGPASSWORD="$SCHEMA_MIGRATOR_SECRET"
docker exec -i -e PGPASSWORD -e "PGOPTIONS=-c role=happy_owner" "$POSTGRES_CONTAINER" \
  psql -X -v ON_ERROR_STOP=1 \
  --dbname="host=localhost dbname=happyalmere user=happy_schema_migrator sslmode=verify-full sslrootcert=/var/lib/postgresql-tls/server.crt" \
  < "$MIGRATION_SQL_FILE"
unset PGPASSWORD

docker exec -i "$POSTGRES_CONTAINER" psql -X -v ON_ERROR_STOP=1 -U happy_migrator -d happyalmere <<'SQL'
GRANT USAGE ON SCHEMA catalog, discovery TO happy_app, happy_backup;
GRANT SELECT ON catalog.cities, catalog.categories, catalog.category_translations,
  catalog.city_domains, catalog.districts, catalog.locations, discovery.discovery_entries
  TO happy_app, happy_backup;
GRANT SELECT ON public."__EFMigrationsHistory" TO happy_backup;
REVOKE ALL ON public."__EFMigrationsHistory" FROM happy_app;
REVOKE EXECUTE ON ALL FUNCTIONS IN SCHEMA catalog FROM PUBLIC;
ALTER DEFAULT PRIVILEGES FOR ROLE happy_owner IN SCHEMA catalog, discovery REVOKE ALL ON TABLES FROM happy_app, happy_backup;
SQL

echo "Database bootstrap and least-privilege grants verified."
