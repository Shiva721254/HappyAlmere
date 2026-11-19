# HA-001-R02 ? Independent audit remediation

Date: 19 November 2026 (Europe/Amsterdam). Branch: `fix/ha-001-r02`.
Reviewed/recovery base: `7b6e082662fb5449cfae0a3586a91488db6ef880` on `fix/ha-001-r01`.
Remote: `https://github.com/Shiva721254/HappyAlmere.git` (fetch and push).

The owner's HA-001-R02 assignment and HA-001-ARCH-02 findings supplement authorize
this scoped continuation. The layered ASP.NET Core MVC architecture is accepted in
principle; no rewrite, HA-002/HA-003 work, customer data, merge or public deployment
was introduced. `develop` and `main` were not changed. Their verified local refs remain
`c2964eeff4232dd1f925c63af06f8c97c751b161` and
`ebc99a0d9bf496821fc497222ac216388b700846`, respectively. The existing clean R01 commit
was the recoverable checkpoint before edits; R02 uses its own scoped branch.

## Outcome and acceptance

Local technical remediation is complete and ready for independent review. Debug and
Release build with **zero warnings/errors**. **33 tests pass**, **42 responsive cases
pass at seven widths**, **190 production security assertions and five live rate
assertions pass**. All **21 pre/post-formatting screenshots are byte-identical**.
All three browser suites verify loopback-only listeners and reject six connection
attempts through three local non-loopback IPv4 interfaces. npm and NuGet scans
report no known vulnerabilities; bounded current/history credential scans find none.

Acceptance remains **12 PASS, 2 PARTIAL, 0 FAIL, 1 NOT TESTED**. AC13 awaits review of
this new R02 version, AC14 requires the documented risk decisions and AC15 requires
formal product-owner V0.1 approval. Acceptance in principle of architecture is not
blanket residual-risk acceptance or final acceptance. QA-F02's original submitted
commit execution is verified; the revised R02 remote browser job remains **NOT RUN**
until an authorized push. No old GitHub success is attributed to unpushed changes.

The exact final commit SHA is reported in the handoff after committing; it cannot be
embedded in its own content. In this checkout retrieve the report's containing commit
with `git log -1 --format=%H -- docs/reports/HA-001-R02-independent-audit-remediation.md`.
The [source manifest](evidence/HA-001-R02/verified-source-sha256.json) identifies the
verified executable/configuration/test bytes independently of that commit identifier.

## Finding dispositions

| Finding | R02 action and actual result | Status / boundary |
|---|---|---|
| SEC-F01 | Confirm normal launch and VS Code localhost URLs; add configuration and actual socket checks; reject non-loopback local connections; clarify Host/noindex limitations. | Locally verified; independent closure pending. |
| QA-F01 | Add private development-only Playwright 1.65.1, lockfile, Node pin and standard npm commands. Preserve trusted TLS and all regression assertions. | Locally reproducible and passing. Windows harness supported; other OS browser trust not claimed. |
| QA-F02 | Inspect exact submitted workflow blob and completed push run; add Windows browser CI, read-only token, nonpersistent checkout credentials and full SHA pins. | R01 run PASS; new R02 workflow awaits authorized push and actual remote execution. |
| CODE-F01 | Format seven Razor files and CSS; format launch/appsettings JSON without semantic changes. | Non-whitespace Razor/CSS content identical; all 21 screenshot bytes identical; regressions pass. |
| SEC-F02?SEC-F06, PERF-F01?PERF-F02, UI-F01?UI-F02, ARCH-F01, GOV-F01 | Preserve authoritative severity/classification, scope, evidence, milestones, verification requirements, responsible role and acceptance decisions in the permanent register. | Open/deferred as directed; no premature database, Identity, production load or multi-city implementation. |

All 15 entries are in [docs/security/security-risk-register.md](../security/security-risk-register.md).
SEC-F01/QA-F01/QA-F02/CODE-F01 formal severity was not supplied; provisional engineering
priorities are explicitly distinguished from the independent reviewer's classifications.

## Implementation decisions

- Preserve the existing MVC, services, repository contracts/implementation, domain and
  typed ViewModel. No .NET project reference or runtime dependency was added.
- Preserve the exact normal localhost bindings rather than changing working security
  semantics. AllowedHosts filters HTTP Host, not network peers; noindex is a crawler
  instruction. Neither provides network isolation. Local users/processes can reach
  loopback; public tunnels/forwarding and wildcard URL overrides remain prohibited.
