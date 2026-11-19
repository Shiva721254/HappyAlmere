# HappyAlmere permanent security and engineering risk register

Last updated: 19 November 2026 (Europe/Amsterdam). Scope: V0.1, reviewed commit `7b6e082662fb5449cfae0a3586a91488db6ef880`, branch `fix/ha-001-r01`; remediation branch `fix/ha-001-r02`.

Authority: the owner's HA-001-R02 assignment and supplied **HA-001-ARCH-02 Independent Audit Findings Supplement**. SEC-F02?F06, PERF-F01?F02, UI-F01?F02, ARCH-F01 and GOV-F01 retain the supplied classifications, severity and deadlines. SEC-F01, QA-F01, QA-F02 and CODE-F01 were specified in the assignment without formal severity values; their provisional engineering priorities below are explicitly not attributed to the independent reviewer.

The owner now temporarily accepts documented residual risks for local synthetic-data development only under HA-001-CLOSE. V0.1's remote-CI condition is satisfied by run 44541406736 for the CI-corrected descendant a174aa7 (application/tests unchanged from cbe2def). Final foundation PR-head Ubuntu/Windows checks passed and PR #2 merged normally. This is not public deployment authorization or acceptance for production/customer data. Original per-finding R02 decisions below are historical; this limited owner decision supersedes their local acceptance-pending wording. Resolution milestones remain binding. Future requirements are not claims of currently exploitable defects or implemented capabilities. R02 stops before HA-002/HA-003. Responsible roles are assignments to roles, not claims that named people accepted them.

Source shorthand: `Web/`, `Application/`, `Infrastructure/` resolve under `HappyAlmere_V0.1_MVC/src/HappyCity.*`; `tests/` resolves under `HappyAlmere_V0.1_MVC/`. Other paths are repository-relative. R01 evidence is historical; fresh evidence and exact commands are in [the R02 report](../reports/HA-001-R02-independent-audit-remediation.md).

## SEC-F01 ? Development network isolation

- **Severity:** Not supplied by reviewer; Medium provisional engineering priority.
- **Classification:** Deployment-boundary verification requirement.
- **Current status:** Verified locally; independent closure pending.
- **Source evidence:** Web/Properties/launchSettings.json; .vscode/launch.json; scripts/verify-network.ps1; R02 network evidence.
- **Risk and scope:** Normal profiles must bind only to loopback. AllowedHosts and noindex cannot prevent network access. Command-line/environment overrides or forwarding could invalidate the local-only boundary.
- **Required resolution:** Keep normal launch URLs on localhost; verify actual process listeners and reject non-loopback interface connections. Do not publish, tunnel or forward the service.
- **Responsible milestone / deadline:** HA-001-R02; recheck before any deployment.
- **Verification requirements:** Inspect all owned listening sockets, test trusted HTTPS/HTTP redirect, and attempt connections to this machine through non-loopback interfaces. Record environment and limitations.
- **Responsible role:** Application engineer; security reviewer.
- **Risk acceptance decision:** Local synthetic use only; no public deployment approval.

## SEC-F02 ? Limited rate-limiting coverage

- **Severity:** Medium.
- **Classification:** Confirmed implementation limitation.
- **Current status:** Open; temporarily acceptable for a local synthetic-data prototype.
- **Source evidence:** Web/Program.cs; Web/Controllers/HomeController.cs; tests/HappyCity.FoundationTests/WebTests.cs; R01 reports and continuation ledger.
- **Risk and scope:** Only discovery has the 120/minute fixed-window search policy. Other routes/static assets are outside it. Process-local limits allow shared-IP interference, boundary bursts and distributed abuse.
- **Required resolution:** Design endpoint-sensitive layered limits, global resource/concurrency protection and trusted proxy handling. Evaluate distributed limiting for multiple instances and dedicated identity-operation limits. Preserve existing limiter in R02.
- **Responsible milestone / deadline:** Before public deployment.
- **Verification requirements:** Test sustained/concurrent/burst traffic, multiple clients, trusted proxies and fair legitimate access.
- **Responsible role:** Security architect; application/platform engineers.
- **Risk acceptance decision:** Reviewer describes temporary local synthetic acceptability only; no blanket owner risk acceptance.

## SEC-F03 ? Search data privacy and URL exposure

