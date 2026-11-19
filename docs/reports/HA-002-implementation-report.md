# Current implementation ? HA-002-R01 remediation candidate

Authority: independently supplied [HA-002-ARCH-02 / Issue #10](https://github.com/Shiva721254/HappyAlmere/issues/10), owner-authorized bounded remediation, 19 November 2026. Baseline **d3d6a558cccda2d8e437854c46b5cbfe5ab7b9f4**, branch **feature/ha-002-a**, [draft PR #6](https://github.com/Shiva721254/HappyAlmere/pull/6). This section supersedes historical implementation/pending-design statements below. It records implementing-agent evidence, **not independent acceptance**. HA-002-ARCH-01 remains unavailable as a verified standalone reference.

## Finding ? correction ? verification

| Finding / task | Correction and evidence source | Disposition |
|---|---|---|
| F01 / R01-01 | Application ICityResolver, Infrastructure PostgresCityResolver, HomeController; RemediationTests trusted-host and actual HTTP tests | Implemented; explicit localhost/127.0.0.1 configuration; active synthetic .test domains only; unknown/inactive reject; primary/alias redirect ignores forwarded host and port; another city's results remain isolated. No real DNS activation. Existing Almere branding retained; not a complete multi-city presentation feature |
| F02 / R01-02 | verify-forward-migration.py execute/verify_role_controls | Exact SQLSTATE; exact controlled P0001 failpoint message and hierarchy reason; successful identity/TLS/read controls; unrelated syntax/authentication/unavailable-target errors rejected. No raw secret-bearing diagnostics |
| F03 / R01-03 | migrated_snapshot/security_snapshot plus independent restricted restore | Seven tables, columns/types/defaults, constraints, indexes, functions, triggers, history, owners, effective grants/default ACLs/memberships; restored runtime/backup negatives and category/district/regional controls. Fresh credentials and revoked temporary restore identity deliberately differ |
| F04 / R01-04 | New SupportedRegions migration and RemediationTests | Existing BEFORE-row ancestor traversal needed to detect references to a not-yet-inserted new node during multi-row inserts. New forward function includes ancestor parent references. Real two-connection A?B / waiting B?A rejects cycle after A commits; valid competing change and rollback; categories/districts, multi-row insert/update. Existing applied migrations unchanged |
| F05 / R01-05 | relational_metadata/compare_model in forward runner | Execute actual EF-generated relational schema in separate reference instance; compare canonical PostgreSQL definitions against clean, upgrade and restored paths. Full column type/nullability/default/length/precision, key/FK/check/index definitions; explicit SQL-only inventory retained |
| F06 / R01-06 | ICategoryLookup/PostgresCategoryLookup; RemediationTests plus existing CatalogTests duplicate constraint | At most 100 active categories; requested translation ? configured city default ? retained legacy name. Tests missing/default/legacy/unsupported language/invalid FK/duplicate/bound. Existing discovery/search labels preserved; no full multilingual site |
| F07 / R01-07 | QueryEvidenceTests; SafeQueryTimingInterceptor | Actual parameterized EF queries/plans, first/middle/deep pages, mixed active/publication states and filters, full stable counts and cancellation. Existing 10k/250ms test retained. Slow-read event 2001 logs elapsed milliseconds only, threshold 250ms, at most once/minute/process |
| F08 / R01-08 | RegionalPolicy; new SupportedRegions checks; RemediationTests | Supported product countries NL, languages nl/en. Canonical database values; helper trims and normalizes case. All city/location/translation writes constrained; existing timezone catalog validation preserved. Other syntactically valid ISO codes are unsupported, not declared invalid |
| F09 / R01-08 | Explicit grants in forward runner | Revoke future default SELECT; grant only current seven public synthetic tables (history backup-only). Newly created private_probe denied to both runtime and backup. No demonstrated private data exposure in baseline |
| F10 / R01-09 | Current sections of existing reports/database docs/HANDOVER; governance reconciliation plan below | Historical evidence retained and superseded explicitly. Exact final-head verification is published in the Issue #10 synchronization ledger; independent reviewer/owner gates remain open |

## Evidence scope and repeatability

Use the exact environment printed by `python scripts/verify-forward-migration.py --gate <verified gate evidence.json>`. It creates separate synthetic instances and volumes; never point these tests at the preserved original database. The existing verified source archive gate at `artifacts/ha002a/20261119161050-4b5de6/evidence.json` was reused locally, **not claimed as a fresh original-source backup**. Each R01 rehearsal creates fresh enriched and pre-correction migrated protected archives and independent restores before correction. CI creates its own fresh synthetic baseline gate. Archives/passwords/private inventories stay ignored and protected; only sanitized evidence is published.

Commands: `dotnet restore HappyAlmere.slnx`; Debug and Release `dotnet build HappyAlmere.slnx -c <configuration> --no-restore`; forward runner above; `scripts/Test-Local.ps1 -EnvironmentFile <rehearsal.env>` with HAPPYCITY_EVIDENCE_DIRECTORY; load that environment then `npm run test:browser`; `dotnet package list --project HappyAlmere.slnx --vulnerable --include-transitive --format json`; `npm audit --json`; `python scripts/verify-source.py`; `git diff --check`.

Query workloads/thresholds are encoded before execution in QueryEvidenceTests and documented in migration-policy.md. 10,000 synthetic rows, 30 warmed sequential samples per first/middle/deepest available position, page 50, five scenarios, p95 <=250ms. EXPLAIN uses actual intercepted EF command plus cloned bound synthetic parameters. Artifacts omit parameter values, but plans may contain explicitly synthetic test constants. No real searches or connection strings. Slow-query monitoring measures command execution/reader opening, not complete result consumption; it is a diagnostic signal, not a production SLA or comprehensive monitoring platform. Framework sensitive-data logging remains disabled.

Supported hierarchy transaction isolation is READ COMMITTED. Existing statement advisory lock serializes writes; higher isolation is rejected explicitly. Tests use bounded statement timeouts to distinguish deadlock/hang from expected 23514 cycle rejection. Multi-row insert and update are separately exercised.

SQL-only objects: archival discovery legacy_city_id/legacy_category_id; preserved UUID and legacy indexes plus entries_page_order expression index (exact definitions in rehearsal evidence); catalog.lock_hierarchy, check_hierarchy and timezone validation functions and their triggers (exact restore comparison). EF-generated schema is a definition reference only, not a replacement for migrations or SQL-only controls. PostgreSQL implicit NOT NULL constraints are represented by column nullability rather than compared as extra named constraints.

Restoration does not replay privileged global roles/passwords. Common role capabilities, memberships, effective table/schema/database/function access and default ACLs are compared. Fresh per-instance passwords intentionally differ and are not read into evidence. A restricted temporary restore login assumes the non-login owner only during restoration; login and owner membership are explicitly revoked and verified afterward. Source global administrator secrets are never replayed. PUBLIC function execution is revoked after no-ACL restore.

## Updated acceptance matrix A ? Issue #4

| ID | Current technical disposition / evidence | Acceptance boundary |
|---|---|---|
| AC01 | Preserved original database/volumes and applied migration bytes; baseline d3d6a | Independent review pending |
| AC02 | Reused verified protected original archive; fresh isolated enriched/pre-correction/restricted backups | Local synthetic protection only |
| AC03 | Extended independent restoration metadata/rows/security comparisons | R01 fresh evidence required |
| AC04 | Six catalog tables/UUIDs plus retained discovery; actual EF physical parity | Approved D01-D10 retained |
| AC05 | Full UUID mappings and boundary IDs reconciled by runner | Single-source namespace unchanged |
| AC06 | Full original rows/relationships/publication reconciled | Synthetic-only |
| AC07 | Clean/upgrade/no-op and immutable applied history | New forward correction only |
| AC08 | Exact intended permission denials, positive controls, deny-by-default future tables | No runtime owner/migration privilege |
| AC09 | VerifyFull TLS, protected external credentials, scans | Production lifecycle deferred |
| AC10 | Existing regressions plus actual deep filtered EF plans/timings | Local sequential budget only |
| AC11 | Exact P0001 marker, full rollback, fresh independent recovery | No production RTO/PITR claim |
| AC12 | Updated evidence and re-review submission | Independent acceptance and owner disposition pending |

## Updated acceptance matrix B ? master HA-002

These classifications concern observed implementation, not architectural acceptance.

| ID | Classification | Actual evidence / remaining limitation |
|---|---|---|
| AC01 | implemented | Layered project graph, build and source checks; EF only Infrastructure |
| AC02 | implemented | Isolated persistent Docker volumes, loopback bindings, verified TLS |
| AC03 | implemented | Catalog UUID definitions; clean/upgrade/reference metadata comparisons |
| AC04 | implemented | Cities fields/checks, active state and supported region tests |
| AC05 | implemented | Trusted synthetic resolver and HTTP host/alias/inactive tests; no live domain activation |
| AC06 | implemented | Same-city district FK and committed concurrency/multi-row cycle tests |
| AC07 | implemented | Structured location checks and NL support; no fabricated addresses |
| AC08 | implemented | Shared category constraints and concurrency tests |
| AC09 | implemented | Composite translation key/FK and bounded requested/default/legacy lookup |
| AC10 | implemented | Relational same-city constraints and actual request city scoping; private authorization is future HA-003/004 |
| AC11 | implemented | Async bounded projected/keyset queries; actual EF deep/filter plans and local budget; production capacity deferred |
| AC12 | implemented | Separated roles, exact negatives/positive controls, explicit grants and future-table denial |
| AC13 | partial | Verified local TLS/secrets; production rotation/storage/retention remains HA-009/010, not waived |
| AC14 | implemented | Immutable applied history, new forward correction, clean/upgrade/no-op and complete recovery comparison |
| AC15 | implemented | Synthetic-only fixtures; no auth/payments/imports/live deployment |
| AC16 | implemented | Actual PostgreSQL suites; fresh final-head CI still required at this checkpoint |
| AC17 | partial | Actual protected local backup/restore demonstrated; production encryption, RTO/RPO and recovery operations deferred HA-009/010 |
| AC18 | partial | Current documentation/matrices supplied; independent acceptance and owner decision outstanding |

## R01 acceptance checklist ? evidence submission, not self-approval

| ID | Technical evidence / remaining gate |
|---|---|
| AC01 | Ten-finding mapping above; future risks and limitations explicit |
| AC02 | Resolver and HTTP host/canonical/isolation tests; no DNS activation |
| AC03 | Exact SQLSTATE/reason and positive controls; syntax/auth/unavailable-target false positives rejected |
| AC04 | Exact controlled P0001 marker plus rollback and independent recovery |
| AC05 | Full migrated metadata/rows/security comparisons and restored functional negatives; intentional credential/restore differences documented |
| AC06 | Categories and districts: committed competing-cycle rejection, valid competition/rollback, multi-row insert/update |
| AC07 | Actual EF plans/deep filter traversal; EF physical definitions; regional checks; unchanged local budget |
| AC08 | Bounded translation fallback tests and future private-table denial |
| AC09 | Preserved applied history, source rows/publication, role/TLS/security/discovery regressions |
| AC10 | Fresh local and exact final-head remote results must be recorded below; no historical results relabeled |
| AC11 | Isolated environments only; original/main/develop/governance untouched; no merge/deployment |
| AC12 | Both original matrices above; independent re-review to be requested in Issue #10, Issue #4 and PR #6; no acceptance claim |

## Governance reconciliation plan and residual risks

PR #9 / docs/ha-gov-001 at 871e7a35c95b05b75b047fc1cc048f29e33b7cbc remains separate and unchanged. After explicit integration authorization: compare its document index/protocol with these current top sections, retain review provenance and historical evidence, resolve links/status conflicts, validate all matrices, and submit the combined diff for review. Do not merge that branch as part of R01. HA-003/004 design observations remain design-only. ARCH-01 missing provenance is not replaced with invented findings.

Residual production encryption, retention, credential rotation, operational RTO/RPO, capacity and infrastructure controls are proposed for HA-009/010 disposition; private resource authorization belongs to HA-003/004. These are explicit outstanding requirements, not silently waived or claimed complete. Public deployment remains prohibited. Independent reviewer and owner must decide acceptance and any scope deferral.

## Fresh verification record

Latest local final verification: **104 .NET passed, zero failed/skipped; 42 responsive cases across seven widths; 190 functional/security and five rate assertions passed**. All three suites verified loopback listeners and rejected six non-loopback connections each across three local interfaces, with HTTPS certificate validation retained. Debug/Release builds: zero warnings/errors. Restore succeeded. NuGet direct/transitive and npm audits: zero known vulnerabilities. Fresh isolated rehearsal: **18 checks passed**, including restored district and category controls. Actual EF query evidence contains 11 workload/position measurements with p95 **1.5998?26.9403 ms**, below 250ms. Sanitized artifacts: [evidence/HA-002-A/r01](evidence/HA-002-A/r01/README.md). Scoped current-source and reachable-history credential scan passed with zero findings; dependency direction and development binding configuration checks passed. Exact final-head CI and containing synchronization SHA are published in [Issue #10](https://github.com/Shiva721254/HappyAlmere/issues/10), avoiding a self-referential commit hash in this report. Captured checkpoint results below retain their original outcomes.

Changed files include Application resolver/lookup contracts; Domain RegionalPolicy; Infrastructure resolver/lookup/elapsed-only interceptor, DbContext, new correction migration/designer and snapshot; HomeController, Program, appsettings; three test files; forward verifier; CI artifact list; existing documentation and sanitized evidence. No package manifests or existing applied migration files changed. Exact list is `git diff --name-only d3d6a558cccda2d8e437854c46b5cbfe5ab7b9f4..HEAD` after commit.

Initial expanded .NET run: 100 passed/4 failed (three overlength synthetic category slugs, one HSTS test-host mapping). Corrected fixtures: 104 passed, zero failures/skips. Earlier rehearsal exposed equivalent PostgreSQL dump serialization for a new draft language check; simplified that uncommitted expression without modifying applied migrations. Restricted sandbox build returned failure without compiler diagnostics; authorized local build passed with zero warnings/errors. These failed attempts are retained as limitations of the working process, not counted as passing runs.

## Remote verification checkpoints

Implementation **b2355c30e9e08686a95b0e97ed672c0798401451** was pushed and remote verified. [PR CI 44586738859](https://github.com/Shiva721254/HappyAlmere/actions/runs/44586738859) succeeded. [Push CI 44586720660](https://github.com/Shiva721254/HappyAlmere/actions/runs/44586720660) passed database/application/scans/responsive tests but failed one functional assertion: `1440 skip target` read an empty fragment immediately after Enter. The test now waits at most five seconds for the exact `#main` navigation before the unchanged assertion. This corrects a browser event timing race without weakening accessibility or HTTPS/security checks. Local browser re-verification after this correction passed all 42 responsive, 190 functional/security and five rate assertions, with verified TLS and loopback checks. Exact latest-head remote results are recorded in Issue #10; the failed run is not relabeled successful.

### Performance checkpoint and controlled fixture preparation

Head 63cc36a47b0e511aba6b3ff909ab02e70f218af7 push [44588270956](https://github.com/Shiva721254/HappyAlmere/actions/runs/44588270956) failed the retained 10,000-row benchmark at **p95 280.1229ms**, above the unchanged 250ms budget (103/104 .NET tests passed). The new actual-query workloads in that run remained below budget (maximum 75.07ms). The failed older benchmark wrote its artifact only after asserting, so its plan/samples were unavailable; this evidence limitation is explicit.

Inspection found that rollback fixtures share the same physical test tables and can leave dead tuples/statistics that vary with test order. Both benchmark fixtures now establish a controlled baseline with ordinary VACUUM (ANALYZE) on the isolated synthetic cities/categories/discovery tables **before** their transaction. This removes dead tuple interference, not live data. It is test-only preparation, not a runtime application change. The exact 10,000-row workloads, 30 samples, traversal assertions and 250ms budget are retained. Measurements/plans are written before threshold assertions so failures retain diagnostics. The earlier hosted-run slowdown is not conclusively attributed to one cause. Local re-verification passes 104/104 tests; actual-query p95 range 1.2909?19.794ms. The companion PR run [44588282832](https://github.com/Shiva721254/HappyAlmere/actions/runs/44588282832) passed the preceding head, while its push performance failure remains recorded. Fresh final-head CI is required; no production capacity claim.

## Historical implementation record (superseded status; evidence retained)

# HA-002 implementation report - HA-002-A recovery/design phase

Date: 19 November 2026 (Europe/Amsterdam). [Assignment: Issue #4](https://github.com/Shiva721254/HappyAlmere/issues/4).
Branch: feature/ha-002-a. Starting implementation commit:
62b1defdca00989bf363a39057f5558aae8ee3e3. Original develop:
c2964eeff4232dd1f925c63af06f8c97c751b161. Approved main:
0e7739d3f72f08b095c3316e6680053ae99915f6. Neither branch is modified.

**Status: baseline inventory, protected backup, isolated restore and non-destructive
migration design delivered for independent review. HA-002-A remains active and is
NOT fully complete or accepted.** No new forward EF migration, key conversion,
schema-ownership change or existing-table alteration has been implemented.

## Requirements reconciliation and authorization

The owner explicitly directed Issue #4 implementation and subsequently clarified the
immediate sequence: (1) inventory, (2) backup, (3) isolated restoration/integrity,
(4) non-destructive design based on repository architecture. All twelve issue criteria
remain applicable. HA-002-ARCH-01 has no verified standalone repository reference,
confirmed by the owner; its missing detailed findings are not invented. The existing
master architecture and 18 consolidated criteria remain authoritative technical
references. Issue AC01-AC12 are distinct from master HA-002-AC01-AC18.

Existing architecture explicitly requires catalog schemas, UUID entities, six catalog
concepts and preservation of useful discovery. It does not specify final physical
mapping/defaults for the pre-existing DiscoveryEntries extension and district display
strings. These decisions are surfaced in [migration-policy.md](../database/migration-policy.md)
and Issue #4 for independent review instead of guessed as authoritative geography or
silently applied. The owner's restriction on changing keys/ownership/structures before
successful backup/restore was respected. Passing that gate is not design self-approval.

## Inventory and preserved source

Live source: local happyalmere-v02-postgres-1, configured 127.0.0.1:55432 only;
PostgreSQL **18.7 (Debian 18.7-1.pgdg12+2)**, pinned existing postgres:18-bookworm digest.
EF Core Relational/Design and dotnet-ef: **10.0.14**; Npgsql EF provider: **10.0.3**;
SDK pin **10.0.403**. No new package dependency was introduced.

Source public schema contains Cities (1), Categories (5), DiscoveryEntries (6) and
__EFMigrationsHistory (1). Applied migration:
20261119000108_InitialDiscovery. Full schema/column/constraint/index/sequence/role/grant
inventory and all-row digests are in evidence/recovery JSON, without raw row content.
All six discovery entries are published; unpublished count is zero. Orphan city and
category references and duplicate city/slug groups are zero. Source must exactly match
known synthetic fixtures; unknown data is rejected before backup. Future migration
verification still needs unpublished/additional-data cases on isolated fixtures.

Existing roles remain inventoried, not hardened: happy_migrator is development admin;
happy_app is restricted read-only. The source was started only for read-only inventory
and pg_dump, then restored to its initial stopped state. Repeated before/after inventories
match. No source schema, roles, keys, table data or migration files changed. All owned
rehearsal containers are stopped after verification; volumes and protected archives remain.

## Backup and restoration evidence

New script: scripts/verify-database-recovery.py (Python standard library only).
Isolated compose: infrastructure/recovery/compose.yaml, reusing existing pinned image,
TLS start script and development bootstrap, with unique project/data/TLS volumes.
Runbook: [docs/database/backup-and-recovery.md](../database/backup-and-recovery.md).

| Local run (UTC identifier) | Backup / restore seconds | Archive SHA-256 |
|---|---|---|
| 20261119152841-bc3950 | 0.335 / 0.127 | 9fb4ecdd85ecb158cb5b334ae88bd5cf654d08702bf589ab2dd065b557518451 |
| 20261119153036-9edf41 | 0.327 / 0.132 | dec44519eca4fff30d88ec902e5d305a43cf7f4d312f6baca7796d96573e49bc |
| 20261119153223-5a20f4 (final runner) | 0.334 / 0.136 | 99a21f3760156abebc3f7860be6a1611bfc9c980c43f350ffb1b92c2a32f9908 |

Archive byte hashes differ because dumps contain run metadata; restored canonical row
and schema equality, not binary archive identity across runs, is the repeatability test.
All runs reconciled complete rows/counts, constraints, indexes, sequences, migration
history, roles/grants, publication states, FKs and representative queries. Final runner
also rejected four altered non-fixture inputs, rejected a truncated archive with an
empty target, then successfully restored the intact archive in one transaction.
This is failed-restore recovery, **not** a forward-migration failure rehearsal.

Artifacts stay in ignored artifacts/ha002a/<run> with Windows current-user/SYSTEM-only
ACL (Linux CI uses 0700/0600). Backup/global metadata, private row snapshots and generated
credentials are not committed/uploaded. Globals exclude password hashes; restore
recreates known local identities with new passwords, no blind superuser-role replay.
Host disk encryption and production backup identity/retention/storage controls remain
unverified. Only sanitized evidence JSON is publishable after scoped review.

## Fresh regression and verification

Commands executed in the HA-002-A worktree:

```powershell
python scripts/verify-database-recovery.py --leave-target-running
# Load only that isolated target's protected rehearsal.env in-process, never print it.
dotnet restore HappyAlmere.slnx
dotnet build HappyAlmere.slnx -c Debug --no-restore
dotnet build HappyAlmere.slnx -c Release --no-restore
dotnet test HappyAlmere.slnx -c Release --no-build --logger trx --results-directory artifacts/ha002a-tests
npm ci --ignore-scripts
npm run test:browser
python scripts/verify-database-recovery.py --port 55434
dotnet list HappyAlmere.slnx package --vulnerable --include-transitive --format json
npm audit --json
```

Fresh results against the restored baseline: Debug 6.78s and Release 3.37s, zero
warnings/errors; **68 tests passed**, zero failed/skipped (reported duration 13s).
**42 responsive cases, 190 functional/security assertions and five rate assertions
passed**; actual HTTP/HTTPS loopback bindings and non-loopback rejection passed.
TLS was validated with the isolated public certificate; no validation bypass.
NuGet/npm scans report no known vulnerabilities at execution. Source/secret and final
CI outcomes are added through the exact implementation commit and issue/PR handoff;
queued CI is never reported as passing.

A read-only EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON) of the existing projected/bounded
familie search returned one row on the restored six-row fixture. See query-plan.json.
This verifies execution only: no representative throughput/index scalability claim,
no new pagination contract and no production benchmark. Database tests may advance
sequences on the isolated target despite fixture transaction rollback; the source
is never used for regression tests and remains unchanged.

## Issue #4 acceptance matrix (not owner acceptance)

| Criterion | Current evidence/status | Remaining work |
|---|---|---|
| AC01 | Baseline inventory and requirements mapping demonstrated; existing stack/history retained | Independent review of inventory/design; unavailable ARCH-01 provenance explicit |
| AC02 | Protected repeatable logical backup/checksums/version/prerequisites demonstrated | Production encryption/retention/identity controls not claimed |
| AC03 | Clean isolated restores demonstrated before any schema modification | Gate evidence ready for review |
| AC04 | Partial: target/master traceability and non-destructive mapping proposal documented | Resolve identified mapping/default/role/pagination decisions independently |
| AC05 | Not implemented: no UUID conversion | Review mapping, implement and prove identity/FK reconciliation |
| AC06 | Partial: all baseline rows/relationships/publication state preserved by restore | Prove before/after forward-migration preservation, including unpublished fixtures |
| AC07 | Not implemented: original migration repeats in existing tests | New forward migration clean/upgrade/failure/rerun paths pending design review |
| AC08 | Partial: existing runtime TLS/read-only and denied DDL tested | Separate owner/migration/backup identities and escalation negatives not implemented |
| AC09 | Local protections demonstrated: external secrets, strict TLS, protected ignored artifacts; scans | No production secret lifecycle/host encryption claim |
| AC10 | Partial: existing discovery/security/browser regressions pass; baseline plan recorded | Stable pagination and representative index/query-plan acceptance pending |
| AC11 | Not demonstrated for migration failure | Truncated-backup rejection/recovery is not a substitute; rehearse after reviewed migration exists |
| AC12 | Partial: existing tests and recovery gate pass; docs/evidence and review request prepared | Remaining implementation criteria, independent review and final owner approval pending |

## Changed files, decisions and remaining gate

Added recovery runner/isolated compose, backup runbook, proposed migration policy,
this existing-planned implementation report and sanitized HA-002-A evidence. Updated
.gitignore for archive/cache exclusions, existing workflow for the same isolated recovery
gate, README, HANDOVER, CHANGELOG, AGENTS, database architecture and existing gap/risk
records. No new overlapping PROJECT_STATUS/ROADMAP/TASKS/ACCEPTANCE files were created;
existing report/audit/criteria/risk files serve those roles. No original requirement
or history was discarded. No new application entity, query, migration or schema exists.

The immediate owner-authorized recovery/design sequence is ready for independent
review. HA-002-A stays active; do not close Issue #4 or mark HA-002 accepted. Decisions
requested: legacy UUID mapping, existing discovery extension placement, treatment of
unverified district/domain/address information and backfill defaults, role ownership
and pagination contract. Forward implementation/rehearsal waits for those dispositions.
No merge, deployment, source-volume deletion, production access or signing expense.


Pre-commit verification: dependency direction, JSON and development launch configuration
checks passed; credential-pattern scan covered 18 reachable commits / 2,042 tracked
file versions plus current authored files and found no matches. This is scoped pattern
review, not a guarantee against all secret forms. Relative documentation links and
`git diff --check` passed. Original Infrastructure/entities/migrations diff against
develop is empty; backup archives are confirmed Git-ignored. No raw dump, globals,
row snapshot, environment file or private certificate material is staged.


## Initial remote CI portability correction

Implementation checkpoint e652622f2d4243dac55aa12e32f2d92e003b81e4 was pushed for review
in [draft PR #6](https://github.com/Shiva721254/HappyAlmere/pull/6), based on integration,
without any merge. Initial push run [44557097305](https://github.com/Shiva721254/HappyAlmere/actions/runs/44557097305)
failed in recovery inventory metadata with Git exit 128: a clean Actions checkout does
not create a local develop branch. This is not counted as a passed recovery run.
The correction fetches history with checkout and resolves refs/remotes/origin/develop;
local resolution was verified as c2964eeff4232dd1f925c63af06f8c97c751b161. Backup/restore,
TLS, isolation and data checks are unchanged. Corrected CI results are recorded against
their exact final SHA in Issue #4/PR #6; no pending result is presumed successful.


## Verified implementation checkpoint and review handoff

Corrected implementation: **c238b3618c732f288726950e5a59a504975dce1e**, after initial
implementation e652622f2d4243dac55aa12e32f2d92e003b81e4. Both actual corrected runs
succeeded: [push 44557553382](https://github.com/Shiva721254/HappyAlmere/actions/runs/44557553382)
and [PR 44557570176](https://github.com/Shiva721254/HappyAlmere/actions/runs/44557570176).
Ubuntu Debug/Release: zero warnings/errors; 68 tests passed, zero failed/skipped
(push log reports four seconds); isolated synthetic backup/restore gate passed;
HTTPS/browser/responsive/security/rate/network suites passed. NuGet/npm/source checks
passed. CI metadata and actual downloaded sanitized recovery JSON are committed in
`evidence/HA-002-A/ci-*.json`. Linux backup storage uses 0700/0600; no dumps/credentials
were included in the uploaded artifact. The initial PR run 44557257024 also failed
before the same reference fix; neither initial failure is counted as a pass.

Remote feature branch SHA matched c238b36 after push. Main remains 0e7739d and develop
c2964ee; no merge, deployment or source-volume change. The documentation/evidence-only
handoff commit follows this verified implementation checkpoint and is identified by
Git history and the final Issue #4 comment. Draft PR #6 remains unmerged for independent
review. AC04 design decisions and forward criteria AC05/AC07/AC08/AC10/AC11 remain
open as detailed above; do not close the issue or claim HA-002 acceptance.


## Owner-authorized forward phase - 19 November 2026 (latest)

This section supersedes earlier design-only/implementation-pending status, preserving
its historical evidence. Authority: [HA-DB-D01..D10 and eight safeguards](https://github.com/Shiva721254/HappyAlmere/issues/4#issuecomment-7014717337).
Starting checkpoint 3287994afb0bb3fe7e69f60a6bda689f77c40323; branch feature/ha-002-a,
draft PR #6. Governance PR #9 is not merged or overwritten. Independent final
acceptance is still pending; no main/develop modification or deployment.

Implemented six-table catalog plus preserved discovery extension using explicit
transactional expand/backfill/validate/cutover SQL. Original migration files/history
are unchanged. UUIDv8 boundary mapping and full original rows/publication/FKs are
reconciled. New UUIDs use v4. Empty unverified geography/domains remain empty; no live
resolver, Identity or business implementation. Category defaults, Dutch name retention,
city defaults, timestamp provenance and versioned keyset contract were recorded before
schema execution in migration-policy.md. Existing discovery UI remains Dutch.

### Executed verification and evidence

- Fresh source gate reproduced 1 city, 5 categories, 6 entries and original history,
  with source unchanged. Enriched isolated baseline adds two unpublished boundary
  records (IDs 0 and int.MaxValue), restores exactly before any schema modification.
- Forward runner: exact UUIDs, all original content/relationships/publication, clean
  creation, no-op repeat, controlled post-backfill failure/transactional rollback,
  independent fresh failure recovery and restricted post-migration backup/restore
  (all seven tables, constraints, indexes). Twelve explicit runner checks pass.
- Actual owner/migrator/runtime/backup/restore boundaries and TLS connections tested;
  runtime/backup writes, DDL and role escalation rejected. Restore login/membership
  revoked after use. Original legacy bootstrap administrator is not runtime.
- Debug/Release builds: zero warnings/errors. 87 .NET tests passed, zero failed/skipped
  in the latest expanded run before final synchronization; original 68 retained.
  Tests cover model drift, physical metadata, pagination, inactive city, constraints,
  same-city FK, hierarchy cycles/write serialization, security/TLS and existing MVC.
- 10,000-row synthetic workload: 30 warmed sequential page samples, p95 below the
  predeclared 250ms bound; exact timings and plans in performance.json. Stable traversal
  returned all 10,000 titles exactly once. Not a production concurrency/load claim.
- Browser: 42 responsive cases over seven widths; 190 functional/security and five
  rate assertions passed. HTTPS certificate validation retained; actual sockets loopback
  only, non-loopback probes rejected. Prior and new results remain separately scoped.
- NuGet direct/transitive and npm scans reported no known vulnerabilities. Scoped
  tracked-history/current-source credential scan found zero matches; no exhaustive
  secret absence or production-readiness guarantee.
- One final-rehearsal attempt failed before clean-target creation because Docker's
  default address pools were exhausted. Earlier successful evidence was not relabeled.
  Stopped earlier task containers, retained volumes; forward instances now share a
  verified task-owned private network while keeping distinct volumes/credentials.
  Subsequent complete rehearsal passed. A sandboxed NuGet restore/scan failed; the
  network-authorized restore and scan succeeded. No security controls were weakened.

Sanitized [forward evidence](evidence/HA-002-A/forward/) excludes archives, credentials,
raw inventories and private keys. Final commit/remote CI links are recorded at handoff;
local working-tree runs are not falsely attributed to the unchanged baseline commit.

### Issue #4 acceptance matrix - implementing-agent evidence, not final approval

| ID | Forward-phase evidence | Remaining limitation/gate |
|---|---|---|
| AC01 | Existing stack/history retained; baseline inventory and mapping | Independent acceptance pending |
| AC02 | Protected baseline/enriched/restricted logical backups; checksums and prerequisites | Local protected storage, no production encryption claim |
| AC03 | Exact enriched fresh restore before migration | Demonstrated on synthetic isolated instances |
| AC04 | Approved D01-D10 mapping and explicit defaults; six catalog tables plus discovery | No unapproved geography/domain activation |
| AC05 | Entity-tagged UUIDv8; unique legacy IDs; boundary IDs/FK/full-row reconciliation | Import namespace is single-source; new records v4 |
| AC06 | Complete original content, associations and publication preserved | Synthetic dataset scope |
| AC07 | Clean creation, upgrade, no-op repeat, immutable initial history, EF/physical metadata tests | Down refuses destructive downgrade |
| AC08 | Non-login owner and separated migration/runtime/backup/restore/admin; actual permission negatives | Controlled migration/restore intentionally assume owner within isolated DB |
| AC09 | External protected credentials, VerifyFull TLS, negative TLS tests, scoped scans | Production rotation/encryption/retention not asserted |
| AC10 | Existing regression suite plus deterministic bounded keyset, 10k traversal and timing/plan | No concurrent production-load capacity claim |
| AC11 | Post-backfill injected failure rolls back; independent fresh restore reconciles | Local logical recovery, not PITR/production SLA |
| AC12 | Evidence/documents/CI workflow prepared; review requested at synchronization | Remote CI and independent acceptance/owner disposition remain gates |

### Changed-file scope and reproducible commands

Domain catalog entities/UUID helper; Application page/cursor contracts; Infrastructure
EF model, appended CatalogForward SQL/designer/snapshot, repository and compatible
fixture source; MVC continuation link; relational/regression tests. Verification scripts,
separate forward compose, baseline-pinned initialization, explicit environment selection,
CI and existing documentation are updated. No new package or framework dependency.
The exact file list is the PR diff from 3287994.

Commands: dotnet restore HappyAlmere.slnx; dotnet build HappyAlmere.slnx -c Debug/Release;
python scripts/verify-database-recovery.py; python scripts/verify-forward-migration.py
--gate <printed gate>; scripts/Test-Local.ps1 -EnvironmentFile <printed environment>;
npm ci --ignore-scripts; npm run test:browser with that environment; dotnet package list
--project HappyAlmere.slnx --vulnerable --include-transitive --format json; npm audit
--audit-level=high; python scripts/verify-source.py; git diff --check.


### Verified remote CI and implementation checkpoint

Implementation commit **4f8a756c1dbf506b77528428a907054cd61a95f0** was pushed and
remote verified on feature/ha-002-a. Both workflows completed successfully:

- [Push 44567576217](https://github.com/Shiva721254/HappyAlmere/actions/runs/44567576217)
- [PR 44567590442](https://github.com/Shiva721254/HappyAlmere/actions/runs/44567590442)
- [PR verification job](https://github.com/Shiva721254/HappyAlmere/actions/runs/44567590442/job/135713477310)

Both logs report 87 passed, zero failed/skipped .NET tests and successful trusted
HTTPS responsive/security/rate/network suites. Downloaded PR artifacts confirm all
12 forward checks pass. Exact run/job metadata, sanitized recovery/performance and
browser summaries are committed in the forward evidence directory. The PR run tests
GitHub's merge checkout; workflow metadata records the submitted head above.

These are implementing-agent-verified GitHub executions, not independently rerun
architectural tests. AC12's remote-execution gate is now demonstrated for that source
commit; independent architectural acceptance and final owner disposition remain pending.
This evidence-only follow-up changes no application or migration source. Main/develop
and governance PR #9 remain unchanged. Local task containers stopped; volumes retained,
original source stopped. No merge/deployment or issue closure.
