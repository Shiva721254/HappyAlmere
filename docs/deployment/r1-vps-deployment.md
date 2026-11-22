# R1 Vendor-Neutral EU VPS Deployment & Restore Runbook (`HA-009`)

This runbook applies identically to **STRATO Linux VPS (`VC 2-4`, 4 GB RAM)** and **Hetzner Cloud (`CX23`, 4 GB RAM)** running Ubuntu 24.04 LTS. No production deployment is authorized merely by committing these templates; complete owner release gates (`G01–G15`, `DEF-03`–`DEF-08`) before opening R1 publicly.

## 1. Host hardening (before starting containers)

1. Provision Ubuntu 24.04 LTS with SSH key authentication only (`PasswordAuthentication no`, `PermitRootLogin prohibit-password`).
2. Enable `unattended-upgrades` for automatic security patches.
3. Configure firewall (UFW or provider cloud firewall) to allow **only** TCP `80`, TCP `443`, and restricted SSH; keep PostgreSQL (`5432`) and Kestrel (`8080`) completely closed to the internet.
4. Install official Docker Engine + Docker Compose plugin and `age` (plus `rclone` for independent off-box backup shipping).

## 2. Secrets & configuration (`0600` outside Git)

Create `/etc/happyalmere/staging.env` (`chmod 600`, `chown root:root`) with:
- `HAPPYCITY_DOMAIN` (e.g., staging hostname)
- `HAPPYCITY_ALLOWED_HOSTS` (exact semicolon-separated hostnames, never `*`)
- `POSTGRES_ADMIN_SECRET` (32+ random hex chars for `happy_migrator`)
- `POSTGRES_APP_SECRET` (32+ random hex chars for `happy_app`)
- `HAPPYCITY_RUNTIME_CONNECTION` (`Host=postgres;Port=5432;Database=happyalmere;Username=happy_app;...;SSL Mode=VerifyFull;Root Certificate=/var/lib/postgresql-tls/server.crt`)

Note: `compose.prod.yaml` enables `TrustedProxy__Enabled=true` strictly for Caddy's pinned internal bridge IP (`172.29.0.10`), honors only `X-Forwarded-For` and `X-Forwarded-Proto` (`ForwardLimit=1`), and never trusts `X-Forwarded-Host`.

## 3. Starting and verifying staging

```bash
docker compose --env-file /etc/happyalmere/staging.env -f infrastructure/deploy/compose.prod.yaml up -d
```

- Keep `<meta name="robots" content="noindex,nofollow">` enabled during staging.
- Note on live domains (`DEF-03`): `PostgresCityResolver` restricts registered domains to `.test` during synthetic development; live `.nl` activation requires an explicit reviewed domain-policy amendment before public R1 launch.

## 4. Independent off-box backup & restore drill (`DEF-06`)

1. Schedule `infrastructure/deploy/backup-offbox.sh` nightly via `systemd` timer or `cron`, shipping encrypted `.dump.age` archives to an independent off-box target (separate EU storage or encrypted home Proxmox pull).
2. Before public launch, run at least one fresh isolated restore drill onto a separate container using `age -d` + `pg_restore --single-transaction --exit-on-error` and verify row counts, schemas, and least-privilege `happy_app` access.
