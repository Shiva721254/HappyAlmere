# HappyAlmere V0.1

Proprietary commercial platform foundation. Local demo only: no verified businesses,
accounts, customer registration, persistent database, tracking or payments.
No license to redistribute this proprietary source is granted.

Open this directory (the Git root) in VS Code. The original versioned source folder
is retained to avoid an unnecessary directory migration.

## Prerequisites and commands

- .NET SDK 10.0.403 (global.json permits later patches within that SDK feature band).
- A trusted ASP.NET development HTTPS certificate.
- VS Code C# extension for F5 debugging; C# Dev Kit is recommended.

From this directory:

```powershell
dotnet dev-certs https --check --trust
# Only if a trusted certificate is missing:
dotnet dev-certs https --trust
dotnet restore HappyAlmere.slnx
dotnet build HappyAlmere.slnx --configuration Release --no-restore
dotnet test HappyAlmere.slnx --configuration Release --no-build --logger trx
dotnet run --project HappyAlmere_V0.1_MVC/src/HappyCity.Web --configuration Release --no-build --launch-profile https
```

Visit https://localhost:7143. HTTP port 5143 redirects to HTTPS.
Stop the running app before rebuilding its configuration on Windows to avoid DLL locks.
If restricted MSBuild worker processes fail, run build/test with `-m:1` and build
with `--disable-build-servers`; this does not change compiler or security settings.
Certificate private-key access and browser launches may require execution outside
an agent sandbox. Do not bypass TLS certificate validation.

VS Code: Ctrl+Shift+B builds the solution; F5 launches the HTTPS debugger.
The tasks menu also provides tests and an HTTPS run task.

## Verification

The solution contains application and full MVC pipeline tests using xUnit and
Microsoft.AspNetCore.Mvc.Testing. Runtime projects have no additional package dependencies.
The root GitHub workflow restores, builds and tests Release.

Browser regression tooling is a development-only dependency. On Windows, install
Node 24.22.3 (pinned in `.node-version`; Node 25 is also supported locally), then:

```powershell
npm ci --ignore-scripts
npm run browser:install
npm audit --audit-level=high
# Stop any manually started test server first; Release must be built.
npm run test:browser
```

The harness starts and stops its own loopback-only servers, retains TLS certificate
validation, checks actual listeners and non-loopback connection rejection, and runs
42 responsive cases plus production security and rate-limit regressions. Results
and screenshots go to ignored `artifacts/`. No absolute Playwright path is needed.
The Windows browser CI job uses the same commands; the Linux .NET job remains.
See [development instructions](docs/DEVELOPMENT.md) for setup and CI limitations.

See [architecture](docs/ARCHITECTURE.md), [security review](docs/SECURITY.md), and
[V0.1 verification report](docs/V0.1-VERIFICATION.md).

## HA-001-R02 remediation checkout

This branch is based on V0.1 commit ebc99a0. The original develop branch already
contains separate V0.2 work and is intentionally untouched. Open this worktree root
(the directory containing HappyAlmere.slnx) in VS Code, not the nested source folder.
No PostgreSQL, Docker or customer-data setup is required for this V0.1 checkout.

Browser regression coverage now includes all seven required widths and accepted
long-category text, with screenshots and document/viewport measurements in artifacts/.
See [development instructions](docs/DEVELOPMENT.md), [engineering rules](AGENTS.md),
[security](SECURITY.md), [changelog](CHANGELOG.md),
[acceptance audit](docs/reports/HA-001-acceptance-audit.md) and
[R02 remediation report](docs/reports/HA-001-R02-independent-audit-remediation.md) and
[permanent risk register](docs/security/security-risk-register.md).
V0.1 acceptance remains subject to architect recommendation and product-owner approval.

Historical R01 continuation results: [verification ledger](docs/reports/evidence/HA-001-R01-refresh/README.md). Browser coverage includes
42 layouts across seven widths, including combined maximum-length query/category input.
