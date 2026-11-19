# HappyAlmere roadmap and dependency gates

Recorded 19 November 2026. This indexes existing authorized scope and the explicitly
unapproved proposal in Issue #5; it does not approve or renumber milestones.
Current status belongs to [PROJECT_STATUS.md](PROJECT_STATUS.md); actionable work
belongs to [TASKS.md](TASKS.md); acceptance sources are in [ACCEPTANCE.md](ACCEPTANCE.md).

## Established scope and dependencies

| Work | Source / owner | Dependency and permitted next action |
|---|---|---|
| HA-001 foundation, R01/R02 and ARCH-03 | [Closure and attributed decisions](docs/reports/HA-001-CLOSE-final-report.md); owner final authority, independent architect review | Foundation accepted for local synthetic development; preserve MVC/layers/security and approved checkpoint |
| HA-001-CLOSE | Same closure record; PR #2 and draft PR #3 | Foundation merge complete; reconciliation review remains separate from HA-002 acceptance |
| HA-002 database foundations | [Supplied specification](docs/database/database-architecture.md), [master criteria](docs/requirements/HA-002-acceptance-criteria.md) | Catalog-first; existing persistence reused; no assumption all conceptual schemas are authorized |
| HA-002-A recovery and migration | [Issue #4](https://github.com/Shiva721254/HappyAlmere/issues/4); Codex implementation, ChatGPT review, owner approval | Primary. Inventory/backup/restore/design verified; reviewed design dispositions precede forward migration. All twelve criteria remain active |
| HA-GOV-001 documentation | [Issue #5](https://github.com/Shiva721254/HappyAlmere/issues/5); owner authorized documentation only | Parallel side task on isolated branch; must not interrupt/change HA-002-A source or database architecture |
| Subsequent database-dependent implementation | Existing master/issue gates | Requires relevant HA-002-A/database acceptance and explicit bounded assignment; no implementation authorized by roadmap |
| Pre-release hosting/signing | Owner instruction recorded in HANDOVER | Microsoft business verification, Azure registration, Artifact Signing and production certificates deferred; not internal-development blockers. STRATO-first, other hosting separately approved |

HA-003 identity/Data Protection requirements appear as future deadlines in the existing
risk register. That does not establish approval of the entire numbered roadmap below
or authorize authentication implementation now. Non-mutating security/identity design
may be proposed separately; database-dependent implementation must wait for its gates.

## Issue #5 proposal - NOT APPROVED

| Proposed ID | Proposed theme | Reconciliation / boundary |
|---|---|---|
| HA-001 | Foundation | Existing ID retained; do not reinterpret accepted scope |
| HA-002 | Database architecture | Existing master retained; not full acceptance |
| HA-003 | Identity/access management | Future requirements exist; detailed implementation assignment still required |
| HA-004 | Business management | Provisional numbering/scope, owner confirmation required |
| HA-005 | Discovery/search | Provisional; working discovery already exists, do not rebuild it to match a future label |
| HA-006 | Multi-city architecture | Provisional and overlaps multi-city catalog already required in HA-002; do not defer those existing requirements to HA-006 |
| HA-007 | Versioned API | Provisional; no API implementation authorization |
| HA-008 | Commercial functionality | Provisional; no payments/subscriptions authorization |
| HA-009 | Infrastructure | Provisional; existing Docker/TLS controls already apply, no cloud expansion authorization |
| HA-010 | Production readiness | Provisional; no public deployment permission |

No independently approved complete roadmap for HA-004 through HA-010 was found.
The owner must reconcile numbering/scope and dependencies before these become assigned
milestones. Preserve original master requirements; new proposals never supersede them
implicitly. Use Issue #5 for that governance review, not invented historical approvals.
