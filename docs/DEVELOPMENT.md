# V0.1 development and verification

Open the Git worktree root containing HappyAlmere.slnx. Use the .NET SDK selected
by global.json and the C# extension for debugging. Python 3 is used for the standard-library-only source/credential scan. Browser tooling is development-only:
Windows 10/11 or Windows Server with Windows PowerShell 5.1+, Node 24.22.3 as pinned
in `.node-version` (Node 25 also supported locally), npm and the committed lockfile.
The browser harness intentionally targets Windows trust stores and socket inventory;
Linux .NET integration tests remain supported, but Linux browser trust setup is not
claimed by this workflow. Node/Playwright are not deployed with the application.

## Repeatable setup and checks

Run from the root. On first setup, explicitly create/trust the local certificate:

```powershell
dotnet dev-certs https --trust
dotnet dev-certs https --check --trust
dotnet restore HappyAlmere.slnx --force --no-http-cache
dotnet build HappyAlmere.slnx -c Debug --no-restore -m:1 --disable-build-servers
dotnet build HappyAlmere.slnx -c Release --no-restore -m:1 --disable-build-servers
dotnet test HappyAlmere.slnx -c Release --no-build -m:1 --logger trx
dotnet list HappyAlmere.slnx package --vulnerable --include-transitive --format json
npm ci --ignore-scripts
npm run browser:install
npm audit --audit-level=high
python scripts/verify-source.py
npm run test:browser
```

`npm ci` installs exactly the lockfile's development dependencies. Playwright 1.65.1
pins its Chromium revision; `browser:install` downloads that matching browser.
No global npm module or machine-specific path is needed. Package lifecycle scripts
are disabled during install; browser installation is a separate explicit operation.
Review the lockfile, dependency audit and browser changes when upgrading.

The browser harness checks certificate trust, exports only the public development
certificate to ignored artifacts, and passes it to Node via NODE_EXTRA_CA_CERTS.
It never exports the private key and never sets ignoreHTTPSErrors or disables TLS
validation. Chromium uses Windows certificate trust. Agent sandboxes may require
approved certificate/process/socket access; do not weaken TLS to avoid that boundary.

The harness starts the actual `https` launch profile for responsive checks on
7143/5143. Fresh Production servers on 7243/5243 separately run functional/security
and rate-budget probes. Readiness uses TCP so it does not consume discovery permits.
Existing listeners cause failure; only harness-owned processes are terminated.
All three suites verify loopback sockets and attempt connections through local
non-loopback IPv4 interfaces. IPv6 listeners are inventoried and must be loopback;
non-loopback IPv6 reachability is not separately probed. At least one non-loopback
IPv4 interface is required to claim the boundary probe. No external host is contacted.
Never raise the 120/minute limit to pass tests.

For a manually started development server:

```powershell
dotnet run --project HappyAlmere_V0.1_MVC/src/HappyCity.Web -c Release --no-build --launch-profile https
# In another terminal:
npm run test:responsive
powershell.exe -NoProfile -File scripts/verify-network.ps1
```

Stop the server before rebuilding on Windows. Browse https://localhost:7143;
http://localhost:5143 redirects. `localhost` binds loopback IPv4/IPv6, not LAN/WAN.
Never override URLs with wildcard/all-interface addresses, add port forwarding or
create a public tunnel. AllowedHosts validates HTTP Host and noindex asks crawlers
not to index; neither is a network access control. Local processes/users can still
reach loopback. Use synthetic input only. See [security](SECURITY.md).

## CI and evidence

The root workflow retains the Ubuntu .NET job and adds a Windows 2025 browser job.
Actions are pinned by full commit SHA; token permissions are contents:read, checkout
credentials are not persisted, and package-manager caching is disabled. The browser
job uses the same lockfile, pinned Node version, Chromium installer and harness.
A trusted certificate is generated on the ephemeral runner; no repository secret is
needed. Artifact selection includes results/screenshots only, excluding certificates,
keys and server logs. Retention is seven days. Hosted runner images and the OS trust
store are external inputs, not bit-for-bit frozen infrastructure.

Changing a workflow locally does not establish its remote result. The R02 report
separates the submitted R01 commit's actual GitHub success from the revised R02 job,
which can run only after an authorized push. No push or merge is implied by testing.

Raw evidence is ignored under artifacts/. Publish only reviewed sanitized records.
The suite retains seven viewport widths, 42 layouts, navigation/search, boundary
inputs, skip-link controls, 190 production assertions and five live rate checks.
Browser tests are not full accessibility, physical-device, load or penetration tests.

## Debugging and acceptance

Ctrl+Shift+B builds; F5 selects HappyAlmere HTTPS with the same localhost URLs.
Set a HomeController.Index breakpoint, browse a search, inspect locals/continue and
stop. Record actual debugger evidence separately; configuration inspection alone is
not a debugger execution test. HSTS is tested with a non-loopback test host in the
integration suite because localhost is intentionally excluded by the framework.

See [R02 report](reports/HA-001-R02-independent-audit-remediation.md) and
[risk register](security/security-risk-register.md). Stop after R02 pending independent
review and formal owner approval; do not start HA-002/HA-003.

Reference: [Playwright CI setup](https://playwright.dev/docs/ci) and
[.NET certificate export semantics](https://learn.microsoft.com/dotnet/core/tools/dotnet-dev-certs).
