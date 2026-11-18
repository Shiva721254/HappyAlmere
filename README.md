# HappyAlmere V0.1

Proprietary commercial platform foundation. Local demo only: no verified businesses,
accounts, customer registration, persistent database, tracking or payments.
No license to redistribute this proprietary source is granted.

Open this directory (the Git root) in VS Code. The original versioned source folder
is retained to avoid an unnecessary directory migration.

## Git workflow

Private repository: https://github.com/Shiva721254/HappyAlmere

- `main` holds the verified baseline and reviewed stable changes.
- `develop` is the working branch for ongoing development.
- Build and test changes before pushing. Promote verified work from `develop`
  to `main` through a reviewed pull request.
- Do not commit secrets, customer data, or the confidential business-plan files.

This is the agreed workflow; GitHub branch protection is not yet configured.

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

For optional browser smoke tests, start the HTTPS app, then use an existing
Playwright/Chromium installation:

```text
node scripts/verify-browser.cjs <absolute-path-to-playwright-or-playwright-core>
```

The script keeps certificate validation enabled, checks five viewport widths,
navigation, keyboard skip access and search, and writes screenshots to ignored
`artifacts/`. Node/Playwright are QA tools, not application runtime requirements.
Browser verification is separate from CI's .NET integration tests.

See [architecture](docs/ARCHITECTURE.md), [security review](docs/SECURITY.md), and
[V0.1 verification report](docs/V0.1-VERIFICATION.md).
