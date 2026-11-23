# Threat model

> HA-010-A documentation contract, 21 November 2026. [Authority](https://github.com/Shiva721254/HappyAlmere/issues/33#issuecomment-7065334379); [phase evidence and dispositions](../phases/HA-010-A.md). Documentation is not production certification, an operational exercise, or permission to process customer data.

Model version HA010-A/1, baseline7cb61f8, reviewed2026-11-21 by Codex for documentation only. Source [architecture](../ARCHITECTURE.md), [database](../database/database-architecture.md), [Identity](../database/identity-foundation.md); normative [Issue20](https://github.com/Shiva721254/HappyAlmere/issues/20). Model changes never grant implementation authority.

## Actual data flow and trust boundaries

Public synthetic browser -> Kestrel/host filter/security headers/HTTPS -> MVC HomeController -> scoped DiscoveryService/filter -> repository EF projection -> TLS PostgreSQL catalog/discovery. Results -> typed ViewModel -> encoded Razor -> browser. Optional Identity managers/context have separate runtime credentials and tables, no sign-in endpoint. Tests alone use task-owned synthetic provisioning/backup credentials. No runtime database migration credential.

| Boundary | Asset and entry | Current / future distinction |
|---|---|---|
| TB-01 | Client/ingress: Host, path, query, headers, TLS | Current local Kestrel; actual proxy/public domain unavailable |
| TB-02 | Identity/request authority: session, account, city | Identity persistence only; accepted resolver public scope; HA006 immutable context pending |
| TB-03 | Resource/data/admin: projections, rows, delegation | Accepted DB least privilege; private business/account/admin flows absent |
| TB-04 | Providers/payments: messages, external identifiers | No live payment/notification/tracking integration; future signed event/idempotency boundary |
| TB-05 | Privileged operations/recovery: artifacts, keys, backups | Synthetic accepted harness; independent production custody and operator controls unresolved |

Untrusted input never selects an authorization identity, grants, connection string, deployment destination or executable branding. Global account identity, city context, organization membership and action authority are separate. Public UUIDs are identifiers, not secrets. Storage integrity includes same-city relationships, publication state and history.

## STRIDE application

Spoofing: THR01/02/05/12; tampering: THR10/13/16/22; repudiation: THR06/17/24; disclosure: THR04/07/11/14/19; denial: THR09/20/21; elevation: THR03/06/15/24. Threats may span categories. All24 original threats remain in [threat register](threat-register.md), with controls/test destinations and unresolved residuals.

Review on changed endpoint, schema, role, city/domain, provider, source of public content, mobile distribution, data purpose or release scope. Confirm actual topology and data paths, do not draw planned providers as deployed. Independent reviewer assesses completeness and risk-selected stronger ASVS controls. Owner accepts only eligible residuals through [policy](../governance/residual-risk-policy.md); no threat removal because a document exists.
