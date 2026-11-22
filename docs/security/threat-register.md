# Threat register

> HA-010-A documentation contract, 21 November 2026. [Authority](https://github.com/Shiva721254/HappyAlmere/issues/33#issuecomment-7065334379); [phase evidence and dispositions](../phases/HA-010-A.md). Documentation is not production certification, an operational exercise, or permission to process customer data.

PROD-A05; model HA010-A/1; reviewed2026-11-21. Normative IDs/names from Issue33; [model](threat-model.md) defines the five boundaries. S/T/R/I/D/E are STRIDE labels, not severity. Each row is a prospective threat, not a confirmed exploit. Likelihood is UNASSESSED pending actual exposed feature/topology; impact is loss of the named asset's confidentiality, integrity or availability. Preconditions: the named feature becomes accessible and its listed control fails. Accountable milestone roles below are proposed; owner must appoint named operators. Residual acceptance is NOT GRANTED for any row. Reassess each at its milestone commissioning and before affected release.

| ID / original threat | Boundary / STRIDE | Asset | Control and current scope | Required scenario / owner milestone |
|---|---|---|---|---|
| THR-01 Credential compromise | TB-02 / S | account secrets | Public login absent; future MFA/anti-enumeration | PROD-SEC02 / HA-003 |
| THR-02 Recovery abuse | TB-02 / S/E | recovery authority | Future bounded one-use recovery and session revocation | PROD-SEC03 / HA-003 |
| THR-03 Session replay/revocation | TB-02 / S/E | session authority | Future server-tracked revocation; persistence is not sessions | PROD-SEC03 / HA-003/007 |
| THR-04 Cross-account access | TB-03 / I/E | private rows | Deny-by-default resource authorization before publication | PROD-SEC04 / HA-003/004 |
| THR-05 Fraudulent organization claim | TB-03 / S | organization authority | Separate registration, email control and representative verification | PROD-SEC06 / HA-004 |
| THR-06 Unauthorized delegation | TB-03 / E/R | delegation/audit | Explicit grant ceiling, scope and attributable changes | PROD-SEC06 / HA-004 |
| THR-07 Private evidence exposure | TB-03 / I | private claim evidence | Separate private evidence store/access from public media | PROD-SEC10 / HA-004 |
| THR-08 SQL injection | TB-03 / T/I | catalog data | Parameterized EF queries; retain hostile input checks | PROD-SEC07 / HA-005 |
| THR-09 Expensive FTS exhaustion | TB-01 / D | query availability | Current bounds/rate limiter; future FTS workload/timeout tests | PROD-SEC23 / HA-005/009 |
| THR-10 Cursor tampering | TB-01 / T | pagination integrity | Current bounded city/filter cursor; signed opaque search cursor future | PROD-SEC07 / HA-005 |
| THR-11 Cross-city leakage | TB-03 / I/E | city data | Current public filters; HA006 recovery/isolation stopped; no private authority | PROD-SEC05 / HA-005/006 |
| THR-12 Host impersonation | TB-01 / S | host/city identity | Explicit AllowedHosts/database resolver; trusted proxy/live domains future | PROD-SEC11 / HA-006/009 |
| THR-13 Malicious branding/media | TB-01 / T/I | published content | Encoded Razor; future typed media/branding validation | PROD-SEC08 / HA-004/006 |
| THR-14 Mobile token/storage exposure | TB-02 / I | native credentials | Native app absent; future platform secure storage/token lifecycle | PROD-SEC03 / HA-007 |
| THR-15 API object auth error | TB-03 / E/I | API objects | Public/private API absent; explicit DTO/resource policies required | PROD-SEC04 / HA-007 |
| THR-16 Forged/replayed payment | TB-04 / S/T | financial state | No payment integration; future verified events/idempotency/reconciliation | PROD-SEC20 / HA-008 |
| THR-17 Entitlement abuse | TB-04 / E/R | entitlements | Server-authoritative periods/source, no membership from payment | PROD-SEC21 / HA-008 |
| THR-18 Paid organic manipulation | TB-04 / T | ranking trust | Organic independent of commercial state; separate labeled sponsorship | PROD-SEC21 / HA-005/008 |
| THR-19 Production secret compromise | TB-05 / I/E | keys/credentials | Protected synthetic env; actual production rotation/recovery DEF04 | PROD-SEC14 / HA-009/010 |
| THR-20 Public DB/management | TB-05 / E/D | database/admin | Loopback development; real production IPv4/IPv6 vantage unresolved | PROD-SEC12 / HA-009 |
| THR-21 Backup/key destruction | TB-05 / T/D | recoverability | Independent encrypted retained copy/key custody not implemented | PROD-SEC22 / HA-009/010 |
| THR-22 Supply-chain compromise | TB-05 / T | source/artifact | Pinned workflow and audits; future artifact signing/SBOM/independent retest | PROD-SEC15 / HA-010 |
| THR-23 Restored deleted data | TB-05 / I/T | deletion/revocation | Protected reconciliation before restored access; mechanism future | PROD-SEC19 / HA-009/010 |
| THR-24 Unauthorized release | TB-05 / E/R | release authority | Separate candidate review/owner/integration/release gates | PROD-SEC16 / HA-010 |

Link controls to [ASVS matrix](security-control-matrix.md), [scenario dispositions](../phases/HA-010-A.md) and original PROD-RISK/EXEC/R11 IDs in the phase record. All24 remain OPEN for release-level assessment; source-aligned development tests do not eliminate production threats. Threats involving absent features remain future obligations with those features inaccessible. No independent penetration testing has been performed by this phase.
