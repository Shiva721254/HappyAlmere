# HappyAlmere V0.2 — database-backed discovery

Proprietary commercial platform foundation. Local synthetic-data prototype only:
no real businesses, customer accounts, registration, tracking or payments.
No license to redistribute this proprietary source is granted.

The application uses ASP.NET Core MVC/Razor on .NET 10, PostgreSQL and EF Core.
Open this Git-root directory in VS Code. The original source folder name is
retained to avoid an unnecessary directory migration.

## Git workflow

Private repository: https://github.com/Shiva721254/HappyAlmere

- `main` holds the verified baseline and reviewed stable changes.
- `develop` is the working branch for ongoing development.
- Build and test before pushing. Promote verified work through a pull request.
- Main requires an up-to-date branch, passing `verify` CI and resolved conversations.
  Admin enforcement is enabled; force pushes and deletion are blocked.
- No independent approval count is configured until a separate reviewer exists.
- Never commit secrets, customer information or the confidential business plans.

## Run locally

Prerequisites: .NET SDK 10.0.403, Docker with Linux containers, PowerShell,
and a trusted ASP.NET HTTPS development certificate.

From the repository root:

```powershell
dotnet dev-certs https --check --trust
# Only if a trusted certificate is missing:
dotnet dev-certs https --trust
dotnet restore HappyAlmere.slnx
./scripts/Initialize-LocalDatabase.ps1
dotnet build HappyAlmere.slnx --configuration Release --no-restore
./scripts/Test-Local.ps1
./scripts/Start-Local.ps1
```

Visit https://localhost:7143. HTTP port 5143 redirects to HTTPS.
Bootstrap generates ignored local credentials, verified database TLS and a
read-only runtime connection, then applies the synthetic-data migration.
The web process never runs migrations. Re-running initialization preserves data.

VS Code: Ctrl+Shift+B builds; F5 starts the HTTPS debugger using the generated
`.env.runtime`. The tasks menu includes tests and the HTTPS run script.
The C# extension is required for debugging; C# Dev Kit is recommended.

Stop the running app before rebuilding on Windows. In restricted agent
environments, build with `-m:1 --disable-build-servers` if MSBuild workers fail.
Docker, certificate private-key access and browser launches may require approved
execution outside the agent sandbox; never bypass TLS validation.

## Verification

`Test-Local.ps1` runs all 63 foundation and actual PostgreSQL integration tests.
Missing database configuration is a failure, not a skipped integration suite.
CI provisions an isolated database using the same scripts.

For optional browser checks, start the HTTPS app and use an existing
Playwright/Chromium installation:

```text
node scripts/verify-browser.cjs <absolute-path-to-playwright-or-playwright-core>
```

The script validates trusted HTTPS, five viewport widths, navigation and search.
Screenshots are written to ignored `artifacts/`. Node/Playwright are optional QA
tools, not application runtime dependencies.

See [V0.2 architecture, security and verification](docs/V0.2-DATABASE-DISCOVERY.md).
Historical records: [V0.1 architecture](docs/ARCHITECTURE.md),
[V0.1 security](docs/SECURITY.md), [V0.1 verification](docs/V0.1-VERIFICATION.md).

Real customer data and public production deployment remain blocked until identity,
authorization, database operations and privacy controls are implemented and tested.
