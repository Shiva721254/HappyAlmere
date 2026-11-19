> **Current R02 status:** [Issue #14](https://github.com/Shiva721254/HappyAlmere/issues/14) and the [current implementation report](../reports/HA-002-implementation-report.md) govern verification closure. Expanded applicable privilege/grant-option inventory, independent SQL-only contract and correction-specific rollback/full no-op checks are verification-only. All existing migrations and original source remain immutable. Historical status below is superseded. Independent acceptance and explicit owner decisions on proposed production/private deferrals remain pending.

> **Current status (HA-002-R01, 19 November 2026):** The owner-authorized remediation in [Issue #10](https://github.com/Shiva721254/HappyAlmere/issues/10) is a review candidate on feature/ha-002-a. Earlier pending-design statements below are historical and superseded by the [current implementation report](../reports/HA-002-implementation-report.md). Existing applied migrations and original database remain preserved. No independent acceptance, merge or deployment is implied. HA-002-ARCH-01 remains unavailable as a standalone verified source.

# HA-002 - Consolidated database architecture

Source: master database architecture supplied in the HA-001-CLOSE clarification on
19 November 2026. This is a consolidated architectural specification based on the previously supplied
requirements, not an implementation report or new independent approval. Implementation
acceptance remains pending. The inventory is conceptual, not permission to create all tables.

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

## Business objective and architectural boundary

Support reusable city discovery, businesses and establishments, events, activities,
services, promotions, content, and later accounts, subscriptions, referrals, imports
and analytics. Almere is the initial market; additional cities must reuse the core
services. Retain ASP.NET Core MVC, .NET 10, C#, Razor and the approved modular layers.
Domain has no dependencies; Application depends on Domain; Infrastructure depends
on Application and Domain; Web composes Application and Infrastructure; tests reference
Web. Public DTOs are introduced only for an approved API. No architectural rewrite.

The shared database is not a shared private-authorization boundary. A business
organization may own establishments in several cities; future memberships control
private access. City identifiers, hostnames and UUIDs are never authorization secrets.

## Proposed logical relationships

Cities have many domains, districts and locations. Districts may have same-city child
districts and locations. Categories are shared, may have child categories and have
many translations. Composite unique keys and foreign keys may enforce same-city
relationships. Both district and category hierarchies must reject self-parenting and
cycles. The exact physical naming, optional lengths and migration mechanics require
review; equivalent names are acceptable when invariants are preserved.

## Future conceptual inventory (not immediate table-creation authority)

| Schema | Proposed concepts |
|---|---|
| catalog | Cities, CityDomains, Districts, Locations, Categories, CategoryTranslations, Tags |
| identity | Users, Roles, UserRoles, UserClaims, RoleClaims, UserLogins, UserTokens, UserPreferences, UserCityMemberships, PrivacyDocuments, ConsentRecords, DataSubjectRequests |
| business | Businesses, Establishments, BusinessMemberships, BusinessCategories, BusinessVerifications, BusinessContacts, OpeningHours, OpeningExceptions, BusinessMedia |
| discovery | Events, EventOccurrences, Activities, Services, ActivityCategories, EventCategories, Favorites |
| marketing | Promotions, Coupons, CouponRedemptions, Campaigns, Advertisements, AdvertisementPlacements, NewsletterSubscribers, NewsletterCampaigns, NewsletterDeliveries |
| billing | SubscriptionPlans, PlanPrices, PlanFeatures, Subscriptions, PaymentCustomers, Payments, Invoices, InvoiceLines, Refunds, PaymentWebhookEvents |
| referral | Leads, LeadRecipients, LeadStatusHistory, LeadCharges, AffiliatePartners, AffiliateOffers, AffiliateClicks, AffiliateConversions |
| content | Articles, Pages, ContentTranslations, MediaAssets, ContentRevisions, PublicationReviews, SeoMetadata, ExternalReferences |
| integration | DataSources, ImportJobs, ImportRecords, RawImportData, SynchronizationErrors, ExternalIdentifiers, OutboxMessages |
| analytics | AnalyticsEvents, DailyCityMetrics, DailyBusinessMetrics, DailyPromotionMetrics, SearchMetrics, RevenueMetrics |
| audit | AuditLogs, AuthenticationEvents, AdministrativeActions, SecurityIncidents, DataAccessEvents, RetentionJobs |

This is not a finalized physical schema. Reconcile future identity entities with
ASP.NET Core Identity instead of constructing duplicate authentication. Favorites
need enforceable references. RLS, PostGIS, Valkey/distributed caching and separate
NoSQL storage remain conditional future decisions, not current acceptance gaps.

## Security and operational detail

- Runtime access must not require superuser or uncontrolled migration permissions.
  A read-only runtime role is sufficient for current discovery. Introduce write,
  worker, reporting and backup grants only when needed and reviewed.
- Require authenticated private database connections and verified TLS where applicable;
  never disable certificate validation for tests. Host publication must be genuinely
  needed and loopback-only. Review any proxy or external hosting boundary separately.
- Keep environment-specific secrets outside Git, separate migration/runtime credentials,
  document rotation and use protected production secret storage. Suppress sensitive
  SQL parameters/error details. Review raw SQL and parameterize untrusted input.
- Use validated public city context and database-enforced relationships. Private-resource
  authorization tests belong to the milestone introducing those operations. Evaluate
  RLS and pooled session-context safety when justified, not speculatively.
- Preserve HTTPS, HSTS, host validation, CSP/headers, antiforgery, output encoding,
  bounded input, socket-peer rate limiting and responsive/accessibility controls.

## Query and lifecycle acceptance

Use asynchronous database I/O, propagated cancellation, database filtering/sorting,
bounded results, appropriate pagination, no-tracking projections, justified indexes,
unique invariants and Npgsql pooling. Inspect representative SQL plans with realistic
synthetic datasets; monitor slow queries. Define workloads and thresholds before
performance testing. Do not infer database scalability from six seed rows or localhost
sequential timings. Optimistic concurrency is required when relevant write workflows
are introduced. Avoid N+1, entire-table client filtering and speculative caching.

Maintain distinct development, test, staging and production environments. Use reviewed,
versioned migrations, predictable seeds and reproducible clean creation. Do not run
privileged production migrations automatically on application startup. Do not erase
volumes/data or rewrite applied migrations without an approved plan and verified
recovery checkpoint. A Git bundle is not a database backup.

Document logical backups, protected/encrypted storage and restricted backup access.
Restore into a separate test environment and verify schema, constraints and representative
synthetic data. A backup file alone is insufficient: an actual successful restore is
mandatory for HA-002 acceptance.

## Privacy and excluded work

Only synthetic data may be collected/seeded now. Future retention proposals are not
legal compliance periods: evaluate legal, contractual and operational obligations
before adopting them. Export, correction, consent and deletion belong to relevant
later milestones. No BSN, medical data or unnecessary children's profiles.
No real registration, production authentication, payments/subscriptions, scraping,
complete analytics, public deployment, production load testing or wholesale future
schema creation is authorized.

## Acceptance and reconciliation

[The 18 consolidated acceptance criteria](../requirements/HA-002-acceptance-criteria.md)
trace this specification to verification requirements. [The existing implementation
analysis](../reports/HA-002-existing-implementation-gap-analysis.md) is separate from
this intended design. Existing discovery entities, migrations, TLS, roles and repository
behaviour must be reused where compatible. Differences require invariant-based review,
not automatic replacement. No new HA-002 feature implementation is authorized before
independent gap analysis review. Original develop and approved V0.1 histories remain
preserved on the dedicated integration branch.


## HA-002-A recovery/design evidence (requirements unchanged)

Issue #4 and the owner's clarification authorize a recovery-first sequence. A verified
synthetic baseline backup/restore is now documented in [backup-and-recovery.md](backup-and-recovery.md).
The proposed [migration-policy.md](migration-policy.md) maps existing data to this
architecture and identifies decisions still requiring independent review. It does not
change these requirements or claim a new schema/UUID conversion has been implemented.
HA-002-ARCH-01 has no verified standalone record; no detailed findings are invented.
