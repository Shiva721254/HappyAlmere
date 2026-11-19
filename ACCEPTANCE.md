# Acceptance traceability

## HA-003-A current candidate acceptance (not independent acceptance)

Sources: [#26 original criteria](https://github.com/Shiva721254/HappyAlmere/issues/26), [#28 approved compatibility additions](https://github.com/Shiva721254/HappyAlmere/issues/28), [IAM-G04](https://github.com/Shiva721254/HappyAlmere/issues/28#issuecomment-7023159754). Exact baseline `216df25c5656a979fcf67dfe3a16d07d7d66f154`; branch `feature/ha-003-a`. [Technical contract/runbook](docs/database/identity-foundation.md). Criteria retain separate identities and no automatic acceptance from authorization.

| ID | Requirement | Actual evidence route | Current disposition |
|---|---|---|---|
| IAM-A-AC01 | Baseline/authority | IAM-G04 + IAM-G05 comments; git ancestry/path audit | Preflight verified |
| IAM-A-AC02 | Compatible reviewed dependencies | Identity EF 10.0.14; EF/Npgsql graph; Debug/Release + NuGet audit | Local verified; CI pending |
| IAM-A-AC03 | UUID accounts/roles and mappings | IdentityPersistenceTests; independent SQL contract | Local verified; exact-head CI pending |
| IAM-A-AC04 | Infrastructure/layer boundaries | Preserved source verifier; unchanged project references | Local verified |
| IAM-A-AC05 | Dedicated history/physical schema | IdentityMigrationTests; runner independent contract | Local verified; exact-head CI pending |
| IAM-A-AC06 | Existing foundation preservation | Original 105 tests; protected blobs; runner original-state snapshots | Local verified; exact-head CI pending |
| IAM-A-AC07 | Least privilege | IdentityPrivilegeTests; runner full ACL/role/sequence matrix | Local verified; exact-head CI pending |
| IAM-A-AC08 | Exact denials and controls | 42501/28000 checks with live TLS positive controls; contract mismatch controls | Local verified; exact-head CI pending |
| IAM-A-AC09 | Actual relational invariants | IdentityPersistenceTests on actual PostgreSQL | Local verified; exact-head CI pending |
| IAM-A-AC10 | Clean/upgrade/no-op/recovery | verify-identity-foundation.py full state comparison | Local verified; exact-head CI pending |
| IAM-A-AC11 | Fresh original/new regressions and CI | Original suite default-off; full suite enabled; browser; push and PR CI | Pending |
| IAM-A-AC12 | Scope/docs/review submission | No auth surface; 34-path maximum; draft PR/handover | Pending submission; acceptance reserved |
| IAM-EXEC-AC01 | INT-G12/exact baseline | Issue #27 comment7022990043, exact216df25 | Verified |
| IAM-EXEC-AC02 | Seven functional + history | Independent SQL + actual metadata, IdentityMigrationTests | Local verified; exact-head CI pending |
| IAM-EXEC-AC03 | Complete physical contract | Column/PK/FK/index/ownership/sequence snapshots and mismatch controls | Local verified; exact-head CI pending |
| IAM-EXEC-AC04 | Concurrent email uniqueness | Concurrent_normalized_email_insert_is_rejected_by_database | Local verified; exact-head CI pending |
| IAM-EXEC-AC05 | Framework hashing/concurrency | Framework_managers_round_trip_hash_normalize_and_cascade_all_stores; Stale_framework_concurrency_stamp_rejects_update | Local verified; exact-head CI pending |
| IAM-EXEC-AC06 | Explicit DML/sequence rights | Full table/column/sequence effective privileges and actual framework operations | Local verified; exact-head CI pending |
| IAM-EXEC-AC07 | Discovery/backup exclusion | Positive_control_precedes_exact_authorization_denial and runner | Local verified; exact-head CI pending |
| IAM-EXEC-AC08 | Applied migration preservation | Protected blobs; original-state/history snapshots and original no-op tests | Local verified; exact-head CI pending |
| IAM-EXEC-AC09 | Identity-inclusive restoration | Coordinated shared-state restore and retired login tests | Local verified; exact-head CI pending |
| IAM-EXEC-AC10 | No public auth routes | IdentityArchitectureTests endpoint cases; no sign-in/cookie service | Local verified (account-route/DI tests) |
| IAM-EXEC-AC11 | Allowlist/no secrets/production | Scope diff; source/history scan; task-owned isolated resources | 34/34 approved paths; local scan passed; final CI pending |
| IAM-EXEC-AC12 | Exact-head handoff/review | New draft PR + actual push/PR logs/artifacts + review request | Pending |

### Scenario-to-test traceability

| Scenarios | Concrete verification |
|---|---|
| IAM-T01–03 | IdentityArchitectureTests.Enabled_services_resolve_without_sign_in_or_cookie_schemes |
| IAM-T04 | Unchanged verify-source.py dependency direction |
| IAM-T05–08,10–14,16–19,21–23 | IdentityPersistenceTests framework managers/physical role invariants |
| IAM-T09 | Concurrent_normalized_email_insert_is_rejected_by_database (one save, one23505) |
| IAM-T15 | Stale_framework_concurrency_stamp_rejects_update |
| IAM-T20 | Physical_constraints_reject_exact_invalid_operation (23503) |
| IAM-T24–28 | IdentityPrivilegeTests exact42501 with positive TLS controls |
| IAM-T29 | Runner private_probe actual denied access, finally removed |
| IAM-T30–34 | Runner clean/upgrade/full no-op/controlledP0001/fresh recovery/coordinated restore |
| IAM-T35 | Original105 tests disabled/enabled, browser harness, account endpoint404 tests |
| IAM-T36 | NuGet/npm audits, preserved source/history verifier, final scoped review |
| IAM-PRE-T01 | Both explicit factories used by runner and original tooling |
| IAM-PRE-T02 | Original105 tests with flagfalse, default-off service test |
| IAM-PRE-T03 | Enabled_invalid_configuration_fails_closed |
| IAM-PRE-T04–07 | IdentityPrivilegeTests and runner role/backup denial matrix |
| IAM-PRE-T08 | Framework claims create; exact sequence ACLs, setval/backup nextval42501 |
| IAM-PRE-T09 | Actual_child_processes_exclude_privileged_environment, both Node and PowerShell |
| IAM-PRE-T10 | Runner both-history/full-state restoration |
| IAM-PRE-T11 | Retired_identity_restore_login_denied_after_live_tls_control (Npgsql28000) |
| IAM-PRE-T12 | Persistence_does_not_publish_account_endpoints; no SignInManager/scheme |

All 36 original +12 compatibility scenarios are retained; grouped rows identify shared tests, not reduced criteria. Original parent IAM-AC01–12 (#7) remain distinct: A contributes persistence/layer/security/threat-test foundations only. Lifecycle/recovery endpoints, scoped roles/resource authorization, web/native sessions, MFA, business ownership, production secrets/audit/privacy and complete independent identity acceptance remain later gates. IAM-G01–05 prerequisites verified; G06–09 candidate verification/submission pending; G10–12 independent review, owner candidate acceptance and separately authorized integration remain pending. No parent HA-003 completion is claimed.


Canonical cross-milestone index. Definitions remain in original sources; no renumbering or replacement. Documentation date 19 November 2026. [Status](PROJECT_STATUS.md) summarizes milestones; [risk register](docs/security/security-risk-register.md) owns outstanding obligations.

## Evidence and authority

- HA-001 AC01–15: [criteria](docs/reports/HA-001-acceptance-criteria.md), [official audit](docs/reports/HA-001-acceptance-audit.md), [ARCH-03](docs/reports/HA-001-ARCH-03-final-architectural-review.md), [closure](docs/reports/HA-001-CLOSE-final-report.md); PR #2/main 0e7739d, local synthetic acceptance only.
- HA-002-A: [reconstructed Issue #4](https://github.com/Shiva721254/HappyAlmere/issues/4), twelve criteria; standalone ARCH-01 unavailable. Master [eighteen consolidated criteria](docs/requirements/HA-002-acceptance-criteria.md) derive transparently from supplied architecture, not authenticated original numbering. R01 [#10](https://github.com/Shiva721254/HappyAlmere/issues/10) and R02 [#14](https://github.com/Shiva721254/HappyAlmere/issues/14) are distinct sets.
- Per-criterion implementation and finding-to-test mapping: [report](docs/reports/HA-002-implementation-report.md), [gap history](docs/reports/HA-002-existing-implementation-gap-analysis.md), [R01 evidence](docs/reports/evidence/HA-002-A/r01/), [R02 evidence](docs/reports/evidence/HA-002-A/r02/), [exact-head final submission](https://github.com/Shiva721254/HappyAlmere/issues/14#issuecomment-7019523173).
- Independent source for every historical disposition below: [ARCH-04 #21](https://github.com/Shiva721254/HappyAlmere/issues/21) at **6da4b25e7a291e08b8fb0feab1b75ff22e55d0e0**. It inspected source/evidence and actual CI, not an independent runtime rerun or physical original-DB inspection. All five residual findings and original ten findings dispositioned within inspected scope; privilege exclusions explicitly scoped.
- Subsequent [owner decision](https://github.com/Shiva721254/HappyAlmere/issues/21#issuecomment-7021312167) accepts development scope, supplies **Issue #4 AC12, master HA-002-AC18, R01-AC12 and R02-AC06** owner disposition, and approves DEF-01–08. It does not pass master AC10/13/17 private/production obligations. Original 42 historical results remain **36 supported / 3 partial / 3 formal owner gates**. The additional eight R02 rows overlap: **50 rows are not a completion percentage**.

Tables retain historical review dispositions and append subsequent owner evidence to formal-gate rows. The owner-disposition rule above applies individually to the four named rows; all other rows retain scoped independent support. Their criterion-level technical evidence is the linked implementation matrix; no aggregate replaces row identity.

## Original Issue #4 — 12

| ID | Requirement | Final disposition |
|---|---|---|
| AC01 | Existing architecture, migration history, inventory | Supported |
| AC02 | Protected backup, integrity, version/prerequisites | Supported development scope |
| AC03 | Verified isolated restore before schema changes | Supported |
| AC04 | Approved target schema/catalog mapping | Supported |
| AC05 | UUID mapping and FK integrity | Supported |
| AC06 | Records/relationships/publication preservation | Supported |
| AC07 | Clean/upgrade migration, history, failure/rerun | Supported |
| AC08 | Distinct least privilege with negative tests | Supported |
| AC09 | External credentials/TLS/sanitized evidence | Supported inspected scope |
| AC10 | Functional regression/pagination/index/query plans | Supported |
| AC11 | Isolated failure/recovery runbook/rehearsal | Supported |
| AC12 | Evidence/tests/docs and independent review, final owner approval | Review-time owner gate pending; subsequently [owner development disposition supplied](https://github.com/Shiva721254/HappyAlmere/issues/21#issuecomment-7021312167) |

## Master HA-002 — 18

| ID | Requirement | Final disposition |
|---|---|---|
| HA-002-AC01 | Layered PostgreSQL architecture | Supported |
| HA-002-AC02 | Isolated development DB | Supported |
| HA-002-AC03 | Catalog schemas/UUID definitions | Supported |
| HA-002-AC04 | City configuration | Supported |
| HA-002-AC05 | Trusted city-domain resolution | Supported synthetically |
| HA-002-AC06 | District hierarchy | Supported |
| HA-002-AC07 | Structured locations | Supported |
| HA-002-AC08 | Shared category hierarchy | Supported |
| HA-002-AC09 | Category translations | Supported |
| HA-002-AC10 | City scope/relationship integrity | Partial — private authorization outstanding |
| HA-002-AC11 | Efficient bounded queries | Supported controlled workloads |
| HA-002-AC12 | DB privileges | Supported applicable scope |
| HA-002-AC13 | TLS/secret lifecycle | Partial — production lifecycle outstanding |
| HA-002-AC14 | Versioned migrations | Supported |
| HA-002-AC15 | Synthetic boundary | Supported |
| HA-002-AC16 | Actual PostgreSQL evidence | Supported |
| HA-002-AC17 | Backup/recovery | Partial — production recovery outstanding |
| HA-002-AC18 | Docs/final acceptance | Review-time owner gate pending; subsequently [owner development disposition supplied](https://github.com/Shiva721254/HappyAlmere/issues/21#issuecomment-7021312167) |

## HA-002-R01 — 12

| ID | Requirement | Final disposition |
|---|---|---|
| R01-AC01 | Ten findings evidence mapped | Supported |
| R01-AC02 | Trusted synthetic city resolution | Supported |
| R01-AC03 | Exact negative security verification | Supported |
| R01-AC04 | Intended migration failure/recovery | Supported |
| R01-AC05 | Restored metadata/permissions | Supported applicable scope |
| R01-AC06 | Concurrent hierarchy integrity | Supported |
| R01-AC07 | Actual query plans/physical definitions | Supported |
| R01-AC08 | Translation/future-table denial | Supported |
| R01-AC09 | Migration/data preservation | Supported |
| R01-AC10 | Fresh exact-head CI/regressions | Supported |
| R01-AC11 | Isolation/authorized scope | Supported inspected evidence |
| R01-AC12 | Independent final disposition | Review-time owner gate pending; subsequently [owner development disposition supplied](https://github.com/Shiva721254/HappyAlmere/issues/21#issuecomment-7021312167) |

## HA-002-R02 — 8

| ID | Final disposition |
|---|---|
| R02-AC01 | Satisfied — exact final push/PR CI and prior reconciliation |
| R02-AC02 | Satisfied within scope — privilege inventory/exclusions and revocation |
| R02-AC03 | Satisfied — SQL-only definition provenance and comparison |
| R02-AC04 | Satisfied — correction rollback/recovery and complete no-op |
| R02-AC05 | Satisfied — three matrices separately reconciled |
| R02-AC06 | Review-time documentation satisfied/owner pending; subsequently [owner approved all eight deferrals](https://github.com/Shiva721254/HappyAlmere/issues/21#issuecomment-7021312167), mandatory future gates retained |
| R02-AC07 | Supported within inspected evidence — scope/source/history/security preserved |
| R02-AC08 | Satisfied — exact-head final review requested and delivered here, no self-acceptance/merge/deploy |

## Integration gates — 12

Source [integration review #22](https://github.com/Shiva721254/HappyAlmere/issues/22); updated disposition [INT-G11](https://github.com/Shiva721254/HappyAlmere/issues/22#issuecomment-7022022047). Eleven completed or dispositioned, **not eleven fully independently verified**. G08 limitation survives. No main integration authority.

| ID | Requirement | Current evidence/disposition |
|---|---|---|
| INT-G01 | Independent technical acceptance | Satisfied ARCH-04 #21 |
| INT-G02 | Owner exact candidate acceptance | Satisfied owner comment 7021312167 |
| INT-G03 | Eight deferrals recorded | Satisfied; mandatory future gates not passed |
| INT-G04 | Exact accepted source HEAD | Verified 6da4b25 before integration; preserved now |
| INT-G05 | Exact target baseline | Verified 62b1def before integration; resulting 719dbcc is current |
| INT-G06 | No unresolved PR #6 conflict | Satisfied by controlled merge; no new merge inferred |
| INT-G07 | Exact candidate checks | Satisfied R02 push 44594959083 / PR 44594969490 |
| INT-G08 | Protections/reviewers/permitted merge method | Dispositioned with administrative endpoint HTTP403 limitation; not fully independently verified |
| INT-G09 | Explicit ready/merge authorization | Recorded by independent INT-G11 as explicit owner authorization for PR #6 only |
| INT-G10 | Integrated SHA/ancestry/tree | Verified two parents and identical 7965cf8 tree, 719dbcc |
| INT-G11 | Fresh integrated CI | PASSED per comment 7022022047; evidence below |
| INT-G12 | Governance reconciliation accepted/integrated | PENDING: review + owner acceptance + separate merge + post-integration verification required |

Historical integrated verification (not fresh governance tests): push [44610653646/job 135866023944](https://github.com/Shiva721254/HappyAlmere/actions/runs/44610653646/job/135866023944), checkout 719dbcc6f0d05f9d2f338f74b89cec8991a3aa6f, 105 passed / 0 failed / 0 skipped, 17s, artifact [14571393572](https://github.com/Shiva721254/HappyAlmere/actions/runs/44610653646/artifacts/14571393572); PR #3 [44610669585/job 135866083447](https://github.com/Shiva721254/HappyAlmere/actions/runs/44610669585/job/135866083447), synthetic c64fa6baa64c32ab191bb86a6bc1666adea403d2 with integration and main 0e7739d parents, 105 passed / 0 failed / 0 skipped, 16s, artifact [14571714639](https://github.com/Shiva721254/HappyAlmere/actions/runs/44610669585/artifacts/14571714639). Both 24/24 recorded steps successful. Reviewer inspected logs and enforced 250 ms database assertions; ZIP contents/browser counts and exact fresh DB p95 were not independently extracted. Privacy latency 4.75/2.47ms was not DB performance. No fabricated extraction; governance runs must supply their own evidence.

## Governance — original ten criteria

Source [#5](https://github.com/Shiva721254/HappyAlmere/issues/5); meanings retained. States below are implementer evidence, **independent review and owner governance acceptance pending**. Final post-push exact-head ledger is [Issue #23](https://github.com/Shiva721254/HappyAlmere/issues/23) and its new draft PR; no historical CI substituted.

| ID | Original meaning | Candidate evidence / remaining gate |
|---|---|---|
| GOV-AC01 | Inventory/baseline/canonical paths | docs/README inventory and eleven-path proof; review pending |
| GOV-AC02 | README current links/history labels | README and docs/README; historical records preserved |
| GOV-AC03 | Implemented/reported/verified/accepted distinct | PROJECT_STATUS, ARCH-04 and exact owner decision; no production acceptance |
| GOV-AC04 | Actual issue/decision dependencies, proposals labeled | ROADMAP/TASKS; HA-MASTER-EXEC-01 proposed |
| GOV-AC05 | Stable decision provenance, unavailable ARCH-01 | DECISIONS retains twelve ADRs, D01–10/safeguards; no invented original |
| GOV-AC06 | Known issues impact/owner/action | Existing risk register, DEF-01–08 and governance risks |
| GOV-AC07 | Distinct master and Issue #4 criteria/evidence | Four separate tables above, row-level owner disposition |
| GOV-AC08 | Restart without private chat | HANDOVER, exact refs/authority, existing reports and GitHub ledger |
| GOV-AC09 | Independent review/owner approval; no auto merge | AGENTS and GOV-G07–10 pending; protocol supplied, actual acceptance separate |
| GOV-AC10 | Actual links/consistency, protected scope, review request | docs/README validation and final GitHub submission; unchanged CI only under explicit expanded synthetic authorization |

## Governance — fourteen reconciliation criteria

Source [#23](https://github.com/Shiva721254/HappyAlmere/issues/23); all initially unverified, then evidence supplied individually. No self-acceptance.

| ID | Required evidence | Candidate disposition / evidence location |
|---|---|---|
| GOV-R01-AC01 | Exact baseline/governance provenance | Verified inventory, docs/README, 719dbcc / 871e7a3 / 3287994 |
| GOV-R01-AC02 | Eleven paths, five overlaps/six indices | Reconciliation table docs/README; one canonical responsibility each |
| GOV-R01-AC03 | Ten milestones/assignment states | PROJECT_STATUS and TASKS, future designs not execution |
| GOV-R01-AC04 | Ten GOV criteria individually linked | Original table above; source #5 meanings retained |
| GOV-R01-AC05 | Original 42 + R02 eight and owner identities | Four tables above; ARCH-04 and comment 7021312167 |
| GOV-R01-AC06 | Eight criteria-linked mandatory deferrals | Risk register DEF-01–08, owner/role/destination/gate |
| GOV-R01-AC07 | Twelve integration gates/CI/limits | Table above; G08 limitation and G11 actual evidence; G12 pending |
| GOV-R01-AC08 | Historical #9/reports/failures/ARCH-01 | Preserved branches and unchanged report/evidence tree, docs/README |
| GOV-R01-AC09 | Non-allowlisted content/migrations unchanged | Complete Git tree/blob/mode comparison, docs/README validation |
| GOV-R01-AC10 | Links/coverage/consistency/whitespace/secrets | Actual local validation ledger and external-reference limitations |
| GOV-R01-AC11 | Fresh exact-head push CI | Required after push; exact final results in Issue #23/new draft PR |
| GOV-R01-AC12 | Fresh PR CI checkout/two parents | Required after draft creation; exact final results in Issue #23/new draft PR |
| GOV-R01-AC13 | Isolated draft/preserved branches/settings/DB | Ref snapshots and scoped diff; no local DB access/settings change |
| GOV-R01-AC14 | Complete handover/independent review request | HANDOVER + final Issue #23/draft PR; independent/owner/integration gates remain pending |

## Governance approval gates

| ID | Gate | Authority |
|---|---|---|
| GOV-G01 | Exact baseline and scope | Codex |
| GOV-G02 | Eleven-document reconciliation | Codex |
| GOV-G03 | Historical/migration preservation | Codex, independent verification |
| GOV-G04 | Documentation validation | Codex |
| GOV-G05 | Fresh exact-head push/PR CI | GitHub Actions, independent verification |
| GOV-G06 | Complete handoff/criterion evidence | Codex |
| GOV-G07 | Independent review all 24 governance criteria | ChatGPT |
| GOV-G08 | Formal governance acceptance | Product owner |
| GOV-G09 | Explicit separate governance merge authority | Product owner |
| GOV-G10 | Post-integration integrity/applicable fresh CI | Independent reviewer |

G01–G06 are execution/submission gates, evidenced in docs/README and the final GitHub ledger. G07–G10 remain pending independent review, owner acceptance, separate merge authorization and post-integration checks. INT-G12 cannot close at draft submission or candidate acceptance alone. No issue/PR closure or merge from this task.
