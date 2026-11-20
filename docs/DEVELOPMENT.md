# Reconciled PostgreSQL development verification

## HA-003-A current execution update

Current branch `feature/ha-003-a`, exact accepted baseline `216df25c5656a979fcf67dfe3a16d07d7d66f154`. [IAM-G04 authorization](https://github.com/Shiva721254/HappyAlmere/issues/28#issuecomment-7023159754) and [approved #28 contract](https://github.com/Shiva721254/HappyAlmere/issues/28) supersede historical execution snapshots below only within the bounded persistence scope. [INT-G12 passed](https://github.com/Shiva721254/HappyAlmere/issues/27#issuecomment-7022990043); [IAM-G05 passed before implementation](https://github.com/Shiva721254/HappyAlmere/issues/28#issuecomment-7023218886).

IAM-A01–02 preflight complete; A03–07 entities/context/migration/roles/minimal DI implemented locally; A08–09 verification in progress; A10 draft PR/handover pending. Identity defaults off, with seven functional tables and separate context/history. Existing catalog/discovery source, migrations, original tests and performance thresholds are preserved. Exact authorized changes remain within the 34-path maximum. [Technical dictionary/runbook](database/identity-foundation.md), [acceptance](../ACCEPTANCE.md) and [handover](../HANDOVER.md) own detailed evidence. Debug/Release builds pass with zero warnings/errors; 157 enabled .NET tests and all 105 original tests with Identity disabled pass (zero failures/skips). Eleven isolated database rehearsal checks, 42 responsive cases, 190 functional/security assertions and five rate assertions pass. NuGet/npm audits report no known vulnerabilities. Implementation head d4ec551 passed both push and PR CI; actual links/artifacts are in HANDOVER.md. Final documentation-head CI is reported in the GitHub submission ledger. Independent review remains outstanding.

Owner remains migration coordinator and final approval authority; ChatGPT performs independent HA-003-A-ARCH-01 review. No merge, deployment, public auth endpoints, real accounts, business features or later Identity phases. IAM-OPEN-01–10 and IAM-PRE-D01/D02 are approved as recorded in #28; future cryptographic/session/resource policies remain deferred. Final commit, draft PR and push/PR CI are recorded in the GitHub submission ledger after actual synchronization; no self-referential commit hash is fabricated. Historical content below remains intact.

See the root README and docs/V0.2-DATABASE-DISCOVERY.md for existing protected database
setup. Windows local browser verification remains supported. Integration CI runs the
same browser assertions on Ubuntu with an actual isolated TLS PostgreSQL container.
Linux uses explicit OS/NSS trust; Chromium and Node HTTPS certificate validation stay
on. Python 3 performs the bounded source/history scan. Node is pinned by .node-version,
Playwright by package-lock.json. No Node dependency is deployed with the app.

```powershell
dotnet restore HappyAlmere.slnx --force --no-http-cache
dotnet build HappyAlmere.slnx -c Debug --no-restore -m:1 --disable-build-servers
dotnet build HappyAlmere.slnx -c Release --no-restore -m:1 --disable-build-servers
./scripts/Test-Local.ps1 -EnvironmentFile artifacts/forward/<run>/rehearsal.env
npm ci --ignore-scripts
npm run browser:install
. ./scripts/Load-LocalEnvironment.ps1 -EnvironmentFile artifacts/forward/<run>/rehearsal.env
npm run test:browser
python scripts/verify-source.py
```

The first database setup uses Initialize-LocalDatabase.ps1 only for an isolated
synthetic instance with matching credentials/volumes. Do not create mismatched credentials
against preserved data. In closure, existing protected parent-checkout environment
configuration was loaded without copying it to source or printing values.

The harness starts fresh loopback servers, strips admin/migration credentials from
child environments, verifies actual socket bindings and runs responsive, security and
rate suites separately. It does not raise rate limits or bypass TLS. Linux socket checks
use ss and local IPv4 interface probes; Windows uses the preserved PowerShell probe.
No public perimeter, production load, comprehensive accessibility or device claim.

CI pins actions, keeps contents:read and nonpersistent checkout credentials. It installs
matching Chromium dependencies and libnss3-tools on the ephemeral runner. Only public
certificate material is used for trust; no certificate/key/server logs are uploaded.
Runtime permissions remain read-only; migration is separate and never runs at app startup.
See the closure report for actual results and independent-review stop boundary.


## Forward-phase environment selection

The old initialization command is intentionally pinned to InitialDiscovery and is
only the preserved source fixture. Follow docs/database/backup-and-recovery.md for
isolated forward instances. Pass the exact printed protected environment file to
scripts/Test-Local.ps1 or scripts/Start-Local.ps1 using -EnvironmentFile. Do not run
the new application against the original old-schema .env. Browser tests must load
the same explicit rehearsal environment before npm run test:browser. Keep all
credentials/dumps ignored and stop task containers after verification without deleting volumes.
