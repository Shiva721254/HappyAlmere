# Production release gates

> HA-010-A documentation contract, 21 November 2026. [Authority](https://github.com/Shiva721254/HappyAlmere/issues/33#issuecomment-7065334379); [phase evidence and dispositions](../phases/HA-010-A.md). Documentation is not production certification, an operational exercise, or permission to process customer data.

PROD-A09; canonical [Issue20](https://github.com/Shiva721254/HappyAlmere/issues/20) and [master roadmap](https://github.com/Shiva721254/HappyAlmere/issues/24). These G01–15 master gates are distinct from this phase's PROD-G01–12. None passes merely because documents or CI exist.

| Gate | Required evidence | Present production disposition / owner |
|---|---|---|
| G01 Architecture | Exact release scope and approved decisions | PENDING release-specific architecture / architect+owner |
| G02 Baseline | Accepted dependencies and integrated immutable artifacts | Development7cb61f8 accepted; production candidate PENDING / independent reviewer |
| G03 Functional | Actual complete released workflows and disabled-future checks | PENDING production scope; synthetic regression only / engineering |
| G04 Database | Integrity/migration/roles/real protected recovery | Development evidence bounded; production PENDING / database coordinator |
| G05 Identity | Applicable auth/authz/lifecycle | Persistence only; private capability blocked DEF01/02 / identity owner |
| G06 Security | Independent assessment and resolved blockers | PENDING qualified testing / security lead |
| G07 Privacy | Lawful basis/rights/retention/provider evidence | PENDING actual mechanisms/contracts / privacy lead |
| G08 Infrastructure | Actual host/network/secrets/TLS | PENDING DEF03/04/08 / authorized operator |
| G09 Performance | Representative sustained/concurrent workload | PENDING DEF07; no SLA from synthetic p95 / engineering+operations |
| G10 Recovery | Independent protected copies/measured restore | PENDING DEF05/06 / independent recovery owner |
| G11 Commercial | Applicable financial transactions/reconciliation | Future disabled; release-specific N/A must be justified / finance/legal |
| G12 Legal | Current applicability/policies/professional conclusions | PENDING material legal/contract facts / qualified adviser+owner |
| G13 Accessibility | Actual manual+automated web/native evidence | PENDING / accessibility lead |
| G14 Operations | Named responders/alerts/rehearsed runbooks | PENDING appointments/exercises / operations owner |
| G15 Release | Independent recommendation and exact owner authority | NOT AUTHORIZED / product owner |

Release sequence: approved architecture -> bounded implementation -> exact candidate evidence -> independent review -> owner milestone acceptance -> separate integration/post-integrity checks -> applicable production gates -> owner authorization for exact immutable artifact/environment -> authorized deployment/postcheck/rollback decision. Green CI, merged code, issue closure or agent statements cannot substitute.

Every gate requires [evidence provenance](release-evidence-register.md), date, scope, actual result/failures/skips, reviewer and residual disposition. Applicable partial/failed/pending blocks release. Deferred/N/A needs rationale and tested inaccessible excluded feature; no counted pass. Only eligible medium/low residuals may be owner accepted with mitigation/expiry; no mandatory-control/legal waiver.

R0 synthetic; R1 anonymous public directory; R2 customer accounts; R3 business; R4 commerce; R5 native; R6 regional expansion are illustrative release scopes, not dates/authority. Public native may precede commerce after its own gates. HA010 phasesA documentation, B privacy, C security, D capacity, E incident/recovery, F legal/accessibility/professional review, G independent release recommendation each require separate authority. This assignment ends PROD-G09; no merge or production.
