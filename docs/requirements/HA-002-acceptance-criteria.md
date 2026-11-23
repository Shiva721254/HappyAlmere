# HA-002 - Consolidated acceptance criteria

Date: 19 November 2026 (Europe/Amsterdam). Status: architectural specification for
independent gap review; implementation acceptance pending.

These 18 criteria consolidate the product-owner-supplied HA-002 master architecture
and requirements. The supplied master was organized by ADRs and sections, not an
explicit AC01-AC18 list in the available record. The numbering below is a transparent
repository consolidation, not invented original numbering, an independent review
of HA-002, or a claim that the architect approved this mapping. Each item traces to
its supplied source. HA-001 AC01-AC15 are a separate milestone and are not renumbered.

Read [database architecture](../database/database-architecture.md) for complete
logical fields, invariants, scope and future exclusions. Differences in physical names
are acceptable when they meet the underlying invariant; explicit UUID/schema decisions
require review if not met. No destructive migration or new feature implementation is
authorized by this document. Future features are conditional, not current vulnerabilities.

## Evidence and classification rules

- **implemented:** required current-scope behavior is present with source and applicable recorded verification.
- **partial:** some behavior exists but one or more required portions remain missing or unverified.
- **missing:** required immediate functionality is absent from inspected model/source.
- **conflicting:** an explicit architectural decision differs; functional equivalence and migration consequences must be reviewed.
- **unverified:** evidence is insufficient to establish the required outcome; documentation claims are not proof.

Each criterion has one overall classification in the gap report; subordinate missing,
conflicting or unverified elements remain explicit. No classification grants acceptance.

## HA-002-AC01 - Layered PostgreSQL architecture

**Requirement:** Retain MVC/.NET 10 and the approved dependency graph; EF Core/Npgsql persistence stays in Infrastructure, Domain stays independent. One shared relational database; no speculative NoSQL or whole-model JSON.

**Supplied source:** Master sections 2 (ADR-001/004/006/007), 15; ARCH-03 sections 1-2.

**Verification required:** Build both configurations, inspect project references and actual PostgreSQL repository registration.

## HA-002-AC02 - Reproducible isolated development database

**Requirement:** Use Docker PostgreSQL, persistent data/TLS volumes and authenticated private connectivity. Publish a host port only when required and only on loopback.

**Supplied source:** Master ADR-005 and sections 8.2, 10.

**Verification required:** Create an isolated CI instance, inspect actual host bindings, verify TLS access, preserve existing local volumes and histories.

## HA-002-AC03 - Functional catalog schema and identifiers

**Requirement:** Use the approved immediate catalog schema, UUID primary application entities and suitable composite relationship/translation keys. Introduce no unused future schemas.

**Supplied source:** Master ADR-002/003/009; sections 4-6.

**Verification required:** Inspect EF model and applied PostgreSQL migration/catalog; assert identifier types, keys and schema placement.

## HA-002-AC04 - Cities catalog

**Requirement:** Provide cities with all specified logical fields: UUID, bounded name/unique slug, country, language, timezone, active state and timestamps. Almere first; extensible cities and inactive-city exclusion.

**Supplied source:** Master section 5.1.

**Verification required:** Test uniqueness/bounds, valid country/language/timezone, inactive-city exclusion and adding a second synthetic city without service duplication.

## HA-002-AC05 - Trusted city domains

**Requirement:** Provide city_domains UUID/city FK, normalized unique hostnames, active and unambiguous primary flags/timestamps, trusted city resolution and canonical-domain policy. Future cities stay inactive until authorized.

**Supplied source:** Master section 5.2 and section 8.5.

**Verification required:** Test hostname normalization/uniqueness, active/primary selection, aliases/canonical routing and rejection of untrusted Host-derived context.

## HA-002-AC06 - Structured districts and hierarchy

**Requirement:** Provide district fields, city+slug uniqueness, same-city parent relationships, type/external code/active/timestamps and reject self-parenting/cycles.

**Supplied source:** Master section 5.3 and section 6.

**Verification required:** Actual PostgreSQL tests for unique city+slug, allowed cross-city name reuse, invalid parent city, self-parenting and cycles.

## HA-002-AC07 - Structured locations and coordinate integrity

**Requirement:** Provide structured address/country/BAG fields, city/district relationships and timestamps; district must share city; coordinates both absent or valid paired ranges.

**Supplied source:** Master section 5.4 and section 6.

**Verification required:** PostgreSQL tests reject cross-city district links, half coordinate pairs and invalid ranges; test valid optional data.

## HA-002-AC08 - Shared categories and hierarchy

**Requirement:** Provide reusable categories with UUID, unique slug, optional parent, type, display order, active state and timestamps; prevent self-parent/cycles while preserving discovery behaviour.

