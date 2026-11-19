ï»¿# HA-002 migration policy and non-destructive design

Status: proposal for independent review under [Issue #4](https://github.com/Shiva721254/HappyAlmere/issues/4).
No new EF migration, primary-key change, ownership change or existing-structure
alteration has been executed. The original initial migration and snapshots remain
unchanged. The backup/restore gate is demonstrated; that does not approve this design.

## Authority and reconciliation

Use [database-architecture.md](database-architecture.md), DB-ADR-001 through 012,
the [18 master criteria](../requirements/HA-002-acceptance-criteria.md) and the
[existing gap analysis](../reports/HA-002-existing-implementation-gap-analysis.md).
HA-002-ARCH-01 is unavailable as a standalone verified reference, as confirmed by the
owner; no missing findings or original numbered wording are invented. Issue #4 has
its own AC01-AC12 and does not replace master requirements. The owner explicitly
prioritized inventory, backup, restore/integrity and design. Ambiguities below are
escalated to Issue #4 before forward implementation. Main/develop and their histories
remain unchanged; no production or main-merge authorization is implied.

## Proposed mapping (not an applied migration)

| Existing source | Intended target / preservation rule | Review needed |
|---|---|---|
| public.Cities, int Id, Name/Slug | catalog.cities with UUID id and retained unique legacy_id; preserve names/slugs verbatim; add master country/language/timezone/active/timestamps | Initial Almere settings NL/nl/Europe/Amsterdam are a proposal; unknown future values must not be inferred. New timestamps mean migration provenance, not invented historical creation dates |
| public.Categories, int Id, Slug/Name | catalog.categories with UUID id/legacy_id and unchanged slug; copy existing Dutch Name into category_translations(nl) while retaining original Name through compatibility phase | Confirm category_type, display_order and active defaults; do not infer parent hierarchies or erase Name before compatibility is proven |
| public.DiscoveryEntries, Id/CityId/CategoryId | Preserve all content, legacy identity, associations and IsPublished; add UUID identifiers/FKs, validate links before switching keys | Master requires preservation but does not fix this existing extension's final table/schema. Proposed discovery.discovery_entries needs review versus retaining public during transition |
| DiscoveryEntry.District string | Preserve the exact display text; optional linkage to catalog.districts only with reviewed synthetic mapping | A display string is not an authoritative district. Do not fabricate geography, parentage, type or external IDs; initially unlinked text is safer than an invented authoritative record |
| No CityDomains | catalog.city_domains per master, initially empty/inactive until domain policy authorized | Proposed happyalmere.nl/.com are not permission to publish or automatically activate a host resolver |
| No structured Districts/Locations | Define master fields/FKs/checks, initially empty if no approved source mapping | No invented addresses, BAG identifiers, coordinates or hierarchy; same-city integrity and cycle prevention require dedicated tests |
| No CategoryTranslations | Composite UUID category_id + language_code, bounded name/description; existing Dutch names may be copied losslessly | Review fallback/compatibility contract before changing current read behavior |
| public.__EFMigrationsHistory | Preserve existing row and original migration files; append a new forward EF migration only | Do not relocate/reset history or recreate applied migrations to hide conversion |

Physical snake_case/catalog requirements are authoritative targets; integer/public
models already work but conflict with explicit ADR decisions. Harmless property names
alone are not defects. DiscoveryEntries is useful existing functionality and must not
be deleted because it is outside the six initial catalog names.

## Legacy UUID mapping proposal

For reviewed nonnegative legacy integer IDs, reserve a documented UUIDv8 mapping
namespace per existing entity type (city=1, category=2, discovery=3), encoding the
legacy ID in the last 12 hexadecimal digits, for example legacy city 1:
`00000001-0000-8000-8000-000000000001`. This is a proposed custom deterministic UUIDv8
encoding (see [RFC 9562 section 5.8](https://www.rfc-editor.org/rfc/rfc9562.html#name-uuid-version-8)), not UUIDv5/hash-based generation or an authorization secret. It is injective
within the original int range and disjoint by entity tag. Retain legacy_id with a
unique constraint to make mapping auditable and keep legacy pagination order stable.
New records may use random UUIDs under separately reviewed defaults. Do not reuse this
namespace for another imported database without a namespace/isolation decision.

Alternative standard namespace-based UUID mapping is acceptable if independently
selected and implemented with deterministic cross-tool tests. No UUID algorithm is
implemented by this proposal. Approval must select the mapping before backfill.
Reject unsupported legacy IDs, duplicate mappings, unknown dependencies or inconsistent
foreign keys rather than guessing or deleting rows. Publish sanitized counts and
mapping checksums, not unrestricted production row exports.

## Ordered execution plan after design review

1. Verify immutable original migration hashes, exact source/target identity, successful
   protected backup/restore and captured baseline including unpublished data. Preserve
   raw source and use only an isolated restored copy for rehearsal.
2. Inventory all dependent constraints/indexes/views/sequences and role grants. Halt
   on dependencies not covered by the mapping. Capture schema/data/role reconciliation.
3. Scaffold a NEW EF migration from the reviewed model; inspect generated SQL. Replace
   any generated drop/recreate of populated entities with explicit expand/backfill/
   validate/rename operations as needed. Preserve original migration and history.
4. Add UUID/legacy mapping columns and new tables in dependency order: cities and shared
   categories; translations/domains; districts with same-city hierarchy rules; locations
   with same-city and paired coordinate checks; existing discovery UUID associations.
5. Backfill deterministic identity/FKs from old relationships, not names. Validate
   every row, uniqueness and orphan count before adding/not-null/promoting constraints.
   Preserve content and IsPublished bit-for-bit. Keep legacy IDs/order and compatibility
   columns through verified application cutover; no unexplained deletions permitted.
6. Promote reviewed UUID keys and adjust FKs/indexes only within the isolated rehearsal.
   Use a migration transaction where PostgreSQL supports it; reject nontransactional
   steps unless separately reviewed. Do not disable constraints/security to pass tests.
7. Apply ownership/grants through a separately reviewed privileged bootstrap: non-login
   owner, restricted migration login, read-only runtime and distinct backup/restore
   responsibilities. Runtime must not gain membership in owner/migrator/admin roles.
8. Exercise existing-data upgrade AND clean migration path. Run twice to prove expected
   no-op migration behavior with unchanged model/history, counts and relationships.
9. Preserve default discovery response and literal search. Propose bounded keyset
   pagination ordered by retained legacy sequence plus UUID tie-breaker (or an agreed
   stable unique order); validate cursor/filter/city scope and page boundaries. Do not
   silently reorder existing results because UUIDs compare differently from integers.
10. Inspect actual projected query plans with representative synthetic fixtures and
    justified indexes. Six-row baseline plans prove query execution only, not scale.
11. Inject a controlled failure after backfill but before migration commit in a dedicated
    restored copy. Verify transactional rollback and unchanged applied history. Separately
    recover a fresh instance from the protected baseline archive and reconcile again.
12. Record before/after counts, full content digests, catalog associations/publication
    distribution, unique UUIDs, legacy mapping checksums, migration history, privilege
    negatives, TLS, browser/regression results, plans and elapsed recovery times.

## Recovery and downtime policy

For current small isolated rehearsal, assume exclusive migration access and quiescent
writes. No production zero-downtime or SLA claim is made. Large-table locking, backfill
batching, concurrent application versions and index creation require measured separate
review. Failure recovery prefers a new instance restored from the verified baseline,
not destructive reverse SQL against populated source data. Do not treat a `Down()`
method as demonstrated recovery. Do not drop existing volumes or rewrite history.

Owner/schema/runtime/backup privileges and secret rotation are design requirements,
not implemented capabilities in this phase. Existing VerifyFull TLS, loopback binding,
external local secrets and read-only runtime stay intact. Do not introduce payment,
Identity, RLS, cloud storage or extra database products speculatively.

## Decisions requested in Issue #4

- Confirm the legacy UUID mapping and discovery extension's target schema/name while
  preserving legacy IDs and existing compatibility.
- Confirm that unverified district/address/domain data remains empty or unlinked rather
  than invented; approve category/city backfill defaults and timestamp semantics.
- Review role ownership/migration/bootstrap separation and pagination order/contract.
- Authorize the reviewed forward implementation/rehearsal after these decisions.

Until disposition, AC04 is partial and forward-migration acceptance remains open.
Successful baseline recovery is not self-approval of this design or full HA-002-A.
