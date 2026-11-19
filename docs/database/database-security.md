# HA-002-A database security and role boundaries

Local synthetic review candidate, not production security certification.
The forward runner creates fresh loopback-only PostgreSQL instances with independent
volumes and the pinned existing image. No original database grants or schema change.

| Identity | Responsibility / verified boundary |
|---|---|
| happy_migrator (legacy bootstrap name) | Isolated PostgreSQL administrator only; historically the baseline superuser. Never application credentials. Name is retained for existing bootstrap compatibility, not a claim of least privilege. |
| happy_owner | Non-login, non-superuser object owner; owns migrated tables/functions; no cluster administration |
| happy_schema_migrator | Restricted login; controlled SET ROLE happy_owner for reviewed migration. Cannot create roles, become superuser or assume legacy administrator. Not a web process credential. |
| happy_app | Read-only catalog/discovery, no migration history access; no DML/DDL or owner/migrator/admin membership |
| happy_backup | Restricted logical backup with SELECT and schema usage; no writes, DDL or role escalation; actual pg_dump over verified TLS |
| happy_restore | Separate login created solely on a fresh isolated restore instance; controlled owner role for restore, no cluster administration. Login and membership revoked after verification. |

Tests use real TLS connections (sslmode/SSL Mode VerifyFull); rejection of untrusted
certificates and unencrypted connections is retained. No TLS bypass. Object ownership,
positive reads, DML/DDL/role escalation negatives, restricted dump and restored
rows/constraints/indexes are verified. The migration role is intentionally privileged
within its own database object boundary; it is not a runtime role.

Secrets are random per rehearsal, external to source in ignored owner/SYSTEM ACL or
0700/0600 artifacts. Never publish environment files, archives, globals, private SQL
row inventories or keys. Web/browser children receive only the runtime connection.
The legacy initialization script is pinned to InitialDiscovery; it cannot silently
apply CatalogForward to the preserved database.

## Rotation procedure

Quiesce only the isolated instance or approved maintenance target. Generate a fresh
credential outside Git; change the relevant login using the controlled administrator,
update protected process configuration, recycle affected pools, then verify new
connections and reject the old credential. Existing sessions require termination or
pool retirement to invalidate immediately. Rotate runtime/backup/migration separately;
never grant a role merely to work around rotation failure. Rehearsals generate fresh
credentials each run; an in-place production rotation has not been executed.

Host encryption, production storage/backup retention, managed secrets, RTO/RPO, external
firewall and public deployment are not established by these local tests. Preserve
SEC-F05 and related production risks for their remaining lifecycle requirements.
