# Local Docker CI Runner (`scripts/Run-LocalCi.ps1`)

Run the full HappyAlmere verification pipeline locally using your own Docker Engine — **zero GitHub Actions minutes**, works offline, and stays safe on both **Public** and **Private** repositories.

## Prerequisites
- Docker Desktop (or Linux Docker Engine) running locally
- `.NET 10` SDK (`global.json`)
- Python 3
- Node.js (only required if passing `-IncludeBrowser`)

## Local-First Workflow (Recommended)
1. **While developing locally:** Run `.\scripts\Run-LocalCi.ps1 -Quick` (~30–45s) after each change to verify builds, dependency graph, working-tree secrets, unit tests, and the production Docker container `/healthz` smoke test — **zero pushes, zero GitHub CI minutes**.
2. **Once the feature works locally (before pushing):** Run `.\scripts\Run-LocalCi.ps1 -SkipHistoryScan` (~3–4 min) or `.\scripts\Run-LocalCi.ps1` (full git-history scan included) to verify all PostgreSQL 18 migrations, rollback/recovery, Identity isolation, and all 280 tests locally in Docker.
3. **Final PR gate:** Push once to GitHub so GitHub Actions confirms the already-verified branch cleanly on Ubuntu.

## Usage

### 1. Fast inner-loop check (~30–45 seconds)
Runs Debug/Release builds, fast working-tree dependency/JSON/secret scan (`<1s`), NuGet vulnerability audit, non-PostgreSQL unit tests, and the production `Dockerfile` build + live `/healthz` & bad-host `400` smoke test:

```powershell
.\scripts\Run-LocalCi.ps1 -Quick
```

### 2. Full local CI (default — mirrors GitHub Actions)
Runs everything above **plus** full git-history `verify-source.py` (pass `-SkipHistoryScan` on Windows to use the `<1s` working-tree scan while still running every Docker/PostgreSQL stage), isolated PostgreSQL 18 backup/restore (`verify-database-recovery.py`), forward migration + rollback + `HA-005-A` Dutch FTS rehearsal (`verify-forward-migration.py`), Identity-disabled regression tests, isolated Identity contract/privilege/recovery (`verify-identity-foundation.py`), and the full 280-test PostgreSQL 18 / Dutch FTS / City / Identity suite (`Test-Local.ps1`):

```powershell
.\scripts\Run-LocalCi.ps1
# Faster full PostgreSQL 18 + 280-test + container run on Windows:
.\scripts\Run-LocalCi.ps1 -SkipHistoryScan
```

### 3. Full local CI + Playwright browser/responsive/security suite
```powershell
.\scripts\Run-LocalCi.ps1 -IncludeBrowser
```

### 4. Skip container smoke test (only .NET + PostgreSQL stages)
```powershell
.\scripts\Run-LocalCi.ps1 -SkipContainer
```

## Safety & Cleanup Guarantees
- **Non-destructive:** Never deletes Docker volumes or touches unrelated Docker projects. Temporary verification containers are stopped cleanly in a `finally` block; only the ephemeral smoke container `happyalmere-local-ci-smoke` is removed.
- **No self-hosted public-repo risk:** Unlike attaching a GitHub self-hosted runner to a public repository, `Run-LocalCi.ps1` only executes code you explicitly run locally on your machine.
- **Output:** Writes a sanitized stage-by-stage timing report to `artifacts/local-ci/summary.json` (git-ignored).
