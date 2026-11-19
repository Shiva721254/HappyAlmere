## Current HA-002-R02 verification closure

[Issue #14](https://github.com/Shiva721254/HappyAlmere/issues/14) authorizes bounded verification/evidence work only. See [current report](docs/reports/HA-002-implementation-report.md) and [HANDOVER](HANDOVER.md). Applicable privilege/grant-option checks, independent SQL-only definitions and correction-specific rollback/no-op proof extend verification while preserving existing application/migrations/security/thresholds. No merge, deployment, new features or independent acceptance claim. Earlier entries below retain historical evidence.

> **Current development checkpoint:** HA-002-R01 remediation candidate on `feature/ha-002-a`; [Issue #10](https://github.com/Shiva721254/HappyAlmere/issues/10), [draft PR #6](https://github.com/Shiva721254/HappyAlmere/pull/6). See [HANDOVER](HANDOVER.md) and [current report](docs/reports/HA-002-implementation-report.md) for evidence and remaining independent review gates. Earlier phase descriptions below remain historical. No public deployment or merge is authorized.

# HappyAlmere - HA-002-A forward migration review candidate

This branch reconciles existing develop c2964ee with approved V0.1 main 0e7739d.
It is a review candidate, not accepted HA-002 implementation. Only synthetic local use.
The original database and migration history are preserved; the authorized forward catalog migration is rehearsed only on isolated synthetic clones.

## Shared implementation record

Start with [HANDOVER.md](HANDOVER.md) for the current branch, implementation checkpoints,
verification evidence, outstanding issues and review gate. GitHub is the shared source
of truth; ChatGPT reviews independently and the project owner retains final approval.

## Run the existing database-backed application

Prerequisites: .NET SDK from global.json, Docker Linux containers, PowerShell,
trusted HTTPS development certificate, pinned Node/Playwright for browser tests.
On a fresh isolated synthetic development checkout:

```powershell
dotnet restore HappyAlmere.slnx
./scripts/Initialize-LocalDatabase.ps1 # InitialDiscovery baseline only
dotnet build HappyAlmere.slnx -c Debug --no-restore
dotnet build HappyAlmere.slnx -c Release --no-restore
python scripts/verify-database-recovery.py
python scripts/verify-forward-migration.py --gate artifacts/ha002a/<run>/evidence.json
./scripts/Test-Local.ps1 -EnvironmentFile artifacts/forward/<run>/rehearsal.env
npm ci --ignore-scripts
npm run browser:install
. ./scripts/Load-LocalEnvironment.ps1 -EnvironmentFile artifacts/forward/<run>/rehearsal.env
npm run test:browser
./scripts/Start-Local.ps1 -EnvironmentFile artifacts/forward/<run>/rehearsal.env
```

Replace <run> with the exact paths printed by each successful runner.
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


Database review specifications (consolidated from the owner-supplied requirements):

- [Database architecture](docs/database/database-architecture.md)
- [18 HA-002 acceptance criteria](docs/requirements/HA-002-acceptance-criteria.md)
- [Evidence-based 18-criterion gap analysis](docs/reports/HA-002-existing-implementation-gap-analysis.md)

These specifications describe the intended architecture, not completed implementation
or HA-002 approval. Independent review of draft PR #3 precedes new feature work.


## HA-002-A recovery/design progress

[Issue #4](https://github.com/Shiva721254/HappyAlmere/issues/4) is active. The owner-authorized
inventory/backup/isolated restore/design phase has evidence in the
[implementation report](docs/reports/HA-002-implementation-report.md). Reuse the
[backup runbook](docs/database/backup-and-recovery.md) and review the
[non-destructive migration proposal](docs/database/migration-policy.md). No forward
migration or full HA-002 acceptance is claimed; preserve original source/volumes.


## HA-002-A forward review candidate

Owner disposition D01-D10 is recorded in Issue #4. See the latest
[implementation report](docs/reports/HA-002-implementation-report.md),
[ERD](docs/database/entity-relationship-diagram.md), [dictionary](docs/database/data-dictionary.md),
[security](docs/database/database-security.md) and [recovery runbook](docs/database/backup-and-recovery.md).
Use `scripts/Start-Local.ps1 -EnvironmentFile artifacts/forward/<run>/rehearsal.env`
only after a successful isolated forward run; do not point the new application at the
preserved old schema. No main merge, production deployment or final acceptance.