- **Severity:** Medium.
- **Classification:** Confirmed data-handling limitation with conditional privacy risk.
- **Current status:** Open.
- **Source evidence:** Web/Views/Home/Index.cshtml; Web/Views/Home/Privacy.cshtml; Web/Controllers/HomeController.cs; Web/appsettings.json.
- **Risk and scope:** GET q/category may enter URLs, browser history and infrastructure logs. No evidence of deliberate persisted customer search history. Warning and no-referrer reduce but do not eliminate accidental personal input.
- **Required resolution:** Define logging/retention, inspect actual hosting/proxy/telemetry, minimize or redact raw terms, review caching and provide protected sensitive enquiry flows. Ordinary nonsensitive search may retain GET. Preserve warnings in R02.
- **Responsible milestone / deadline:** Before public usage.
- **Verification requirements:** Trace synthetic identifiers through application/proxy logs, telemetry, analytics and storage; verify retention/redaction.
- **Responsible role:** Privacy owner; security/platform engineers.
- **Risk acceptance decision:** Pending; synthetic local input only.

## SEC-F04 ? Exception-handler interaction with future POST operations

- **Severity:** Medium.
- **Classification:** Potential future integration risk; not a reproduced V0.1 vulnerability.
- **Current status:** Deferred.
- **Source evidence:** Web/Program.cs; Web/Controllers/HomeController.cs; Web/Views/Home/Error.cshtml; tests/HappyCity.FoundationTests/WebTests.cs.
- **Risk and scope:** Exception re-execution can retain the original method. GET exception tests do not establish future POST routing/antiforgery behaviour or transactional rollback.
- **Required resolution:** Test valid/invalid POST exceptions, sanitized errors, no repeated side effects and rollback when data-changing features exist. Preserve antiforgery and current handler in R02.
- **Responsible milestone / deadline:** HA-003, before account-related POST release.
- **Verification requirements:** Authenticated/unauthenticated state-changing exception tests, including invalid tokens and transaction rollback.
- **Responsible role:** Identity/application engineer; security reviewer.
- **Risk acceptance decision:** Deferred requirement; no current exploit or acceptance inferred.

## SEC-F05 ? Secrets management and credential protection

- **Severity:** Medium.
- **Classification:** Future infrastructure security requirement.
- **Current status:** Deferred.
- **Source evidence:** .gitignore; AGENTS.md; SECURITY.md; Web/appsettings.json; R01 and R02 static scan evidence.
- **Risk and scope:** Ignore rules cannot prevent credentials in tracked C#/JSON/Markdown. Current V0.1 requires no database credentials; bounded scans are not proof of absence.
- **Required resolution:** Use protected development configuration/User Secrets and production secret storage; separate environments, least privilege, rotation/response, repeatable scans and sanitized templates. Permit a safe .env.example only if required. No real credentials or database work in R02.
- **Responsible milestone / deadline:** HA-002, before persistent database credentials.
- **Verification requirements:** Use synthetic credentials to test exclusion/scanner behaviour; review tracked source, templates, logs and reports for real secrets.
- **Responsible role:** Security/platform engineer; repository maintainer.
- **Risk acceptance decision:** Deferred; current credential protections retained, not a production secret-management implementation.

## SEC-F06 ? ASP.NET Core Data Protection key management

- **Severity:** Medium.
- **Classification:** Future identity and cryptographic infrastructure requirement.
- **Current status:** Deferred.
- **Source evidence:** Web/Program.cs; tests/HappyCity.FoundationTests/WebTests.cs; existing antiforgery results.
- **Risk and scope:** No persistent accounts or dedicated application key configuration exists. Framework-managed keys are not thereby proven insecure. Future restarts/multiple instances need deliberate key lifecycle and isolation.
- **Required resolution:** Define environment-specific storage, authorized identities, at-rest encryption evaluation, rotation/recovery and application isolation. Share keys only when needed; never commit them. No Identity/key infrastructure in R02.
- **Responsible milestone / deadline:** HA-003, before persistent authentication.
- **Verification requirements:** Verify tokens/authentication across controlled restarts, rotation and multiple instances when applicable.
- **Responsible role:** Identity/security architect; platform engineer.
- **Risk acceptance decision:** Deferred requirement; no finding of insecure current keys.

## QA-F01 ? Reproducible browser testing

