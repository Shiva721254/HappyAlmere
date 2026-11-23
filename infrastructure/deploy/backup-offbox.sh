#!/usr/bin/env bash
set -euo pipefail

# Nightly encrypted PostgreSQL backup using least-privilege happy_backup over VerifyFull TLS.
# Required environment (load from root-owned 0600 file outside Git):
#   POSTGRES_CONTAINER   Docker container name (default: happyalmere-r1-postgres-1)
#   BACKUP_SECRET_FILE   Path to 0600 file holding happy_backup secret (read via stdin/env, never argv)
#   AGE_RECIPIENT        Public age recipient key for off-box encryption
#   BACKUP_DIR           Local 0700 staging directory (default: /var/backups/happyalmere)
#   OFFBOX_REMOTE        Optional rclone destination (e.g. hetzner-box:happyalmere-backups)

POSTGRES_CONTAINER="${POSTGRES_CONTAINER:-happyalmere-r1-postgres-1}"
BACKUP_DIR="${BACKUP_DIR:-/var/backups/happyalmere}"
BACKUP_SECRET_FILE="${BACKUP_SECRET_FILE:?Set BACKUP_SECRET_FILE to a 0600 secret file}"
AGE_RECIPIENT="${AGE_RECIPIENT:?Set AGE_RECIPIENT public key for backup encryption}"

umask 077
mkdir -p "$BACKUP_DIR"
chmod 700 "$BACKUP_DIR"

STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
OUT_FILE="${BACKUP_DIR}/happyalmere-${STAMP}.dump.age"
SHA_FILE="${OUT_FILE}.sha256"

# Stream pg_dump over VerifyFull TLS directly into age encryption so no unencrypted dump hits disk.
IFS= read -r PGPASSWORD < "$BACKUP_SECRET_FILE"
export PGPASSWORD
docker exec -e PGPASSWORD "$POSTGRES_CONTAINER" \
  pg_dump --dbname="host=localhost dbname=happyalmere user=happy_backup sslmode=verify-full sslrootcert=/var/lib/postgresql-tls/server.crt" \
  -Fc --schema=public --schema=catalog --schema=discovery \
  | age -r "$AGE_RECIPIENT" -o "$OUT_FILE"
unset PGPASSWORD

sha256sum "$OUT_FILE" > "$SHA_FILE"

if [ -n "${OFFBOX_REMOTE:-}" ]; then
  rclone copy "$OUT_FILE" "$OFFBOX_REMOTE"
  rclone copy "$SHA_FILE" "$OFFBOX_REMOTE"
fi

# Retain 7 days locally; off-box target holds independent retention.
find "$BACKUP_DIR" -maxdepth 1 -type f -name 'happyalmere-*.dump.age*' -mtime +7 -delete
echo "Backup complete: $(basename "$OUT_FILE")"
