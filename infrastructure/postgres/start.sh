#!/bin/bash
set -euo pipefail
# Local synthetic-data development only. Production certificates belong to deployment infrastructure.
if [ ! -f /var/lib/postgresql-tls/server.key ]; then
  openssl req -x509 -newkey rsa:3072 -nodes -days 90 \
    -subj "/CN=localhost" -addext "subjectAltName=DNS:localhost,IP:127.0.0.1" \
    -keyout /var/lib/postgresql-tls/server.key -out /var/lib/postgresql-tls/server.crt
fi
chown postgres:postgres /var/lib/postgresql-tls/server.*
chmod 600 /var/lib/postgresql-tls/server.key
exec /usr/local/bin/docker-entrypoint.sh postgres \
  -c ssl=on \
  -c ssl_min_protocol_version=TLSv1.2 \
  -c ssl_cert_file=/var/lib/postgresql-tls/server.crt \
  -c ssl_key_file=/var/lib/postgresql-tls/server.key \
  -c hba_file=/opt/happy/pg_hba.conf \
  -c password_encryption=scram-sha-256
