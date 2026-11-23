# Roadmap and dependency gates

Documentation date 19 November 2026. This indexes [published master design #11](https://github.com/Shiva721254/HappyAlmere/issues/11) and milestone designs; it is **not an approved execution schedule**. Final critical path, parallelism and named migration coordinator belong to separately reviewed HA-MASTER-EXEC-01. [Status](PROJECT_STATUS.md), [tasks](TASKS.md), [acceptance](ACCEPTANCE.md) and [risks](docs/security/security-risk-register.md) remain distinct.

| Milestone / design | Existing foundation and dependency | Proposed next bounded direction / mandatory gate |
|---|---|---|
| HA-001 foundation | Accepted MVC/.NET/Razor/layers/security; [closure](docs/reports/HA-001-CLOSE-final-report.md) | Preserve approved foundation and regressions |
| HA-002 database / [#11](https://github.com/Shiva721254/HappyAlmere/issues/11) | Accepted catalog/discovery, original requirements unchanged | Preserve migrations; no rebuilding valid functionality; DEF-01–08 gates remain |
| HA-003 identity / [#12](https://github.com/Shiva721254/HappyAlmere/issues/12) | Accepted DB; identity lifecycle/resource policy design | Candidate foundation track, separate authorization; DEF-01 tested resource access before private accounts |
| HA-004 business / [#13](https://github.com/Shiva721254/HappyAlmere/issues/13) | Identity and organization/establishment/resource boundaries | Ownership/delegation/verification/publication policies; DEF-02 before private business operations |
| HA-005 discovery/search / [#15](https://github.com/Shiva721254/HappyAlmere/issues/15) | Reuse bounded current discovery, accepted catalog | Candidate search foundation; content publication/visibility and authorization when relevant; DEF-07 concurrent capacity |
| HA-006 multi-city / [#16](https://github.com/Shiva721254/HappyAlmere/issues/16) | Reuse existing city/domain/catalog constraints; no duplicate catalog | Candidate gap-only foundation; DEF-03 live host/proxy/TLS. Do not defer existing HA-002 invariants |
| HA-007 API/native / [#17](https://github.com/Shiva721254/HappyAlmere/issues/17) | Applicable identity, business, city and search services/security | Versioned contracts/mobile lifecycle only after approved service boundaries; no UI-driven authorization |
| HA-008 commerce / [#18](https://github.com/Shiva721254/HappyAlmere/issues/18) | Applicable identity/business/resource authority, city/search/API and operations | Reviewed catalog/entitlement/payment gates separately; no provider enrollment or billing now |
| HA-009 infrastructure / [#19](https://github.com/Shiva721254/HappyAlmere/issues/19) | STRATO-first feasibility, current local/CI controls preserved | Candidate feasibility track; DEF-03–08; no procurement, DNS changes or production configuration |
| HA-010 assurance / [#20](https://github.com/Shiva721254/HappyAlmere/issues/20) | All applicable available capabilities | Cross-cutting security/privacy/recovery/performance/evidence from each phase, not postponed to end; explicit release approval |

Identity/search/multi-city/infrastructure feasibility are candidate foundation tracks, not instructions to execute in parallel. Business depends on identity/organization boundaries; API/commerce depend on relevant services and security. Shared EF model/snapshot/history/grants require one accountable coordinator and approved migration sequence; no speculative future tables or uncoordinated concurrent migrations.

Next gates: HA-GOV-R01 independent review -> owner governance acceptance -> separate merge authorization -> verified integration/INT-G12 -> separately approved planning/assignment. No issue publication commissions a task. All eight obligations and criterion links remain in the [canonical deferral register](docs/security/security-risk-register.md#approved-future-obligations).

Microsoft business verification, Azure registration, Artifact Signing and production certificates wait until pre-release; internal tests are not blocked. Preserve future signing compatibility without installing or enrolling anything. STRATO-first remains preferred; cloud/home-server expansion needs separate approval. Prices, product feasibility and legal/commercial assumptions in designs are proposals requiring fresh verification before action.
