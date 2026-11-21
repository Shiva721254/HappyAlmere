# Assurance framework

> HA-010-A documentation contract, 21 November 2026. [Authority](https://github.com/Shiva721254/HappyAlmere/issues/33#issuecomment-7065334379); [phase evidence and dispositions](../phases/HA-010-A.md). Documentation is not production certification, an operational exercise, or permission to process customer data.

Scope: PROD-A01/A02/A08/A09, HA-PROD-D01–15. The approved application remains ASP.NET Core MVC/.NET10, Razor, layered Application/Domain/Infrastructure, dependency injection, PostgreSQL18/EF Core. This assignment adds assurance documentation only. The [canonical architecture](../../docs/ARCHITECTURE.md), [master roadmap](https://github.com/Shiva721254/HappyAlmere/issues/24) and accepted source remain unchanged.

## Evidence states

Configured means source declares a control. Inspected means an identified reviewer examined that source. Executed means a recorded command or experiment actually ran. Verified is bounded to its exact assertion/environment/candidate. Independently assessed and owner accepted require separate attributable decisions. Future/conditional/unresolved/N/A are never counted as passing tests. A scenario described in a policy has not thereby been executed.

Use [release evidence](../governance/release-evidence-register.md) for stable evidence identifiers and [phase record](../phases/HA-010-A.md) for current status. Shared decisions/acceptance/risks remain canonical; specialized registers explain controls, not competing acceptance decisions. No percentage derived from overlapping criteria.

## Scope boundaries

The accepted baseline contains anonymous synthetic discovery and optional Identity persistence, not login, MFA, private authorization, business claims, payments or native clients. Identity defaults disabled. HA-006-A remains stopped; its uncommitted implementation and resources are not incorporated. All eight DEF obligations survive. HA-005-A search and HA-009-A infrastructure are planning dependencies.

Retain layered defense: host/network, transport, Identity, resource/action authorization, validation/publication, database least privilege, secrets, monitoring/recovery. City scope cannot grant private authority. Use approved public projections, not entity serialization.

## Reliability contract

| Measurement | Binding status and evidence needed |
|---|---|
| Discovery | Existing 10,000 distinct synthetic records, 30 samples, p95 <=250ms; unchanged CI/regression contract |
| Identity authority | Existing p95 <=25ms, five warmups and40 samples per sync/async mode; unchanged |
| CI | Existing 20-minute job ceiling; no retries/threshold changes to manufacture success |
| FTS | Proposed <=250ms; HA-005 actual query/corpus and concurrency evidence still needed |
| City configuration API | Proposed p95 <=150ms; endpoint not implemented |
| Public discovery API | Proposed p95 <=350ms; API not implemented |
| Native initial results | Proposed2s on defined network/device; no native client |
| Availability | Proposed99.9% monthly;43.2min downtime in30days, not measured/SLA |
| Recovery | Proposed24h RPO/4h RTO; a4h outage exceeds that availability budget |

Before promises, approve endpoint SLIs, failure/timeout definition, observation window, hardware, data distribution, cache state, network, concurrent actors and writes, p50/p95/p99, saturation and errors. Proposed one-hour peak and longer soak require separate workload authority. Include simultaneous city traffic, expensive queries, cold/warm paths and recovery. Historical p95 failure255.1037ms remains a failure; authorized subsequent128.8041ms pass/max755.5450ms did not establish its cause or repair. Preserve [independent disposition](https://github.com/Shiva721254/HappyAlmere/issues/45#issuecomment-7042652323).

## Recovery assurance

HA-009 planning owns independent encrypted3-2-1-1-0 copies, single writable primary, protected deletion authority and keys. A green backup or checksum is insufficient: restore fresh isolated rows, constraints, indexes, routines, triggers, histories, roles/grants, artifact/config/assets/keyrings; compare independent expected definitions, run positive and intended-negative access tests, reconcile deletion/revocation and external financial/notification effects before access. Measure loss/time and obtain independent return-to-service recommendation/owner authority. Never replay external actions during restoration. Full production recovery remains unverified.

## Standards and independent review

ASVS5.0.0 L2 is the selected engineering baseline with risk-selected stronger requirements, not a certification claim; see [control matrix](security-control-matrix.md). MASVS/native profile remains conditional until HA-007; no obsolete assurance-level assumption. WCAG2.2AA is engineering, distinct from legal conformity. [Regulatory register](../compliance/regulatory-applicability.md) records current sources and limitations. Qualified professional security/legal/tax review remains separately commissioned. Documentation cannot waive law or mandatory controls.
