# HappyAlmere - preserved PostgreSQL integration candidate

This branch reconciles existing develop c2964ee with approved V0.1 main 0e7739d.
It is a review candidate, not accepted HA-002 implementation. Only synthetic local use.
The existing database schema/migration and both histories are preserved.

## Run the existing database-backed application

Prerequisites: .NET SDK from global.json, Docker Linux containers, PowerShell,
trusted HTTPS development certificate, pinned Node/Playwright for browser tests.
On a fresh isolated synthetic development checkout:

```powershell
dotnet restore HappyAlmere.slnx
./scripts/Initialize-LocalDatabase.ps1
dotnet build HappyAlmere.slnx -c Debug --no-restore
dotnet build HappyAlmere.slnx -c Release --no-restore
./scripts/Test-Local.ps1
npm ci --ignore-scripts
npm run browser:install
. ./scripts/Load-LocalEnvironment.ps1
npm run test:browser
./scripts/Start-Local.ps1
```

Do not initialize a second checkout against existing volumes with unrelated credentials.
Reuse the existing protected environment configuration for the preserved synthetic
instance. Never print credentials or remove volumes; tests roll back their fixtures.
The runtime uses read-only VerifyFull TLS access. No database fallback or app-startup
migration is added. See [existing database guide](docs/V0.2-DATABASE-DISCOVERY.md).

## Review and stop boundary

[HA-001 closure](docs/reports/HA-001-CLOSE-final-report.md),
[ARCH-03](docs/reports/HA-001-ARCH-03-final-architectural-review.md),
[acceptance audit](docs/reports/HA-001-acceptance-audit.md),
[permanent risks](docs/security/security-risk-register.md), and
[HA-002 gap analysis](docs/reports/HA-002-existing-implementation-gap-analysis.md).

Do not implement UUID/schema/catalog/role/backup gaps until independent review.
No authentication, payments, customer data or public deployment. Main and original
develop are preserved; this branch is not automatically merged into either.

The browser harness retains 42 layouts at seven widths and production security/rate
checks. Windows local certificate trust remains supported. Integration CI uses Ubuntu
with actual isolated TLS PostgreSQL and trusted Chromium to exercise the real backend.
Node/Playwright are development dependencies only. Exact CI and local results are in
the closure report; previous V0.1 evidence remains historical.