- Use `playwright` rather than a new test framework because the existing assertion
  scripts already cover the required behaviour. Its sole package dependency is the
  identically pinned playwright-core. Both are development dependencies. Installation
  uses the npm integrity lockfile and disables lifecycle scripts.
- Match browser CI to the Windows local certificate trust and socket-inventory workflow.
  Keep the existing Ubuntu .NET job. Windows hosted images are not immutable OS builds;
  package/browser versions and actions are pinned. CI uses Node 24.22.3. The full local suite passed on both supported Node 25.6.0
  and the exact pinned Node 24.22.3; npm 11.8.0, SDK 10.0.403 and Chromium
  159.0.8154.6 were used. The Node-24 binary was downloaded from nodejs.org and
  verified against its published SHA-256 list before execution.
- Export only the public development certificate for Node trust; never export a private
  key, suppress TLS validation or use ignoreHTTPSErrors. The certificate remains ignored
  and is excluded from CI artifacts. CI creates/trusts its own ephemeral local cert.
- Each browser suite owns a fresh server so the production limiter remains 120/minute.
  The harness checks for existing listeners and terminates only its own process tree.
  Raw test output and public certificate remain under ignored artifacts; reviewed
  evidence excludes key/certificate files, raw TRX, server logs and private addresses.
- Keep all Dutch text, provenance/privacy notices, semantic markup, CSS selectors,
  variables and layout values. Formatting has no non-whitespace source differences.

## Exact command and result ledger

Commands ran from the repository root unless specified. Shell capture redirection and
Tee-Object only persist output; evidence is sanitized into `docs/reports/evidence/HA-001-R02`.

