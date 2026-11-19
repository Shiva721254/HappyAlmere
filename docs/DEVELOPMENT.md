# Reconciled PostgreSQL development verification

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
