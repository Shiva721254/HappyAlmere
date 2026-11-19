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

## Evidence basis and 18-criterion assessment

Baseline inspected: integration source at ec5bcfd28be773869a64d201fe3fc6300d2c8a46,
with source-bearing reconciliation merge ee9651b75613df4a200adea83e409100011a636c.
This update changes documentation only. Existing observations are recorded results,
not new local test executions. Full 18-item definitions and source-section traceability
are in [the consolidated criteria](../requirements/HA-002-acceptance-criteria.md).

Source shorthand: Domain/, Application/, Infrastructure/, Web/ mean
HappyAlmere_V0.1_MVC/src/HappyCity.<layer>/; PostgresTests and WebTests mean files in
HappyAlmere_V0.1_MVC/tests/HappyCity.FoundationTests/. Evidence filenames resolve under
docs/reports/evidence/HA-001-CLOSE/. Source inspection establishes model/code presence;
executed tests establish only their particular scenarios, not the entire intended design.

Recorded verification: [final baseline PR CI](https://github.com/Shiva721254/HappyAlmere/actions/runs/44546174299)
passed all 68 tests, 42 layout cases, 190 security assertions and five rate assertions
on Ubuntu with actual TLS PostgreSQL and HTTPS browser checks. Local Windows results
are in integration-tests.json and integration-*-results.json; prior successful source
CI metadata/results are in integration-ci.json and integration-linux-*.json.
These are implementation-run evidence, not independently rerun architectural tests.
The historical V0.2 document's claim of 63 tests is not the basis for this assessment.

Overall: **4 implemented, 8 partial, 4 missing, 1 conflicting, 1 unverified**.
None of these counts means HA-002 is accepted. All 18 requirements are assessed below.

| Criterion | Overall classification | Evidence / observed outcome | Remaining gap or equivalence decision |
|---|---|---|---|
| HA-002-AC01 - Layered PostgreSQL architecture | **implemented** | Infrastructure/HappyCity.Infrastructure.csproj, HappyCityDbContext.cs, PostgresDiscoveryRepository.cs; Web/Program.cs; project-reference graph in integration-static-results.json. Recorded Debug/Release builds and 68 tests pass; real PostgreSQL MVC test exercises registration. | Names are compatible; relational EF/Npgsql and layers satisfy the requirement. No JSONB/NoSQL is needed for this scope. |
| HA-002-AC02 - Reproducible isolated development database | **implemented** | compose.yaml; infrastructure/postgres/start.sh and pg_hba.conf; scripts/Initialize-LocalDatabase.ps1. Recorded fresh CI bootstrap succeeds; closure inspected actual 127.0.0.1:55432 binding. Existing local volumes preserved; container restored to stopped state. | Host app/tests need port 55432; differing port number is compatible. This establishes local development isolation, not production perimeter security. |
| HA-002-AC03 - Functional catalog schema and identifiers | **conflicting** | Domain/City.cs, Category.cs, DiscoveryEntry.cs; Infrastructure/HappyCityDbContext.cs and Migrations/20261119000108_InitialDiscovery.cs. Source/migration use int identity keys and public-schema Cities/Categories/DiscoveryEntries; real migration tests pass for that existing design. | Integer keys provide valid current integrity but do not satisfy the explicit UUID decision; public does not satisfy catalog separation. Case/naming alone is not a defect. Preserve data/history until a reviewed migration/recovery plan exists. |
| HA-002-AC04 - Cities catalog | **partial** | Domain/City.cs; Infrastructure/HappyCityDbContext.cs and SyntheticDiscoverySeed.cs; PostgresTests.Cities_and_drafts_are_isolated_and_queries_are_bounded. Real PostgreSQL test inserts Utrecht and scopes reads; city name/slug mapping exists. No active/timezone/country/language fields or tests. | Reusable City and service satisfy extension foundations. UUID/schema conflict is AC03; missing lifecycle/settings fields prevent full acceptance. |
| HA-002-AC05 - Trusted city domains | **missing** | Infrastructure/HappyCityDbContext.cs and model snapshot contain no CityDomain; Web/Controllers/HomeController.cs fixes the current city to Almere. Current MVC test rejects query-string city switching; no domain-resolution test or implementation exists. | Fixed trusted server city plus host validation protects the current single-city prototype. It does not implement configurable domain resolution; do not expose public domains in remediation. |
| HA-002-AC06 - Structured districts and hierarchy | **missing** | Domain/DiscoveryEntry.cs only has District text; model and migration contain no District entity or hierarchy. Existing search tests exercise district text, not district referential integrity. | Display text serves current discovery but cannot satisfy the structured geographical/hierarchy invariants. Preserve existing display data during a future reviewed mapping. |
| HA-002-AC07 - Structured locations and coordinate integrity | **missing** | Domain entities and Infrastructure model/migration contain no Location entity or coordinate/address fields. No location constraints or relational tests are present. | Absence of speculative PostGIS is acceptable; missing relational location invariants are not. Do not import residential information to fill the gap. |
| HA-002-AC08 - Shared categories and hierarchy | **partial** | Domain/Category.cs and DiscoveryCategories.cs; Infrastructure/HappyCityDbContext.cs; PostgresTests.Real_postgres_search_matches_literal_filter_contract. Current categories are shared and search/filter tests pass. Parent/type/order/active/timestamp model and corresponding tests are absent. | Existing shared categories avoid per-city duplication and preserve useful discovery. Name/key differences alone are not defects; UUID remains the AC03 conflict. |
| HA-002-AC09 - Category translations | **missing** | Domain/Category.cs stores Name directly; model/migration have no CategoryTranslation or language key. Dutch presentation works in existing browser tests; no translation storage/constraint tests exist. | A direct Dutch Name is adequate for current rendering, not equivalent to the required localization model. |
| HA-002-AC10 - City relationship integrity and scope | **partial** | PostgresDiscoveryRepository.cs; DiscoveryFilter.cs; HomeController.cs; HappyCityDbContext.cs; PostgresTests.Cities_and_drafts_are_isolated_and_queries_are_bounded and Mvc_uses_postgres_and_cannot_be_switched_to_another_city_by_query_string. Recorded tests prove current city/draft filtering and query-string resistance. District/location composite invariants cannot be tested because those entities are absent. | Current reads meet their narrower invariant. City IDs are not private authorization; no immediate requirement to add Identity or RLS. |
| HA-002-AC11 - Bounded efficient and injection-safe queries | **partial** | PostgresDiscoveryRepository.cs: AsNoTracking, escaped ILIKE, OrderBy, Take(100), Select, ToArrayAsync; DiscoveryFilter.cs; model indexes. Real search, bounds and cancellation tests pass; 68-test suite includes the NUL 400 regression. No page/cursor retrieval, representative SQL-plan, load or slow-query monitoring evidence. | Take(100) satisfies bounded reads but is not pagination. Six seed rows do not prove scalable substring-search indexes. No speculative caching/index changes justified. |
| HA-002-AC12 - Separated database privileges | **partial** | compose.yaml POSTGRES_USER=happy_migrator; infrastructure/postgres/01-runtime-role.sh; PostgresTests.Runtime_connection_uses_tls_and_cannot_write_or_create_tables. Recorded real test verifies TLS and denied DELETE/CREATE for happy_app. Bootstrap migrator is the development administrator; separate non-login owner/restricted migrator are absent. | Read-only runtime is correct and reusable. Its working restriction does not establish complete migration/administration separation. |
| HA-002-AC13 - Verified TLS and secret lifecycle | **partial** | DatabaseConfiguration.cs; pg_hba.conf; .gitignore; Load-LocalEnvironment.ps1; Initialize-LocalDatabase.ps1; run-browser-tests.cjs; appsettings.json. Real TLS, untrusted-root and plaintext-rejection tests pass. Credential-pattern scan has no findings. Ignored local files and separated child-process environment exist; rotation/production storage/backup secret lifecycle unverified. | Negative plaintext test intentionally probes rejection; production configuration remains VerifyFull. Local plaintext environment files are not production secret management. |
| HA-002-AC14 - Versioned migrations and environment recovery | **partial** | Infrastructure/Migrations/; HappyCityDbContextFactory.cs; Initialize-LocalDatabase.ps1; Program.cs; PostgresTests.Migrations_are_current_and_repeatable. Recorded clean CI bootstrap and repeated migration pass, six seeds remain, no pending migration/model changes. No production migration/environment recovery procedure is verified. | Current versioned migration is useful and preserved. Do not rewrite it to match UUID/catalog naming before independent migration review. |
| HA-002-AC15 - Synthetic data and milestone exclusions | **implemented** | SyntheticDiscoverySeed.cs; Views/Home/Privacy.cshtml and Index.cshtml; current source inventory and integration changed-file evidence. Six synthetic seed records and test fixtures; browser notices preserved. No real customer import or new identity/billing/deployment source in reviewed changes. | Implemented only as the current synthetic-data boundary; does not claim GDPR certification or implemented retention/export/deletion workflows. |
| HA-002-AC16 - Real PostgreSQL and foundation regressions | **implemented** | PostgresTests.cs, WebTests.cs; .github/workflows/build.yml; scripts/verify-browser.cjs, verify-security.cjs and network harnesses; committed integration evidence. Local and Ubuntu CI at ec5bcfd: 68 tests, 42 layouts, 190 security and five rate assertions passed; final PR run 44546174299 success. | Implemented for existing functionality, not comprehensive acceptance of absent catalog entities, accessibility/device coverage or production load. |
| HA-002-AC17 - Protected backup and demonstrated restore | **unverified** | Repository docs/scripts inventory has no backup-and-recovery.md or restore test; preserved Docker volumes and Git bundles are recorded in closure report. No actual database backup/restore execution evidence supplied or found. Procedure is missing; successful recovery is unverified. | Persistent volumes and source bundles preserve existing work but are not a demonstrated database restore. This is an HA-002 acceptance blocker, not permission to alter volumes. |
| HA-002-AC18 - Documentation, governance and independent acceptance | **partial** | ARCH-03 report; architecture/criteria files added by this consolidation; HA-001-CLOSE report; README/CHANGELOG; draft PR #3; preserved main/develop identifiers. V0.1 PR #2 merged after checks; integration merge preserves both parents. Remaining required database documents and independent HA-002 acceptance are absent. | Specifications are not implementation completion or approval. No automatic integration merge or new features; review PR #3 remains draft. |

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
- **Documentation:** docs/database/database-architecture.md and docs/requirements/HA-002-acceptance-criteria.md now exist as consolidated specifications. Required docs/database/{entity-relationship-diagram,data-dictionary,database-security,migration-policy,backup-and-recovery}.md, docs/decisions/DB-ADR-001-shared-database.md and docs/reports/HA-002-implementation-report.md remain missing as specified. Existing docs/V0.2-DATABASE-DISCOVERY.md supplies partial material. The baseline digest/gap report does not pretend to complete these implementation documents.
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


## HA-002-A recovery/design update (19 November 2026)

The original 18-item assessment above is the historical 0627839 checkpoint. Subsequent
Issue #4 work demonstrates protected backup and exact isolated restore of the synthetic
baseline, including negative fixture/archive checks. See [current implementation report](HA-002-implementation-report.md)
for the distinct AC01-AC12 matrix and [backup runbook](../database/backup-and-recovery.md).
Master AC17 moves from no restore evidence to **partial**: local synthetic restore is
proven, but broader backup identity/encryption/retention policy remains incomplete.
Master AC18 now has backup/migration policy and implementation-report paths; other
missing ERD/dictionary/security/ADR documentation and independent acceptance remain.
No UUID/schema/catalog/role/pagination gaps have been silently closed. HA-002-ARCH-01
is unavailable as a standalone reference, confirmed by owner; master requirements stay authoritative.
