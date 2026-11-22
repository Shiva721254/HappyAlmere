#!/bin/bash
set -euo pipefail
# Deployment PostgreSQL entrypoint: uses operator-mounted TLS cert/key if present,
# or initializes an internal TLS certificate valid for DNS:postgres and loopback VerifyFull.
if [ ! -f /var/lib/postgresql-tls/server.key ]; then
  openssl req -x509 -newkey rsa:3072 -nodes -days 365 \
    -subj "/CN=postgres" -addext "subjectAltName=DNS:postgres,DNS:localhost,IP:127.0.0.1" \
    -keyout /var/lib/postgresql-tls/server.key -out /var/lib/postgresql-tls/server.crt
fi
chown postgres:postgres /var/lib/postgresql-tls/server.*
chmod 600 /var/lib/postgresql-tls/server.key
chmod 644 /var/lib/postgresql-tls/server.crt
exec /usr/local/bin/docker-entrypoint.sh postgres \
  -c ssl=on \
  -c ssl_min_protocol_version=TLSv1.2 \
  -c ssl_cert_file=/var/lib/postgresql-tls/server.crt \
  -c ssl_key_file=/var/lib/postgresql-tls/server.key \
  -c hba_file=/opt/happy/pg_hba.conf \
  -c password_encryption=scram-sha-256 \
  -c shared_buffers=256MB \
  -c effective_cache_size=768MB