- **Severity:** Not supplied by reviewer; Medium provisional engineering priority.
- **Classification:** Confirmed test reproducibility deficiency.
- **Current status:** Remediated locally; independent closure pending.
- **Source evidence:** scripts/verify-browser.cjs at reviewed commit; README.md; docs/DEVELOPMENT.md; package.json and package-lock.json in R02.
- **Risk and scope:** The submitted standard workflow depended on a machine-specific absolute Playwright installation path.
- **Required resolution:** Pin a development-only browser dependency and lockfile, document installation, retain responsive/security coverage and trusted TLS, audit dependencies.
- **Responsible milestone / deadline:** HA-001-R02.
- **Verification requirements:** Clean npm ci, pinned Chromium install, all 42 layouts and existing security/rate probes; direct/transitive npm audit.
- **Responsible role:** Test/application engineer.
- **Risk acceptance decision:** No residual risk acceptance implied; browser matrix limitations tracked in UI-F02.

## QA-F02 ? GitHub CI verification

- **Severity:** Not supplied by reviewer; Medium provisional engineering priority.
- **Classification:** Missing remote execution evidence and browser CI coverage.
- **Current status:** Submitted commit verified; revised browser workflow awaits authorized push/run.
- **Source evidence:** .github/workflows/build.yml; submitted run 44515481629; R02 report and submitted-ci.json.
- **Risk and scope:** Old reports did not establish remote CI. A locally passing revised workflow is not a GitHub result.
- **Required resolution:** Verify exact submitted SHA, preserve least permissions and immutable action pins, add reproducible trusted-HTTPS browser CI where practical and record actual new results after push authorization.
- **Responsible milestone / deadline:** HA-001-R02; revised remote run after authorized push.
- **Verification requirements:** Check workflow head SHA, jobs/steps/conclusions and browser artifacts. Never use old-run success as new-code proof.
- **Responsible role:** Repository maintainer; independent reviewer.
- **Risk acceptance decision:** Pending remote execution of R02; no push/merge authorization implied.

## PERF-F01 ? Representative database performance remains unverified

- **Severity:** Medium.
- **Classification:** Future performance verification requirement.
- **Current status:** Deferred.
- **Source evidence:** Infrastructure/DemoDiscoveryRepository.cs; Application/DiscoveryService.cs; Web/Controllers/HomeController.cs; docs/reports/evidence/HA-001-R01-refresh/README.md.
- **Risk and scope:** Six in-memory records and sequential localhost timing do not establish PostgreSQL scalability or latency. Async contracts/cancellation are foundations, not database implementation.
- **Required resolution:** Database-side filtering/sorting, bounded pagination, projections/no-tracking, cancellation, indexes, measured SQL and realistic synthetic datasets with latency monitoring. Do not implement PostgreSQL in R02.
- **Responsible milestone / deadline:** HA-002.
- **Verification requirements:** PostgreSQL integration/performance tests with documented datasets, infrastructure and workloads.
- **Responsible role:** Database/application engineer; architect.
- **Risk acceptance decision:** Deferred; no database benchmark or SLA claim.

## PERF-F02 ? Production concurrency and load capacity are unknown

- **Severity:** Medium.
- **Classification:** Missing performance verification evidence.
- **Current status:** Open.
- **Source evidence:** docs/reports/HA-001-R01-remediation-report.md; docs/reports/evidence/HA-001-R01-refresh/README.md; Infrastructure/DemoDiscoveryRepository.cs.
- **Risk and scope:** Existing warmed localhost sequential samples do not establish production throughput, concurrent users or resource limits.
- **Required resolution:** Define workloads/thresholds for sustained/spike/concurrent traffic, database contention, limiter behaviour, CPU/memory/pools, errors and recovery. No disruptive public testing in R02.
- **Responsible milestone / deadline:** Before public deployment.
- **Verification requirements:** Isolated production-like staging with synthetic data; record throughput, p50/p95/p99, resource use, failures and recovery.
- **Responsible role:** Performance/platform engineer; product owner for thresholds.
- **Risk acceptance decision:** Pending; no production capacity claim. Correct authoritative report path used; audit typo not propagated.

## UI-F01 ? Comprehensive accessibility verification outstanding