| Command / operation | Actual result |
|---|---|
| `git status --short --branch`; `git log -3 --oneline`; `git remote -v` | Clean R01 base; exact intended remote. |
| `git switch -c fix/ha-001-r02` | First sandbox shared-ref write denied; approved execution succeeded. No other branch reset or modification. |
| `gh run list --repo Shiva721254/HappyAlmere --commit 7b6e082662fb5449cfae0a3586a91488db6ef880 --json databaseId,headSha,status,conclusion,url,workflowName,event` | Exact-SHA push workflow completed successfully; run 44515481629. |
| `gh run view 44515481629 --repo Shiva721254/HappyAlmere --json conclusion,headSha,url,jobs`; same with `--log` | Success; restore/build/test steps all successful. JSON is packaged; raw logs reviewed locally. |
| `gh api 'repos/Shiva721254/HappyAlmere/contents/.github/workflows/build.yml?ref=7b6e082662fb5449cfae0a3586a91488db6ef880' --jq '{path: .path, sha: .sha, html_url: .html_url}'`; `git rev-parse 7b6e082:.github/workflows/build.yml` | Matching blob SHA `150e86b15dff440f95682f98ff529d453af546d7`; remote source and reviewed local source match. |
| `git ls-remote https://github.com/actions/setup-node.git refs/tags/v6`; `git ls-remote https://github.com/actions/upload-artifact.git refs/tags/v5` | Exact pinned action SHAs verified. Upload-artifact sandbox network failed once; approved read succeeded. |
| `npm view playwright@1.65.1 dependencies optionalDependencies engines --json`; `npm view node@24 version --json` | Exact Playwright/core 1.65.1 dependency and Node 24.22.3 availability inspected. |
| `npm install --package-lock-only --ignore-scripts` | Lockfile generated successfully; no application runtime package introduced. |
| `npm ci --ignore-scripts` (repeated from lockfile after setup) | Two packages installed; exit 0. |
| `npm run browser:install` | Matching Chromium/headless-shell revision 1255 installed successfully. |
| `npm audit --json`; `npm ls --all --json` | Zero vulnerabilities at every severity; direct Playwright and transitive playwright-core only. Root private package is not a production application dependency. |
| `dotnet restore HappyAlmere.slnx --force --no-http-cache` | Five projects restored; exit 0. |
| `dotnet build HappyAlmere.slnx -c Debug --no-restore -m:1 --disable-build-servers` | 0 warnings/errors, 8.74 seconds; exit 0. |
| Same build command with `-c Release` | 0 warnings/errors, 9.26 seconds; exit 0. |
| `dotnet test HappyAlmere.slnx -c Release --no-build -m:1 --logger trx --results-directory artifacts/r02/test-results` | 33 passed, 0 failed, 0 skipped; reported duration 1 second; exit 0. |
| `dotnet list HappyAlmere.slnx package --vulnerable --include-transitive --format json` | Five projects, no known vulnerable direct/transitive packages; exit 0. |
| `artifacts/r02/node-v24.22.3.exe scripts/run-browser-tests.cjs` after verified official binary download | Complete second run: exit 0, same 42/190/5 passing coverage and network boundary checks; `browser-node24.log`. |
| `npm run test:browser` | Final full run exit 0; 42 layouts, 190 security assertions, five rate assertions and three actual network inventories/probes pass. Earlier harness failures below are retained. |
| Harness: `dotnet dev-certs https --check --trust`; `dotnet dev-certs https --export-path <workspace>/artifacts/r02/browser/localhost-public.crt --format PEM` | Trusted certificate checked; public certificate only exported; exit 0. No password/no-password flag used. |
| Harness: `dotnet run --project HappyAlmere_V0.1_MVC/src/HappyCity.Web -c Release --no-build --launch-profile https` | Actual Development profile on 7143/5143, both IPv4/IPv6 loopback. |
| Harness: `ASPNETCORE_ENVIRONMENT=Production` then `dotnet run --project HappyAlmere_V0.1_MVC/src/HappyCity.Web -c Release --no-build --no-launch-profile -- --urls 'https://localhost:7243;http://localhost:5243'` | Separate Production servers for functional/rate tests; both IPv4/IPv6 loopback. Environment syntax set through Node child env, not shell interpolation. |
| Harness: `powershell.exe -NoProfile -File scripts/verify-network.ps1 -HttpsPort 7143 -HttpPort 5143 -Suite responsive`; ports 7243/5243 with suites functional and rate | Each suite: four loopback listener rows, three non-loopback local IPv4 interfaces, six rejected connections. No private interface address published. |
| Harness: `node scripts/verify-browser.cjs`; `node scripts/verify-security.cjs functional`; `node scripts/verify-security.cjs rate` | All pass with NODE_EXTRA_CA_CERTS pointing to the public dev cert and no TLS bypass. |
| `python scripts/verify-source.py` | Approved dependency graph, JSON, required docs and launch config pass; five reachable commits/290 text file versions plus current authored files scanned, no credential-pattern findings. Final packaged-file scan is recorded in static-results.json. |
| Python comparison of reviewed `git show 7b6e082:<path>` against current Razor/CSS, removing whitespace only; SHA-256/byte comparison of 21 before/after PNGs | Eight source files equivalent; 21/21 screenshots identical. Evidence JSON records each file. |
| `git diff --check` | Passed. Git checkout line-ending notices are not build warnings. |

Workflow syntax was manually inspected; PyYAML was unavailable, so no YAML-parser or
remote workflow success is claimed. Node syntax checks passed for all three browser
scripts. Both formatted JSON configuration objects compare equal to the reviewed base.

## Failures encountered and corrected

1. Initial pinned-browser baseline passed responsive checks but the Host-rejection
   probe failed TLS hostname validation: Playwright derived TLS identity from the
   deliberately overridden HTTP Host. The live probe now uses Node HTTPS with the
   actual TLS identity `localhost` and a separate invalid HTTP Host header. Chain and
   hostname validation remain enabled; HTTP 400 is asserted. Integration Host tests
   are also retained. `baseline-browser.log` records the failed attempt.
2. The first full post-formatting run passed responsive and all 190 security assertions
   but its next-suite port reservation failed EADDRINUSE after server shutdown on
   Windows. Replacing bind-based availability reservation with bounded TCP listener
   checks and waiting for owned-server shutdown resolved it. Existing listeners still
   cause failure and are never reused/killed. `browser-port-probe-failure.log` records
   this attempt; `browser-final.log` records the complete passing Node-25 run; `browser-node24.log`
   records the additional passing exact-version run.
3. An early Node release metadata query returned no usable version; npm's version
   metadata then verified the exact chosen Node version. This was tool discovery,
   not a passing test. Sandbox ref/network restrictions were resolved through approved
   execution, not by altering repository or application security.

## GitHub CI evidence

