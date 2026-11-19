# V0.1 development and verification

Open the worktree root containing HappyAlmere.slnx in VS Code. Prerequisites:
.NET SDK selected by global.json, trusted local HTTPS development certificate,
C# extension for debugging; existing Playwright/Chromium for optional browser QA.

```powershell
dotnet --info
dotnet dev-certs https --check --trust
dotnet restore HappyAlmere.slnx --force --no-http-cache
dotnet build HappyAlmere.slnx -c Release --no-restore -m:1 --disable-build-servers
dotnet build HappyAlmere.slnx -c Debug --no-restore -m:1 --disable-build-servers
dotnet test HappyAlmere.slnx -c Release --no-build -m:1 --logger trx
dotnet list HappyAlmere.slnx package --vulnerable --include-transitive --format json
dotnet run --project HappyAlmere_V0.1_MVC/src/HappyCity.Web -c Release --no-build --launch-profile https
# In a second terminal, from the same root:
node scripts/verify-browser.cjs <absolute-path-to-installed-playwright-or-playwright-core>
```

Visit https://localhost:7143; http://localhost:5143 redirects. If no trusted
certificate exists, explicitly create/trust one using dotnet dev-certs https --trust;
never suppress verification. Stop the owned server before rebuilding on Windows.
For production error/HSTS checks use ASPNETCORE_ENVIRONMENT=Production and
--no-launch-profile with explicit HTTPS/HTTP URLs. HSTS excludes localhost by design.
No real customer information is permitted in test input or artifacts.

Ctrl+Shift+B runs the build task. F5 selects HappyAlmere HTTPS. Set a breakpoint in
HomeController.Index, browse a search URL, verify breakpoint/locals/continue and stop
the debugger. Record the actual result, extension versions and evidence; configuration
validation alone does not prove a successful interactive debugger session.

Browser QA uses a real validated TLS connection. Capture all required widths and
long-category cases; native navigation remains visible and wraps, with no scripted
hamburger menu to exercise. Avoid running many discovery probes against one peer
without resetting/waiting for its 120/minute rate window; never raise limits to pass QA.
Raw evidence belongs in ignored artifacts/; publish only sanitized reviewed records.

Fresh continuation results: [verification ledger](reports/evidence/HA-001-R01-refresh/README.md). Browser coverage includes
42 layouts across seven widths, including combined maximum-length query/category input.
