# HA-002 master architecture - owner-supplied baseline

Source: master database architecture supplied in the HA-001-CLOSE clarification on
19 November 2026. This is a traceable requirements digest for gap analysis; implementation
acceptance is pending. The inventory is conceptual, not permission to create all tables.

| ADR | Required decision |
|---|---|
| DB-ADR-001 | One shared multi-city PostgreSQL database; distinguish public city scope from private authorization. |
| DB-ADR-002 | Functional schemas catalog, identity, business, discovery, marketing, billing, referral, content, integration, analytics, audit; introduce only approved milestone needs. |
| DB-ADR-003 | UUID primary application identifiers; composite keys for relationships/translations where appropriate; IDs are not access secrets. |
| DB-ADR-004 | EF Core/Npgsql; persistence in Infrastructure; Domain independent. |
| DB-ADR-005 | Docker development, persistent volumes, private networking, genuinely required host ports restricted to loopback. |
| DB-ADR-006 | Relational critical structures/relationships; JSONB selectively for flexible metadata/payloads, not the entire model. |
| DB-ADR-007 | Separate NoSQL/analytics stores deferred until measurable need. |
| DB-ADR-008 | Separate runtime, schema-owner, migration and administrative roles; prefer non-login owner and restricted login roles. |
| DB-ADR-009 | Catalog-first immediate milestone; identity/commercial features deferred. |
| DB-ADR-010 | Actual PostgreSQL integration tests, not an EF in-memory substitute. |
| DB-ADR-011 | Reviewed versioned migrations; no privileged production migration on normal startup. |
| DB-ADR-012 | Synthetic data only until storage security, authentication, authorization and privacy controls are implemented/tested. |

## Immediate six-table catalog

- **catalog.cities:** UUID id; required name(100), unique bounded slug, country_code CHAR(2), default_language(10), time_zone(100), is_active; created_at/updated_at TIMESTAMPTZ. Validate representations/timezone; initial Almere; inactive cities not public; extension without service duplication.
- **catalog.city_domains:** UUID id/city FK; unique normalized hostname, is_primary, is_active, created_at. One city per hostname; unambiguous primary. Proposed happyalmere.nl primary, happyalmere.com canonical redirect; future Utrecht inactive until authorized. Untrusted Host cannot select city.
- **catalog.districts:** UUID id/city FK/nullable parent FK; name, slug, optional external_code, district_type, is_active, timestamps. Unique city+slug; same-city parent, no self-parent/cycles; valid names reusable between cities.
- **catalog.locations:** UUID id/city FK/nullable district FK; street, structured house_number, optional suffix, postal_code, country_code, optional bag_identifier, latitude/longitude, timestamps. District same city; coordinates paired or absent, latitude [-90,90], longitude [-180,180]; avoid address duplication/private residential import. PostGIS deferred until justified.
- **catalog.categories:** UUID id/nullable parent FK; unique stable reusable slug, category_type, display_order, is_active, timestamps. No unnecessary per-city duplication/self-parent/cycles; preserve discovery behaviour.
- **catalog.category_translations:** composite category_id UUID + language_code(10) PK; bounded localized name, optional description; category FK; one translation per language, initial Dutch.

Cross-city relationships need enforceable relational constraints (e.g. composite keys/FKs);
FKs/UUIDs/city IDs do not establish private authorization. Exact physical names/optional
lengths and migration details remain review decisions; differences in names alone are
not defects. Preserve existing working discovery tables/data and applied migrations.

## Other acceptance requirements

- Separate schema owner/migration/runtime/future worker/reporting/backup identities; read-only runtime valid for read-only discovery. Authenticated private PostgreSQL with verified TLS. Protected environment-specific secrets, separate migration/runtime credentials and documented rotation.
- Parameterized bounded EF queries, reviewed raw SQL, literal search where applicable. Validated city context; test cross-city relationships; private authorization/RLS deferred until applicable, including pool/session safety review.
- Async I/O/cancellation, bounded database filtering/sorting, appropriate pagination, no-tracking projections, justified indexes/FKs/uniqueness, pooling, representative synthetic data and query plans. Concurrency for future writes; slow-query monitoring. Avoid N+1, unbounded loads, speculative distributed caches/indexes.
- Separate environments; reviewed reproducible migrations, clean creation, predictable seeds, persistent storage, recovery before significant changes. Never rewrite applied history/drop volumes without approved recovery/migration plan.
- Document logical backup/restore, protected/encrypted storage/access, separate restore environment, restored schema/constraints/data validation. **An actual restore verification is mandatory for HA-002 acceptance.**
- Only synthetic data now. Future retention proposals (accounts/logs/imports/backups 30 days, analytics 90 days, leads six months, security audit 12 months; financial statutory rules) require legal/operational review, not compliance claims. No BSN, medical records or unnecessary children's profiles.

Required documentation: docs/database/{database-architecture,entity-relationship-diagram,
data-dictionary,database-security,migration-policy,backup-and-recovery}.md;
docs/decisions/DB-ADR-001-shared-database.md;
docs/requirements/HA-002-acceptance-criteria.md;
docs/reports/HA-002-implementation-report.md; README.md; CHANGELOG.md.
Do not mark these complete unless they describe actual implementation accurately.

Future conceptual schemas include business organizations versus establishments/memberships,
Identity-compatible users/roles/consents, discovery events/activities/services/favorites,
promotions/coupons, billing, referral/affiliate, content/media/revisions, integration/imports/
outbox, analytics and audit. Favorites must use enforceable references; do not create a
duplicate custom authentication framework. These are deferred, not current missing-feature
implementation assignments. No authentication, payments, scraping, real customer data,
public deployment, production load tests or wholesale inventory creation is authorized.
