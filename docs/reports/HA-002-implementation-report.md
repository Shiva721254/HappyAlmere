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
