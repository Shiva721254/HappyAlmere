# HappyAlmere decision and ADR index

Canonical decision/provenance index. Architectural requirements remain in their
existing authoritative documents; this index does not redesign or overwrite them.
Record date: 19 November 2026. Where the original decision date is not independently
available, this is the documentation date, not an invented approval date.

## Existing database ADRs

Source for all DB rows: [owner-supplied master digest](docs/requirements/HA-002-master-baseline.md)
and [consolidated architectural specification](docs/database/database-architecture.md),
recorded at 06278390ab4bfa625cf4a705abce17f68bf0a2c8. Status: documented architectural
baseline, **not implemented/accepted merely because an ADR exists**. Alternatives in
the table are comparison options or explicit exclusions, not a claim about original
reviewer deliberations. Actual gaps remain in the [gap analysis](docs/reports/HA-002-existing-implementation-gap-analysis.md).

| Stable ID | Context and recorded direction | Alternative / rationale and consequence |
|---|---|---|
| DB-ADR-001 | Multi-city reuse; one shared PostgreSQL database with city-scoped records | Separate database per city deferred; reuse core architecture; public city scope is not private authorization |
| DB-ADR-002 | Separate responsibilities with functional schemas; create only milestone-required tables | Monolithic/unstructured or speculative wholesale schema creation does not meet the supplied direction; immediate catalog schema still missing |
| DB-ADR-003 | UUID primary application entities, suitable composite relationship/translation keys | Existing integer keys are valid current integrity but conflict with explicit target; reviewed data-preserving conversion required |
| DB-ADR-004 | EF Core/Npgsql in Infrastructure, Domain independent | Persistence leaking inward conflicts with layered boundaries; no stack replacement authorized |
| DB-ADR-005 | Reproducible Docker development, persistent private storage, necessary loopback ports only | Public database publication excluded; preserve existing volumes and TLS |
| DB-ADR-006 | Relational critical structures; selective JSONB for flexible metadata | Whole-model unstructured JSON excluded; no need to add speculative JSONB now |
| DB-ADR-007 | Additional NoSQL/analytics stores deferred until measured need | Premature extra databases increase complexity; no new storage product authorization |
| DB-ADR-008 | Separate ownership/migration/runtime/admin responsibilities, restricted roles | All-powerful runtime excluded; existing read-only runtime useful but migration/admin separation incomplete |
| DB-ADR-009 | Catalog-first, identity/commercial features deferred | Wholesale future inventory creation excluded; preserve useful existing discovery |
| DB-ADR-010 | Actual PostgreSQL relational tests | EF in-memory alone insufficient; test doubles may supplement real database verification |
| DB-ADR-011 | Reviewed versioned migrations; no privileged production startup migration | Rewriting applied history or destructive reset excluded; forward migration and recovery evidence required |
| DB-ADR-012 | Synthetic data until security/authorization/privacy controls exist | Real customer collection excluded; local prototypes are not production approval |

Original independent approval metadata for a complete physical schema is not available;
exact physical mapping is still under Issue #4 review. The required separate
DB-ADR-001-shared-database.md implementation artifact remains absent; this index does
not pretend to supply an independently accepted physical design or new schema decision.

## Review, governance and proposed decisions

| ID | Record date / status | Context, alternatives and rationale | Consequence / evidence |
|---|---|---|---|
| HA-001-ARCH-03 | Supplied review dated 2026-11-19; V0.1 local-scope decision recorded | Retain MVC/.NET/Razor/layers; no rewrite. Static/evidence review, not independent runtime rerun | [Attributed review](docs/reports/HA-001-ARCH-03-final-architectural-review.md); owner conditional CI approval and [closure](docs/reports/HA-001-CLOSE-final-report.md) are distinct |
| HA-002-ARCH-01 | UNAVAILABLE standalone original; provenance gap | Referenced by Issue #4; owner confirmed no verified repository reference. Reconstructing original findings as authenticated history is prohibited | [Owner clarification recorded in Issue #4](https://github.com/Shiva721254/HappyAlmere/issues/4#issuecomment-7013554143); repository specifications used, no detailed findings invented |
| GOV-DEC-001 | 2026-11-19; owner-directed collaboration policy | GitHub shared source of truth instead of private-chat-only state; independent ChatGPT review and final owner authority | Existing [AGENTS.md](AGENTS.md) protocol; [Issue #5 authorization record](https://github.com/Shiva721254/HappyAlmere/issues/5#issuecomment-7014228889); no automatic merge/deploy |
| GOV-DEC-002 | 2026-11-19; authorized documentation implementation, independent review pending | Reuse risk register, evidence, architecture and criteria; add missing navigation/status/task/roadmap indexes rather than competing architecture docs | [Documentation inventory](docs/README.md); no database architecture change |
| GOV-DEC-003 | 2026-11-19; coordination choice, review pending | Separate docs branch from fixed primary checkpoint instead of editing active HA-002-A worktree | [TASKS.md](TASKS.md); primary remains feature/ha-002-a, overlapping documentation explicitly tracked |
| HA-002-A-MAP-PROPOSAL | Proposed, NOT accepted | Deterministic legacy UUID mapping, discovery placement/backfills, role separation and pagination need independent disposition; alternatives retained in existing design | [Unmodified migration proposal](docs/database/migration-policy.md), [Issue #4 review request](https://github.com/Shiva721254/HappyAlmere/issues/4#issuecomment-7014195658); no new architecture decision under governance |
| GOV-ROADMAP-PROPOSAL | Proposed, NOT approved | Issue #5 HA-001 through HA-010 sequence may overlap existing HA-002 multi-city requirements; owner numbering confirmation absent | [ROADMAP.md](ROADMAP.md); existing IDs/requirements preserved |

Do not assign an implementation/acceptance date to a proposal. New decisions must
state ID, date/provenance, context, alternatives, rationale, consequences, status,
source/evidence and who actually approved them; unknown facts remain unknown.
