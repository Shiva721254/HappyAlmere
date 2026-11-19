# PostgreSQL backup and recovery - HA-002-A

Authority: [Issue #4](https://github.com/Shiva721254/HappyAlmere/issues/4) and the
owner's clarified sequence: inventory, backup, isolated restore, migration design.
Scope: the existing local synthetic baseline only. This is a tested development
runbook, not a production/PITR backup policy or approval to mutate the source.

## Prerequisites and protected artifacts

Use the pinned PostgreSQL image/client tools in `infrastructure/recovery/compose.yaml`
(same digest as existing Docker infrastructure), local Docker context, Python 3 and
existing application tooling. The source project must be `happyalmere-v02`, bound to
loopback, with exactly the known six synthetic discovery fixtures and their five
categories/one city. Unknown data is rejected before backup. Stop other writers;
inventory equality before/after guards this quiescent rehearsal, not concurrent
production consistency. PostgreSQL pg_dump itself takes a consistent snapshot.

Run from the scoped HA-002-A checkout:

```powershell
python scripts/verify-database-recovery.py
# For follow-up application regression only, retain the isolated target temporarily:
python scripts/verify-database-recovery.py --port 55434 --leave-target-running
```

Every invocation creates a new timestamp/random project and protected folder under
`artifacts/ha002a/`. It refuses a used target port, foreign source project, remote
Docker context, unexpected baseline or populated restore database. No `--clean`,
drop database, volume removal or source schema/data modification is used. Existing
applied migrations are never rewritten. Source inventory calls enforce read-only
transactions through PGOPTIONS. If this runner starts the source, it stops it again.
The default stops its target; on failure it stops attempted target services even if
`--leave-target-running` was requested. Volumes remain for investigation/recovery.

Windows folder ACL grants only current user and SYSTEM, with inheritance removed.
Linux uses directory 0700/file 0600. Backups are not published, uploaded or committed;
archive extensions and artifacts are ignored. The generated environment and public
certificate remain in that same protected directory. No passwords are printed.
Source/global ownership metadata is captured using `pg_dumpall --globals-only
--no-role-passwords`; never replay those administrative roles into another cluster
without review. The known target identities receive new random credentials.

Host disk encryption is not asserted. For backups leaving this controlled synthetic
host, require approved encrypted storage, access controls, key management, retention,
rotation and disposal policy first. This rehearsal does not satisfy production secret
lifecycle or backup service-account separation. Do not delete existing artifacts or
volumes without explicit recovery/retention authorization.

## Backup, restore and reconciliation

1. Record source versions, schemas, columns, constraints, indexes, sequences, role
   attributes/grants, counts, migration history and publication/relationship invariants.
2. Verify every discovery/category/city row is a known synthetic fixture; negative
   checks prove altered title, publication state, city and category values are rejected.
3. Create a custom-format logical backup with the source container's `pg_dump` and
   separate password-free globals. Record SHA-256 and version prerequisites. Validate
   source inventory still matches, including full-row digests and sequence states.
4. Start a fresh target with the same pinned image and reused TLS/bootstrap scripts,
   unique volumes and loopback-only port. Assert source and target share no volume.
5. Verify the archive checksum and require an empty target. A deliberately truncated
   archive must fail without populating tables. Then restore the intact archive with
   `pg_restore --single-transaction --exit-on-error --no-owner --no-acl`.
6. Compare all baseline rows, counts, definitions, constraints, indexes, sequences,
   roles/grants, migration history, published/unpublished counts, orphan checks,
   unique identifiers and representative literal discovery queries. Abort on mismatch.
7. Compare source inventory again. Save only sanitized evidence for review; raw rows,
   archives, globals and credentials stay protected. No later schema work may precede
   a recorded PASS for this gate.

Ownership/ACL restore is deliberate: existing development bootstrap recreates the
known role names with fresh passwords and grants SELECT by default. Row/schema/grant
comparison proves the intended baseline in this controlled environment. A general
production/global restore is outside this runbook. Source backup uses the existing
local administrative identity without changing its grants; dedicated backup, owner
and restricted migrator identities are still an explicit open acceptance item.

PostgreSQL references: [pg_dump](https://www.postgresql.org/docs/18/app-pgdump.html)
and [pg_restore](https://www.postgresql.org/docs/18/app-pgrestore.html). Dumps are trusted
executable database content; the runner accepts only the known synthetic source.

## Application regression on the restored instance

For a successful `--leave-target-running` invocation, load its private environment
in-process without printing it (PowerShell example; replace the placeholder with the
reported directory). Never run original initialization against these credentials.

```powershell
foreach ($line in [IO.File]::ReadAllLines('artifacts/ha002a/<run>/rehearsal.env')) {
    $parts = $line.Split('=', 2)
    [Environment]::SetEnvironmentVariable($parts[0], $parts[1], 'Process')
}
dotnet restore HappyAlmere.slnx
dotnet build HappyAlmere.slnx -c Debug --no-restore
dotnet build HappyAlmere.slnx -c Release --no-restore
dotnet test HappyAlmere.slnx -c Release --no-build --logger trx
npm ci --ignore-scripts
npm run test:browser
```

The source remains stopped. Test fixtures roll back in the isolated target; the
existing migration-repeatability test is allowed only there. Browser child processes
strip migration/admin credentials. Verify TLS using the isolated public certificate;
never disable certificate validation. Stop the exact target container reported in
evidence after tests (`docker stop <targetContainer>`); do not remove volumes.

## Failure handling and recovery limits

If checksum, synthetic guard, restore or reconciliation fails: stop, retain protected
artifacts, do not mark the gate passed, do not change the source or proceed to schema
migration. Diagnose without publishing raw SQL/rows/credentials. Rerun into another
fresh isolated instance with the intact backup. Never overwrite a populated target.
Repeated baseline restore and recovery after a rejected truncated archive are proven
in the implementation evidence. **Recovery after a new forward migration failure is
not yet demonstrated.** Its transaction and recovery design is in
[migration-policy.md](migration-policy.md), pending independent review.

Measured rehearsal durations and actual counts are in the
[implementation report](../reports/HA-002-implementation-report.md) and linked JSON.
They describe a tiny local synthetic dataset, not a production RTO/RPO or downtime SLA.


## Authorized forward rehearsal (supersedes earlier pending-migration limitation)

Owner disposition: Issue #4 comment 7014717337. Preserve the original source; use
only local synthetic isolated instances. Build Release, then run:

```powershell
python scripts/verify-database-recovery.py
# Substitute the exact evidence path printed by the successful gate:
python scripts/verify-forward-migration.py --gate artifacts/ha002a/<run>/evidence.json
# Substitute the exact protected environment path printed by the forward runner:
./scripts/Test-Local.ps1 -EnvironmentFile artifacts/forward/<run>/rehearsal.env
. ./scripts/Load-LocalEnvironment.ps1 -EnvironmentFile artifacts/forward/<run>/rehearsal.env
npm run test:browser
```

The runner verifies immutable original migration files and archive checksum, clones
into new volumes, adds only synthetic unpublished ID=0/int.MaxValue fixtures, takes
and restores that exact enriched backup before migration. It injects a session-local
failure after UUID backfill/before commit, compares the complete old inventory/history,
then independently restores another new instance. Successful upgrade is reconciled
against all original content, IDs, associations and publication flags. Clean creation,
second no-op, restricted roles/TLS, restricted backup and dedicated restore are tested.

Only evidence.json is publishable after review. Archives, credentials and raw inventory
remain private. Stop exact targetContainers from that run's evidence after confirming
Docker compose project labels start ha002f-; do not delete volumes. The runner stops
its instances on failure, retaining artifacts. A successful run leaves isolated
instances for tests. No recovery command targets the original source for mutation.

CatalogForward.Down deliberately refuses a destructive downgrade. Recovery is a fresh
instance restored from protected backup, not dropping tables or rewriting history.
Inspect all generated SQL before use outside this fixed reviewed local rehearsal.
