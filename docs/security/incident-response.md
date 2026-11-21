# Incident and breach response contract

> HA-010-A documentation contract, 21 November 2026. [Authority](https://github.com/Shiva721254/HappyAlmere/issues/33#issuecomment-7065334379); [phase evidence and dispositions](../phases/HA-010-A.md). Documentation is not production certification, an operational exercise, or permission to process customer data.

PROD-A07; RUN-008/009/010/011. No actual incident, notification or rehearsal is declared by this document. Named commander, operator, privacy/legal lead, communications lead and substitutes are unresolved owner appointments; an ordinary privacy contact is not automatically a DPO.

| Severity | Trigger | Initial route |
|---|---|---|
| SEV1 | Credible serious data/key compromise or integrity loss | Immediate commander/security/privacy escalation |
| SEV2 | Significant outage or suspected privilege escalation | Urgent operator and security lead |
| SEV3 | Limited recoverable degradation | Assigned owner, bounded investigation |
| SEV4 | Minor routine issue | Tracked triage |

## Procedure and authority

1. Record detection, awareness timestamps/timezone, source, affected release/environment and uncertainty; don't wait for a deadline.
2. Establish incident commander and bounded access/containment authorization; preserve evidence before destructive steps where safe.
3. Restrict exposure with approved isolation/credential-revocation controls; don't erase evidence, reset databases or notify others without authority.
4. Maintain chain of custody: evidence ID, collector, time, digest, protected location, access log, redaction and legal hold. Keep secrets/customer records out of ordinary CI/Git.
5. Assess confidentiality/integrity/availability, affected people and likely harm; record every breach decision, including non-reporting rationale.
6. Separately assess each legal/contract regime below with qualified lead; prepare factual communication, uncertainty and follow-up schedule.
7. Eradicate under separate scoped remediation; restore independently verified state, reconcile revocations/deletions/payments and gain return-to-service authority.
8. Independent postmortem records root cause supported by evidence, unresolved causes, mitigations, owners and verification. A successful repeat is not causal closure.

## Distinct reporting regimes

[GDPR Articles33–34](https://eur-lex.europa.eu/legal-content/EN/TXT/?uri=CELEX%3A32016R0679): controller notification without undue delay, where feasible within72h of awareness unless unlikely risk; explain delay and supply phased information where allowed. Processor informs controller without undue delay. High-risk affected-person communication is without undue delay, subject to lawful exceptions. Document all personal-data breaches, including those not notified.

[NIS2 Article23](https://eur-lex.europa.eu/eli/dir/2022/2555/oj) uses a distinct significant-service-incident framework:24h early warning,72h notification, generally one-month final reporting, with special cases. [NCSC](https://www.ncsc.nl/cyberbeveiligingswet-nis2) reports Dutch Cbw effective15August2026. Actual entity scope, national provisions/addressee and trigger need confirmed legal screening before activation. GDPR and Cbw may both apply, one apply or neither. Do not substitute one timeline for the other. Provider/payment/contract obligations are separate.

## Future synthetic scenario contracts

PROD-T42: synthetic unauthorized personal-data disclosure at T0; record awareness and assess risk, competent authority,72h feasibility, phased information and Art34 independently. Negative control: documented unlikely-risk decision still retained. PROD-T43: synthetic qualifying service incident; establish entity/significance before24h/72h/final timeline. Negative control: GDPR-only incident must not invent NIS2 applicability. These are scenario designs, NOT EXECUTED table-top tests; no real notice or filing authorized.

[Audit contract](audit-logging-contract.md), [runbooks](../governance/operational-runbook-register.md) and [legal register](../compliance/regulatory-applicability.md) retain the future rehearsal and named-owner gates.