Submitted R01 SHA: `7b6e082662fb5449cfae0a3586a91488db6ef880`.
[Workflow run 44515481629](https://github.com/Shiva721254/HappyAlmere/actions/runs/44515481629)
completed **success**; [verify job](https://github.com/Shiva721254/HappyAlmere/actions/runs/44515481629/job/135530271908)
started 2026-11-19 11:30:51 UTC and completed 11:31:27 UTC. These UTC values are
provider timestamps (12:30:51?12:31:27 Europe/Amsterdam).

The revised `.github/workflows/build.yml` keeps `contents: read`, pins all four used
actions by complete SHA, disables persisted checkout credentials and adds a Windows
browser job with a 15-minute timeout and seven-day result/screenshot retention. No
secrets are added. The source scanner uses the history reachable in the checkout
(CI checkout is shallow). NuGet auditing remains enabled with warnings as errors;
npm audit gates high/critical findings. Actual R02 workflow result: **NOT RUN**;
requires user-approved push, then inspection of the exact new commit's run/artifacts.
This is the remaining QA-F02 verification step, not an invented success or approval.

## Remaining risks and unexecuted verification

The permanent register is authoritative for open/deferred findings. In particular:
limited process-local endpoint rate limiting; GET exposure through future infrastructure;
future POST exception interactions, credentials and Data Protection; unmeasured database
and production load; incomplete accessibility/browser/device coverage; unimplemented
multi-city support; and the pre-existing V0.2 sequencing exception remain tracked.
No future control is presented as currently implemented. SEC-F02's temporary local
synthetic acceptability is not blanket acceptance of the other findings.

No public TLS/deployment, external firewall, non-loopback IPv6 reachability, physical
device, Firefox/WebKit, comprehensive accessibility, production load, PostgreSQL,
Identity or new interactive debugger run was performed. Local non-loopback rejection
supports the configured loopback boundary, not a remote perimeter certification.
Performance probe numbers remain localhost observations only; no capacity/SLA claim.
OS/browser binaries and advisory databases are external inputs; dependency/secret scans
cannot establish absence of unknown vulnerabilities or all secrets.

Owned test servers were stopped. Raw TRX, baseline screenshots, public dev certificate
and server logs stay ignored. Published evidence uses workspace placeholders and
omits real network addresses; no private key or customer data is packaged. Prior
historical evidence remains unchanged. No push, merge, deployment or develop reset
was performed during R02. Stop pending independent review and product-owner approval.

## Changed files

The exact source/configuration/document inventory is listed below; evidence artifacts
are grouped under `docs/reports/evidence/HA-001-R02/` (JSON results, sanitized logs,
21 screenshots and manifests). All changes are R02-scoped.

```text
.github/workflows/build.yml
.node-version
AGENTS.md
CHANGELOG.md
HappyAlmere_V0.1_MVC/README.md
HappyAlmere_V0.1_MVC/src/HappyCity.Web/Properties/launchSettings.json
HappyAlmere_V0.1_MVC/src/HappyCity.Web/Views/Home/Business.cshtml
HappyAlmere_V0.1_MVC/src/HappyCity.Web/Views/Home/Error.cshtml
HappyAlmere_V0.1_MVC/src/HappyCity.Web/Views/Home/Index.cshtml
HappyAlmere_V0.1_MVC/src/HappyCity.Web/Views/Home/Privacy.cshtml
HappyAlmere_V0.1_MVC/src/HappyCity.Web/Views/Shared/_DiscoveryCard.cshtml
HappyAlmere_V0.1_MVC/src/HappyCity.Web/Views/Shared/_Layout.cshtml
HappyAlmere_V0.1_MVC/src/HappyCity.Web/Views/_ViewStart.cshtml
HappyAlmere_V0.1_MVC/src/HappyCity.Web/appsettings.json
HappyAlmere_V0.1_MVC/src/HappyCity.Web/wwwroot/css/site.css
README.md
SECURITY.md
docs/ARCHITECTURE.md
docs/DEVELOPMENT.md
docs/SECURITY.md
docs/V0.1-VERIFICATION.md
docs/reports/HA-001-R02-independent-audit-remediation.md
docs/reports/HA-001-acceptance-audit.md
docs/security/security-risk-register.md
package-lock.json
package.json
scripts/run-browser-tests.cjs
scripts/verify-browser.cjs
scripts/verify-network.ps1
scripts/verify-security.cjs
scripts/verify-source.py
```
