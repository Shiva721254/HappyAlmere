# HA-002 - Existing implementation gap analysis

Scope: source originally on develop at c2964eeff4232dd1f925c63af06f8c97c751b161,
reconciled with the approved V0.1 foundation on a separate integration branch.
No additional database feature development is authorized by this report.

## Requirements provenance and limits

The authoritative HA-002 master database architecture and HA-001-ARCH-03 review
were supplied by the owner during closure. [The baseline digest](../requirements/HA-002-master-baseline.md)
records all twelve DB ADRs, six immediate catalog tables and acceptance requirements.
The original existing V0.2 report is implementation evidence, not the acceptance baseline.
This analysis distinguishes actual invariants from harmless naming differences.
No future conceptual business/identity/billing inventory is treated as authorization
for immediate implementation. Independent review of these gaps is required first.

Source paths below are under HappyAlmere_V0.1_MVC unless prefixed docs/scripts/infrastructure.

| Area / available requirement | Status from source | Evidence | Gap / verification needed |
|---|---|---|---|
| Preserve layered MVC and repository contracts | Implemented | src projects, Program.cs, DiscoveryService, IDiscoveryRepository | Recheck graph and integration build. No architectural rewrite. |
| Persistent city/category/catalog entities | Implemented narrow catalog | Domain/City, Category, DiscoveryEntry; Infrastructure/HappyCityDbContext | Three public-schema tables cover only part of the six-table catalog baseline; see the field matrix below. |
| Referential integrity and bounded content | Implemented | HappyCityDbContext, InitialDiscovery migration | Required city/category FK, restricted deletes, bounded columns, slug/title checks. No private tenancy model. |
| Unique identifiers | Implemented for current catalog | Unique city/category slugs, unique city+entry slug, integer identity keys | CONFLICT with DB-ADR-003 UUID primary keys. Existing integer keys must not be rewritten without a reviewed migration/recovery plan. |
| City-aware reads | Partial | DiscoveryFilter.CitySlug and PostgresDiscoveryRepository WHERE city slug | Almere fixed server-side. No trusted domain resolver/configurable site branding. |
| Private cross-city authorization | Missing by design at this milestone | No accounts/identity; runtime can SELECT catalog across cities | Do not treat public city filtering as row-level security or private resource authorization. |
| Indexed bounded projected async reads | Implemented | Composite city/published/category/id index; AsNoTracking; Take(100); projection; ToArrayAsync(token) | Index usefulness for substring ILIKE and realistic datasets not measured. |
| Pagination | Partial | MaximumResults=100 and refine-search message | No page/cursor API or keyset pagination. Pagination required by master section 9; review an appropriate contract before implementation. |
| Literal Dutch case-insensitive search | Implemented | Escaped ILIKE and PostgresTests | Test real PostgreSQL, hostile input and database-incompatible text. No full-text ranking implemented. |
| Controlled migrations | Implemented local foundation | EF migration/factory, Initialize-LocalDatabase.ps1; no startup migrate | Local migration identity is development administrator; production privilege design deferred. |
| TLS and least-privilege runtime | Implemented local controls | VerifyFull guard, pg_hba.conf, read-only happy_app grants | Validate real TLS, bad trust/plaintext rejection and denied writes. Not production certification. |
| Secret handling | Partial | Ignored environment files, separated runtime/migration vars, masked CI outputs | Local plaintext dev files not production secret storage. No rotation/recovery design established. |
| Data Protection/identity/payments | Deferred, not implemented | Risk SEC-F06; no such app features | Outside closure; do not implement them to fill this report. |
| Synthetic seed provenance | Implemented | SyntheticDiscoverySeed with six records | Only synthetic content. HasData is not a future production editorial workflow. |
| Security and responsive baseline | Reconciliation required | R01/R02 rate policy, CSS, shared category IDs, browser suites versus older develop | Preserve all improvements and run applicable regression suites against PostgreSQL. |
| Production performance | Unverified | Six-row seed and only local sequential samples | Representative datasets, SQL plans, load/concurrency thresholds remain PERF-F01/F02. |
| Backup/recovery/monitoring | Missing verification | No tested production recovery or operations evidence | Define only after master review; no new operational deployment in closure. |
| Master database architecture/acceptance | Partial and conflicting | Owner-supplied master and matrices below | Independent gap review must approve the smallest migration/remediation plan; no implementation in closure. |

## Master ADR traceability