**Supplied source:** Master section 5.5.

**Verification required:** Test shared categories, slug uniqueness, ordering, hierarchy validity and existing search/category behaviour.

## HA-002-AC09 - Category translations

**Requirement:** Provide category UUID + language-code composite primary key, FK, bounded localized name and optional description, one translation per language; Dutch initially.

**Supplied source:** Master section 5.6.

**Verification required:** Test duplicate language rejection, category FK integrity and Dutch/second-language resolution without duplicated categories.

## HA-002-AC10 - City relationship integrity and scope

**Requirement:** Use validated city context for public reads and enforce applicable same-city relationships in the database. Distinguish public city scope from future private business authorization.

**Supplied source:** Master sections 6-8.5; ADR-001.

**Verification required:** Test public city/draft isolation, unknown cities and invalid cross-city district/location relationships. Private mutation authorization tests become mandatory only with those features.

## HA-002-AC11 - Bounded efficient and injection-safe queries

**Requirement:** Use parameterized bounded input, database filtering/sorting, async cancellation, no-tracking projection, appropriate pagination/indexes/pooling; inspect representative SQL plans and monitor slow queries.

**Supplied source:** Master sections 8.4, 9; PERF-F01/F02.

**Verification required:** Test literal hostile/metacharacter input, bounds, cancellation, projection/tracking; inspect plans on documented realistic synthetic workloads and record latency/round trips.

## HA-002-AC12 - Separated database privileges

**Requirement:** Separate runtime, schema ownership, migration and administration; prefer non-login owner and restricted migration identity. Read-only runtime is valid for current reads.

**Supplied source:** Master ADR-008; section 8.1.

**Verification required:** Real connections must prove runtime read access and denied writes/DDL; inspect owner/migration/admin grants and separation.

## HA-002-AC13 - Verified TLS and secret lifecycle

**Requirement:** Require authenticated private TLS with certificate verification, separate protected environment credentials, no committed secrets or sensitive SQL logs, and documented credential rotation/environment isolation.

**Supplied source:** Master sections 8.2-8.4, 10.

**Verification required:** Verify valid TLS plus bad-root/plaintext rejection, secret exclusions/scans, migration/runtime separation, logging minimization and rotation procedure.

## HA-002-AC14 - Versioned migrations and environment recovery

**Requirement:** Use reviewed versioned migrations, clean creation and predictable seeds; no privileged app-startup migrations, no destructive data/volume/history changes without approved recovery plan.

**Supplied source:** Master ADR-011; section 10.

**Verification required:** Create clean isolated PostgreSQL, migrate/reapply, inspect pending model changes and startup; document environment-specific migration/recovery controls.

## HA-002-AC15 - Synthetic data and milestone exclusions

**Requirement:** Use only synthetic seed/test data; preserve provenance. No real customers, identity/payments/imports/public deployment or speculative future schemas. Retention proposals remain subject to later legal/operational review.

**Supplied source:** Master ADR-009/012; sections 7, 12, 14.

**Verification required:** Inspect seed/content/import paths and tracked changes; verify local synthetic notice and absence of unauthorized feature work.

## HA-002-AC16 - Real PostgreSQL and foundation regressions

**Requirement:** Run actual PostgreSQL relational tests plus MVC/security/responsive/browser checks with verified HTTPS; retain reproducible pinned tools, dependency scans and least-privilege CI.

**Supplied source:** Master ADR-010; ARCH-03 closure conditions; master section 15.

**Verification required:** Record commands, exact commit/workflow URLs, counts and failures; distinguish actual runs from documentation claims and future missing-entity tests.

## HA-002-AC17 - Protected backup and demonstrated restore

**Requirement:** Document logical backup, protected/encrypted storage/access and recovery; restore into a separate environment and verify schema, constraints and representative data. Actual restore required for acceptance.

**Supplied source:** Master section 11.

**Verification required:** Execute and record an authorized isolated backup/restore drill with data/constraint checks and access controls.

## HA-002-AC18 - Documentation, governance and independent acceptance

**Requirement:** Maintain required database architecture/ERD/dictionary/security/migration/backup/ADR/criteria/implementation docs and README/changelog. Preserve both histories; independent gap review precedes new features and final HA-002 acceptance.

**Supplied source:** Master sections 13, 15; ARCH-03 sections 6, 9-11.

**Verification required:** Check required paths and factual consistency; record source/reviewer provenance, approved commits, protected merge/CI, preserved ancestry and outstanding independent review.

## Acceptance gate

All immediate requirements need adequate evidence and an independent disposition of
remaining gaps before HA-002 acceptance. Do not waive gaps because existing tests pass.
In particular, a successful database restore is mandatory. The current assignment
ends after controlled foundation integration and this gap analysis, not implementation.
