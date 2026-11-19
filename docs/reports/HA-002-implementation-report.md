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