| ADR | Disposition | Source and rationale |
|---|---|---|
| DB-ADR-001 | Partial | One shared happyalmere database and city FK/query scope exist; domain-based city resolution absent. Public scope is explicitly not private authorization. |
| DB-ADR-002 | Conflicting | Tables are quoted Cities/Categories/DiscoveryEntries in public. Required immediate catalog schema absent. Do not create all future schemas speculatively. |
| DB-ADR-003 | Conflicting | int identity Id/FKs instead of UUID application identifiers. No destructive conversion authorized; preserve existing applied migration/history. |
| DB-ADR-004 | Implemented | EF Core/Npgsql mapping/migrations/repository in Infrastructure; Domain has no EF/project dependency. |
| DB-ADR-005 | Implemented locally | Digest-pinned Docker PostgreSQL, persistent data/TLS volumes, host publication 127.0.0.1:55432 required by host app/tests. No public exposure. |
| DB-ADR-006 | Implemented for current scope | Relational tables/FKs; no JSONB needed for current data. Absence of speculative JSONB is not a defect. |
| DB-ADR-007 | Satisfied exclusion | No separate NoSQL/analytics database. |
| DB-ADR-008 | Partial | Read-only happy_app separate from happy_migrator; migrator is development administrator/owner, not separate non-login schema owner and restricted migrator. Worker/reporting/backup identities deferred until needed. |
| DB-ADR-009 | Partial/conflicting scope | Useful catalog/discovery prototype preserved; only two of six catalog concepts exist. Identity/commercial features correctly absent. |
| DB-ADR-010 | Implemented; fresh results in closure report | Real PostgresTests, not EF in-memory substitution. Foundation doubles supplement full relational tests. |
| DB-ADR-011 | Implemented locally | Versioned initial migration and repeatability test; migration outside app startup. Production migration governance still unverified. |
| DB-ADR-012 | Satisfied boundary | Six synthetic seed rows; no real customer import. Local acceptance does not authorize production/private data. |

## Immediate six-table field and invariant matrix

| Required table | Existing equivalent | Implemented | Missing/conflicting |
|---|---|---|---|
| catalog.cities | public.Cities | Name max100, unique bounded slug, Almere seed, reusable entity/service path | UUID vs int; catalog schema; country_code, default_language, time_zone, is_active, created_at/updated_at; validity/inactive-city tests. |
| catalog.city_domains | None | None | Entire UUID/city FK/normalized unique hostname/primary/active/timestamp model; trusted domain resolver and canonical redirect policy. No public domain should be activated during closure. |
| catalog.districts | DiscoveryEntry.District free text only | Display text is bounded | Structured UUID district/city/parent entity, city+slug uniqueness, external code/type/active/timestamps; same-city parent, self/cycle rejection. Free text is not district referential integrity. |
| catalog.locations | None | None | UUID/city/district relations, structured address/country/BAG fields, coordinate pair/range checks/timestamps, same-city district FK. PostGIS remains deferred, not a missing mandatory feature. |
| catalog.categories | public.Categories | Shared reusable bounded unique slug and Name; stable discovery IDs/behaviour | UUID vs int; catalog schema; parent hierarchy/self/cycle checks, category_type, display_order, is_active/timestamps. Current Name is not a localization model. |
| catalog.category_translations | None | Dutch category names stored directly | Composite category UUID+language PK, category FK, bounded language/name/optional description; unique language translation invariants. |

public.DiscoveryEntries is an existing useful working extension, not a table to delete
just because it is absent from the initial catalog list. Preserve data/migration and
its current query/UI behaviour while the architect reviews future mapping.

## Additional master acceptance gaps

- **Security:** verified TLS/read-only runtime/parameterization are implemented locally. Protected production secret storage, credential rotation, non-login schema ownership and restricted migration roles are partial/missing. No auth/RLS defect is inferred from deferred private workloads.
- **Performance:** bounded async projection and pooling exist; pagination, representative datasets, EXPLAIN/query plans, slow-query monitoring and production load evidence are missing/unverified. No speculative cache/index addition is made.
- **Recovery:** existing persistent volumes and Git checkpoints are preserved. No documented logical backup/restore procedure, backup access/encryption policy or actual isolated restore test is present. **HA-002 acceptance is blocked until the mandatory real restore verification is performed under a reviewed plan.** No volume is dropped or applied migration rewritten in closure.
- **Environment governance:** reproducible local bootstrap/migrations/tests exist; separate staging/production configuration and deployment/recovery controls remain unverified.
- **Documentation:** required docs/database/{database-architecture,entity-relationship-diagram,data-dictionary,database-security,migration-policy,backup-and-recovery}.md, docs/decisions/DB-ADR-001-shared-database.md, docs/requirements/HA-002-acceptance-criteria.md and docs/reports/HA-002-implementation-report.md are missing as specified. Existing docs/V0.2-DATABASE-DISCOVERY.md supplies partial material. The baseline digest/gap report does not pretend to complete these implementation documents.
- **Privacy:** synthetic-only boundary satisfied. Retention periods in the master are proposals, not established legal compliance periods. No account/payment/import/analytics schema is introduced.

## Smallest proposed next steps (not implementation authorization)

1. Independently review the six-table mapping, UUID migration strategy, public-to-catalog schema plan and preservation of DiscoveryEntries.
2. Review protected backups and prove restoration in an isolated synthetic environment before significant migrations.
3. Approve owner/migrator/runtime role separation and secret lifecycle for the intended milestone.
4. Specify catalog/domain-resolution/hierarchy/coordinate invariants and requirement-level tests before adding tables.
5. Reuse current EF repository, TLS, read-only grants, migration/tests and UI. Implement only after the independent gap review authorizes the next bounded assignment.

## Reconciliation and verification

Final integrated identifiers, changed files, test counts, CI URLs and concrete failures
are recorded in HA-001-CLOSE-final-report.md. Historical 63-test develop results are
not reused as fresh evidence. The existing migration and schema are preserved; any
closure correction must be limited to preserving baseline behaviour/security and
making the existing tests executable.

## Handoff and stop boundary

Do not merge the integration branch into develop/main or implement further database
functionality until independent review of this gap analysis. Preserve original develop
and both parent histories. The supplied master mapping, supported cities and data ownership decisions require
independent review before implementation. No new business schema is introduced.