- **Severity:** Medium.
- **Classification:** Missing verification evidence.
- **Current status:** Open.
- **Source evidence:** Web/Views/Shared/_Layout.cshtml; Web/Views/Home/Index.cshtml; Web/wwwroot/css/site.css; scripts/verify-browser.cjs; R01 continuation ledger.
- **Risk and scope:** Semantic Dutch HTML, labels, visible focus and skip-link tests are positive but do not establish contrast, screen-reader, focus-order or magnification conformance.
- **Required resolution:** Proposed WCAG 2.2 AA engineering target subject to applicable requirements: contrast, keyboard, focus, announcements, names/descriptions, text resize/zoom/reflow, errors, target sizes and headings. Preserve existing controls in R02.
- **Responsible milestone / deadline:** Before public commercial release.
- **Verification requirements:** Automated checks plus manual keyboard/screen-reader testing; record actual violations and fixes. Automation alone cannot establish full conformance.
- **Responsible role:** Accessibility/UX specialist; frontend engineer.
- **Risk acceptance decision:** Pending; no WCAG certification claimed.

## UI-F02 ? Cross-browser and physical-device testing outstanding

- **Severity:** Medium.
- **Classification:** Missing compatibility verification evidence.
- **Current status:** Open.
- **Source evidence:** scripts/verify-browser.cjs; R01/R02 responsive evidence.
- **Risk and scope:** Chromium CSS viewport tests do not certify Firefox, WebKit/Safari, physical mobile keyboards, touch or device-specific accessibility.
- **Required resolution:** Define desktop Chromium, Firefox, Safari/WebKit, Android, iOS and tablet compatibility matrix with touch/keyboard interaction. R02 establishes reproducibility; broader device testing remains scheduled.
- **Responsible milestone / deadline:** Before public commercial release.
- **Verification requirements:** Run representative navigation/search/rendering/accessibility scenarios; record versions and distinguish emulation from physical devices.
- **Responsible role:** QA/UX engineer; product owner for supported matrix.
- **Risk acceptance decision:** Pending; seven Chromium widths are not physical-device acceptance.

## CODE-F01 ? Source maintainability

