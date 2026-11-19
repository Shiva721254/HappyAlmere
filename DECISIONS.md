# Decision and ADR provenance index

## HA-003-A current execution update

Current branch `feature/ha-003-a`, exact accepted baseline `216df25c5656a979fcf67dfe3a16d07d7d66f154`. [IAM-G04 authorization](https://github.com/Shiva721254/HappyAlmere/issues/28#issuecomment-7023159754) and [approved #28 contract](https://github.com/Shiva721254/HappyAlmere/issues/28) supersede historical execution snapshots below only within the bounded persistence scope. [INT-G12 passed](https://github.com/Shiva721254/HappyAlmere/issues/27#issuecomment-7022990043); [IAM-G05 passed before implementation](https://github.com/Shiva721254/HappyAlmere/issues/28#issuecomment-7023218886).

IAM-A01–02 preflight complete; A03–07 entities/context/migration/roles/minimal DI implemented locally; A08–09 verification in progress; A10 draft PR/handover pending. Identity defaults off, with seven functional tables and separate context/history. Existing catalog/discovery source, migrations, original tests and performance thresholds are preserved. Exact authorized changes remain within the 34-path maximum. [Technical dictionary/runbook](docs/database/identity-foundation.md), [acceptance](ACCEPTANCE.md) and [handover](HANDOVER.md) own detailed evidence. Debug/Release builds pass with zero warnings/errors; 157 enabled .NET tests and all 105 original tests with Identity disabled pass (zero failures/skips). Eleven isolated database rehearsal checks, 42 responsive cases, 190 functional/security assertions and five rate assertions pass. NuGet/npm audits report no known vulnerabilities. Exact-head push/PR CI and independent review remain pending.

Owner remains migration coordinator and final approval authority; ChatGPT performs independent HA-003-A-ARCH-01 review. No merge, deployment, public auth endpoints, real accounts, business features or later Identity phases. IAM-OPEN-01–10 and IAM-PRE-D01/D02 are approved as recorded in #28; future cryptographic/session/resource policies remain deferred. Final commit, draft PR and push/PR CI are recorded in the GitHub submission ledger after actual synchronization; no self-referential commit hash is fabricated. Historical content below remains intact.

Documentation date: 19 November 2026. Canonical index, not a replacement architecture. Dates below are documentation dates unless the linked source states an actual decision date. Alternatives are explicitly comparative options, not invented deliberations. Authority is never inferred from implementation or CI.

## Original twelve database ADRs

Source: [supplied master digest](docs/requirements/HA-002-master-baseline.md), [consolidated specification](docs/database/database-architecture.md), recorded at 06278390ab4bfa625cf4a705abce17f68bf0a2c8, and [shared database ADR](docs/decisions/DB-ADR-001-shared-database.md). Original DB-ADR-001–012 identifiers retained. Owner-supplied architectural baseline; current development implementation subsequently reviewed in [ARCH-04](https://github.com/Shiva721254/HappyAlmere/issues/21) and [accepted](https://github.com/Shiva721254/HappyAlmere/issues/21#issuecomment-7021312167). Conceptual future schemas remain unimplemented. The third column contains rationale/consequences and labeled comparative alternatives, not a claim of original debate.

| Stable ID | Context and recorded direction | Alternative / rationale and consequence |
|---|---|---|
| DB-ADR-001 | Multi-city reuse; one shared PostgreSQL database with city-scoped records | Separate database per city deferred; reuse core architecture; public city scope is not private authorization |
| DB-ADR-002 | Separate responsibilities with functional schemas; create only milestone-required tables | Monolithic/unstructured or speculative wholesale schema creation does not meet the supplied direction; catalog/discovery now accepted within development scope |
| DB-ADR-003 | UUID primary application entities, suitable composite relationship/translation keys | Accepted deterministic UUID conversion retains unique archival integer identity; identifiers are not authorization secrets |
| DB-ADR-004 | EF Core/Npgsql in Infrastructure, Domain independent | Persistence leaking inward conflicts with layered boundaries; no stack replacement authorized |
| DB-ADR-005 | Reproducible Docker development, persistent private storage, necessary loopback ports only | Public database publication excluded; preserve existing volumes and TLS |
| DB-ADR-006 | Relational critical structures; selective JSONB for flexible metadata | Whole-model unstructured JSON excluded; no need to add speculative JSONB now |
| DB-ADR-007 | Additional NoSQL/analytics stores deferred until measured need | Premature extra databases increase complexity; no new storage product authorization |
| DB-ADR-008 | Separate ownership/migration/runtime/admin responsibilities, restricted roles | All-powerful runtime excluded; accepted development roles separate ownership, controlled migration, runtime and restricted backup/restore; production lifecycle remains deferred |
| DB-ADR-009 | Catalog-first, identity/commercial features deferred | Wholesale future inventory creation excluded; preserve useful existing discovery |
| DB-ADR-010 | Actual PostgreSQL relational tests | EF in-memory alone insufficient; test doubles may supplement real database verification |
| DB-ADR-011 | Reviewed versioned migrations; no privileged production startup migration | Rewriting applied history or destructive reset excluded; forward migration and recovery evidence required |
| DB-ADR-012 | Synthetic data until security/authorization/privacy controls exist | Real customer collection excluded; local prototypes are not production approval |

## HA-DB-D01–10 and eight safeguards

Actual decision date 19 November 2026; product owner explicitly approved [all decisions and safeguards](https://github.com/Shiva721254/HappyAlmere/issues/4#issuecomment-7014717337), based on recovery checkpoint 3287994. Context: preserve existing records while meeting master catalog/UUID/privilege requirements. Comparative alternative: destructive recreation or rewriting history would violate preservation; unrelated wholesale future schema creation remains excluded. Direction/rationale/consequences are recorded per decision below. Implemented evidence remains in [migration policy](docs/database/migration-policy.md) and [report](docs/reports/HA-002-implementation-report.md); accepted only in the subsequent development scope, not production.

| ID | Decision |
|---|---|
| HA-DB-D01 | Use the proposed deterministic entity-tagged UUIDv8 mapping for supported legacy integer IDs; retain unique legacy IDs and verify injectivity, FK mapping, boundaries and repeatability. New UUID generation must avoid collisions with reserved imported identifiers; do not reuse the import namespace across independent sources without review. |
| HA-DB-D02 | Move existing discovery content to `discovery.discovery_entries` using a data-preserving migration; retain all content, relationships, publication flags, legacy identity and compatibility. |
| HA-DB-D03 | Preserve legacy district display strings exactly; do not infer authoritative district relationships from text. Optional structured links remain unset without an approved mapping. |
| HA-DB-D04 | Leave unverified domains, structured addresses, districts and coordinates unpopulated. No live hostname activation, invented BAG IDs, geography or ownership data. |
| HA-DB-D05 | Set the existing synthetic Almere city's country/language/timezone to NL/nl/Europe/Amsterdam. Do not infer defaults for unknown future cities. |
| HA-DB-D06 | Use migration-generated timestamps with documented provenance; never represent these as recovered historical creation dates. |
| HA-DB-D07 | Separate non-login object ownership, controlled migration, read-only runtime, restricted logical backup, isolated restore operation and administration. No owner/migrator/admin membership for runtime; verify positive and negative permissions using actual connections. |
| HA-DB-D08 | Implement bounded deterministic keyset pagination with an explicit stable unique total order preserving legacy order and supporting future UUID-only records. Define null handling, tie-breakers, cursor validation and city/filter binding; test boundary cases, no duplicates/omissions and malformed/mismatched cursors. |
| HA-DB-D09 | Preserve original EF migration files and applied history; append a reviewed forward migration only. No database resets, destructive drop/recreate shortcut or application-startup privileged migration. |
| HA-DB-D10 | Rehearse controlled failure after backfill and before commit, prove transactional rollback and unchanged history, then independently restore a fresh isolated instance from the protected baseline and reconcile again. |

The following safeguards retain their original authorization-time wording and next actions as historical authority; later completion is assessed in ACCEPTANCE, not edited into the source instruction:

1. Refresh the current branch/PR/issue state and record exact baseline; coordinate with documentation PR #9 without merging or overwriting its work. Keep HA-002-A primary.
2. Before schema changes, document explicit category type/order/active defaults, Dutch translation and fallback semantics, pagination ordering/cursor contract and rationale. Preserve original category names and existing public discovery behavior; use only reviewed synthetic defaults, no invented taxonomy. Escalate material conflicts with the master specification before applying changes.
3. Follow the accepted six-table catalog scope and preserved discovery extension. Enforce same-city district/location relationships, hierarchy cycle prevention, unique keys, coordinate pairing/ranges, normalized domain constraints and inactive-city/publication filtering. Introduce no speculative future schemas or identity/business features.
4. Reconfirm the protected backup/restore gate for the exact isolated migration baseline; include additional synthetic unpublished records and representative data cases. Inventory dependencies; inspect generated SQL and use expand/backfill/validate/cutover in dependency order. Reconcile complete rows, mappings, associations and publication states before/after.
5. Verify clean creation, existing-data upgrade, second-run no-op, model drift, constraint negatives, least privilege and escalation negatives, verified TLS, externalized secrets and rotation procedure. Maintain existing security controls and regression tests.
6. Run actual PostgreSQL and browser/security/regression suites; define representative synthetic search workload and measurable performance thresholds before testing; publish query plans and timings with limitations. No production performance claims.
7. Execute migration failure and recovery rehearsal per D10; record sanitized evidence, exact commit SHA, commands, CI links, before/after reconciliation and recovery timing. Update existing ERD, data dictionary, database security, migration/recovery, ADR and acceptance documentation as required; keep the 18 master HA-002 criteria distinct from Issue #4 AC01–AC12.
8. Request independent review with criterion-level evidence for all outstanding requirements, including gaps that remain unresolved. Keep Issue #4 active; do not self-approve HA-002 or treat successful backup alone as migration acceptance.

## Review and governance decisions

All entries documented 19 November 2026. Review decisions belong to the attributed reviewer; owner decisions recorded by ChatGPT are not represented as personally authored or cryptographically signed owner comments.

| Stable ID | Context / direction | Comparative alternative, rationale / consequence | Status / authority / evidence |
|---|---|---|---|
| HA-001-ARCH-03 | Retain MVC/.NET/Razor/layers | Rewrite excluded; preserve foundation within local synthetic scope | Supplied independent decision; [review](docs/reports/HA-001-ARCH-03-final-architectural-review.md), [closure](docs/reports/HA-001-CLOSE-final-report.md) |
| HA-002-ARCH-01 | Referenced original standalone review unavailable | Fabricated reconstruction rejected; repository specs/reconstructed #4 are known basis | Unavailable provenance; [owner clarification](https://github.com/Shiva721254/HappyAlmere/issues/4#issuecomment-7013554143), not a recreated review |
| GOV-DEC-001 | GitHub durable shared source of truth | Private-chat-only state is not auditable; record exact SHA/evidence | Owner collaboration policy; [original authorization](https://github.com/Shiva721254/HappyAlmere/issues/5#issuecomment-7014228889), AGENTS |
| GOV-DEC-002 | One canonical document per responsibility, reuse risk register | Duplicate KNOWN_ISSUES/report would conflict; add navigation only | Historical #5 docs choice retained and expressly scoped by [#23](https://github.com/Shiva721254/HappyAlmere/issues/23#issuecomment-7022146146) |
| GOV-DEC-003 | Original governance branch from 3287994 | Concurrent primary edits avoided; now stale historical submission | Historical isolation decision at 871e7a3; [PR #9](https://github.com/Shiva721254/HappyAlmere/pull/9) preserved |
| HA-002-A-MAP-PROPOSAL | Original physical mapping proposal | Preserve source rather than recreate DB; proposal alone did not authorize changes | Historical proposal superseded by approved HA-DB-D01–10, not lost |
| GOV-ROADMAP-PROPOSAL | Original #5 numbered roadmap | No invented approved execution order; later #11–20 are published design references | Proposal preserved; final critical path/parallelism still HA-MASTER-EXEC-01 |
| HA-002-ARCH-04 | Five residual findings resolved in scoped technical review | No R03 justified; no blanket PostgreSQL/production certification | Independent ChatGPT [#21](https://github.com/Shiva721254/HappyAlmere/issues/21), exact 6da4b25; exclusions reviewed |
| GOV-DEC-004 | Accept 6da4b25 development scope, DEF-01–08 | Unconditional production pass rejected; private/production obligations survive | Actual owner decision 2026-11-19 [record](https://github.com/Shiva721254/HappyAlmere/issues/21#issuecomment-7021312167); supplies three formal gates and R02-AC06 |
| GOV-DEC-005 | Ordinary two-parent database-first integration | Squash/rebase would obscure accepted ancestry; governance first risks stale overwrite | Separate owner merge authority and actual merge recorded in [INT-G11](https://github.com/Shiva721254/HappyAlmere/issues/22#issuecomment-7022022047), PR #6; not new merge permission |
| GOV-DEC-006 | Reconstruct governance from exact 719dbcc | Blind merge of conflicting #9 rejected; three-way documentary reconciliation preserves history | Actual bounded owner [authorization](https://github.com/Shiva721254/HappyAlmere/issues/23#issuecomment-7022146146); eleven paths, synthetic CI, draft PR only |
| GOV-DEC-007 | Future migrations forward-only with one accountable coordinator | Uncoordinated snapshots/grants or applied-history rewrite rejected; protect recovery | Coordination rule authorized for documentation in #23; named coordinator/future assignments not yet commissioned |
| GOV-DEC-008 | STRATO-first; signing/verification deferred to pre-release | Unapproved cloud/home-server expansion and premature enrollment excluded | Existing owner constraint retained from [historical status](https://github.com/Shiva721254/HappyAlmere/blob/871e7a35c95b05b75b047fc1cc048f29e33b7cbc/PROJECT_STATUS.md); feasibility not verified by this task |

## Operational principles

Authorized as bounded governance specification by [#23 owner record](https://github.com/Shiva721254/HappyAlmere/issues/23#issuecomment-7022146146); independent governance acceptance remains pending. These principles do not independently authorize feature execution:

| ID | Principle |
|---|---|
| GOV-D01 | GitHub durable shared source of truth |
| GOV-D02 | One canonical document per responsibility |
| GOV-D03 | Preserve historical evidence and failed checkpoints |
| GOV-D04 | Attribute execution/review to exact commit |
| GOV-D05 | Separate implementation, CI, review, approval, integration and release |
| GOV-D06 | Explicit bounded owner authorization |
| GOV-D07 | Immutable accepted migration sources |
| GOV-D08 | Criteria-linked deferrals with mandatory future gates |
| GOV-D09 | Controlled branch integration, no history rewrite |
| GOV-D10 | Independent review; no implementer self-approval |
| GOV-D11 | Development acceptance not production readiness |
| GOV-D12 | Reproducible handover without private chat dependency |

DEF-01–08 are individually indexed with original criteria, role, rationale, destination, mandatory gate and owner authority in the [canonical risk register](docs/security/security-risk-register.md#approved-future-obligations). They are approved deferrals, never passed requirements or waived controls. [ACCEPTANCE](ACCEPTANCE.md) records their effect on original criteria.