- **Severity:** Not supplied by reviewer; Low provisional engineering priority.
- **Classification:** Confirmed compressed source readability limitation.
- **Current status:** Remediated locally; independent closure pending.
- **Source evidence:** Web/Views/Home/*.cshtml; Web/Views/Shared/*.cshtml; Web/wwwroot/css/site.css at reviewed commit.
- **Risk and scope:** Compressed Razor/HTML/CSS makes review harder; no architectural rewrite is required.
- **Required resolution:** Format source while preserving content, selectors, variables, shared components, MVC boundaries and visual behaviour.
- **Responsible milestone / deadline:** HA-001-R02.
- **Verification requirements:** Whitespace-only source comparison, fresh builds/integration/browser tests and baseline screenshot comparison.
- **Responsible role:** Application/frontend engineer.
- **Risk acceptance decision:** No functional refactor or architecture change approved.

## ARCH-F01 ? Multi-city functionality is not yet implemented

- **Severity:** Planned architectural requirement; not a current security defect.
- **Classification:** Future functionality and architectural readiness.
- **Current status:** Deferred.
- **Source evidence:** Infrastructure/DemoDiscoveryRepository.cs; Web/Views/Home/Index.cshtml; Web/Views/Shared/_Layout.cshtml; Web/appsettings.json; AGENTS.md; proposed HA-002 architecture (not supplied or independently verified here).
- **Risk and scope:** Layered boundaries support extension, but Almere strings/demo data remain and no city resolver/table/relationships exist. Naming is not implemented isolation.
- **Required resolution:** Shared city entities, trusted domain-to-city resolution, city-aware catalog, invalid cross-city reference prevention and separate private authorization; reuse business rules. Preserve MVC; no early implementation.
- **Responsible milestone / deadline:** HA-002 for database/catalog foundations.
- **Verification requirements:** Add cities without duplicated architecture; test city queries/invalid references and unauthorized modifications when authorization exists.
- **Responsible role:** Architect; domain/database engineer.
- **Risk acceptance decision:** Architecture accepted in principle; future functionality remains unimplemented.

## GOV-F01 ? Existing V0.2 development sequencing exception

- **Severity:** Medium.
- **Classification:** Confirmed governance/integration concern reported by implementation audit.
- **Current status:** Open.
- **Source evidence:** R01 remediation and acceptance reports; audit supplement historical GitHub comparison; reported develop c2964ee.
- **Risk and scope:** Reported develop c2964ee predates formal V0.1 acceptance and has not been fully independently audited. Integrating without reconciliation may lose security/layout corrections or bypass gates. Historical comparison: reviewed branch three ahead/zero behind main; not a fresh comparison claim.
- **Required resolution:** Preserve history, obtain exact-version formal V0.1 approval, use authorized integration workflow, inspect V0.2 against HA-002, resolve conflicts, regress and document exception. No merge/reset/force-push/develop edits in R02.
- **Responsible milestone / deadline:** Before resuming HA-002.
- **Verification requirements:** Record authorized branch comparisons/SHAs, integration results, regression checks and deviations.
- **Responsible role:** Product owner; independent architect; repository maintainer.
- **Risk acceptance decision:** Open. Architecture/remediation direction approval is not final V0.1 approval or blanket residual-risk acceptance.

## HA-001-CLOSE acceptance boundary

The owner explicitly accepts the documented residual risks only during local synthetic-data development. Future controls remain unimplemented requirements with their original deadlines. Successful Ubuntu and Windows CI is recorded in the closure report; local-scope V0.1 acceptance is now recorded. GOV-F01 reconciliation is authorized on a dedicated branch; develop and its original PostgreSQL history must remain recoverable. Independent review of the HA-002 gap analysis precedes additional database features.

## Reconciliation disposition

GOV-F01 sequencing exception is documented: owner-authorized foundation merge through
PR #2 completed after checks; original develop c2964ee is preserved and integration
has its own branch. Integration/gap-analysis acceptance remains subject to independent
review, not automatic approval of existing V0.2 capabilities. ARCH-F01/PERF-F01/SEC-F05
master gaps remain tracked; no production or private-data authorization is inferred.

Integration local verification: 68 tests, 42 layouts, 190 security and five rate
assertions passed against PostgreSQL; loopback-only HTTP/HTTPS and database port
bindings were verified. The NUL-input compatibility regression is resolved by bounded
validation (HTTP 400), without relaxing TLS or other controls. Draft PR #3 and the
HA-002 gap analysis await independent review. This does not resolve deferred catalog,
secrets lifecycle, backup/restore or production-access requirements.


## HA-002-A recovery/design evidence

Local synthetic backup/restore is now demonstrated; [the implementation report](../reports/HA-002-implementation-report.md)
records scope, hashes, exact reconciliation, tests and open issue criteria. Archives and
generated credentials are protected/ignored, never uploaded. Host disk encryption,
production backup protection and separated backup/migration/owner roles are not claimed.
SEC-F05/ARCH-F01/PERF-F01 remain open for their residual requirements. A successful
baseline restore does not demonstrate UUID migration or recovery after its failure.


## Governance provenance and coordination blockers (HA-GOV-001)

This existing register is the canonical KNOWN_ISSUES equivalent; no competing root
risk file is created. Entries below are documentation/governance observations, not
new independently classified security vulnerabilities or architecture changes.

| ID | Blocker / impact | Owner role | Status and next action | Evidence |
|---|---|---|---|---|
| GOV-TRC-001 | Standalone HA-002-ARCH-01 unavailable; original detailed findings cannot be authenticated | Product owner and independent reviewer | Open provenance gap; provide a verified original reference if recovered, otherwise retain explicit reconstructed status; do not fabricate text or block already authorized recovery/design solely on its absence | [Issue #4 clarification](https://github.com/Shiva721254/HappyAlmere/issues/4#issuecomment-7013554143), [decision index](../../DECISIONS.md) |
| GOV-STATE-001 | Issue #5 already closed/completed before implementation evidence; UI could be mistaken for acceptance | Product owner | State discrepancy open; reconcile issue state with actual documentation review/approval. Codex does not reopen/close or infer approval | [Issue #5](https://github.com/Shiva721254/HappyAlmere/issues/5), [status](../../PROJECT_STATUS.md) |
| GOV-PLAN-001 | Proposed milestone numbering may duplicate/defer existing requirements, especially multi-city HA-006 versus HA-002 | Product owner and independent reviewer | Unapproved proposal; confirm roadmap IDs/dependencies without overwriting master requirements | [Roadmap](../../ROADMAP.md) |
| GOV-COORD-001 | Governance and primary work share documentation paths; careless integration could overwrite newer HA-002-A evidence | Implementer/repository maintainer, owner for merge approval | Separate branch based on fixed 3287994; no concurrent primary edits; review overlapping README/HANDOVER/AGENTS/CHANGELOG/register hunks before any future authorized integration | [Task ownership](../../TASKS.md) |

Primary technical blockers and their impact/roles remain in the existing findings and
HA-002-A implementation matrix. Governance indexes do not close UUID/catalog/role/
pagination/recovery-failure criteria or grant acceptance. Keep HA-002-A primary.
