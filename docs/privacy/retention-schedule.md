# Retention and restoration contract

> HA-010-A documentation contract, 21 November 2026. [Authority](https://github.com/Shiva721254/HappyAlmere/issues/33#issuecomment-7065334379); [phase evidence and dispositions](../phases/HA-010-A.md). Documentation is not production certification, an operational exercise, or permission to process customer data.

PROD-OPEN-15/16; all numerical engineering periods below remain proposals subject to purpose/necessity, applicable law, professional review and actual implementation tests. They are not implemented timers or blanket statutory rules. Token validity is not data retention.

| Data / trigger | Proposed period / status | Mechanism and exception requirement |
|---|---|---|
| Active account01 | Justified service duration | Periodic necessity review; no indefinite unused accounts |
| Deleted account01–04 | Operational target30days | Respect actual rights deadline, revoke sessions, restrict lawful hold |
| Unverified registration01 |30days | Erase unneeded record after trigger; no registration endpoint now |
| Email verification token03 |24h validity | One-use expiry/revocation separately from record cleanup |
| Password reset token03 |30min validity | No password/token value in logs |
| Operations16 |30days | Minimize fields, bounded rotation and access |
| Security16 |90days | Justified security purpose and incident hold review |
| Privileged audit17 |12months if justified | Protected access/integrity, lawful holds and disposal |
| Raw claim evidence07 |Within90days of resolution proposed | Earlier disposal when no longer necessary; justified disputes |
| Rejected/abandoned drafts05 |90days proposed | Notice, appeal needs and publication withdrawal |
| Leads13 |90days after resolution proposed | Purpose/dispute review, recipient propagation |
| Raw search10 |No persistent retention default | Check application/proxy/platform telemetry; request-only |
| Precise device position09 |No persistent retention default | Request-scoped option and manual alternative |
| Preferences/device11–12 |Until justified service/consent termination | Revoke stale identifiers and erase across devices/providers |
| Public content05/08 |Approved publication lifecycle | Withdraw public projection/index/cache; document lawful archive exception |
| Finance14–15 |Generally7years, applicable10year exceptions incl OSS | Confirm actual statutory record/trigger with tax adviser; not whole-account retention |
| Backups18 |HA009 proposed7daily/4weekly/12monthly, unresolved | Purpose/cost/legal assessment, independent encrypted custody; deletion reconciliation |
| Aggregates19 |Purpose-specific | Verify anonymity or retain personal safeguards |
| Secrets20 |Approved rotation/validity/recovery window | Retire access, controlled key overlap, secure disposal |

Official [Belastingdienst guidance](https://www.belastingdienst.nl/wps/wcm/connect/nl/administratie-bijhouden/administratie-bijhouden) and [fiscal digital-record guidance](https://download.belastingdienst.nl/belastingdienst/docs/geautomatiseerde_administratie_en_fiscale_bewaarplicht_al0401z13fd.pdf) refreshed2026-11-21; exact applicability/exception remains professional-review gate.

For every store approve purpose, trigger/timezone, period, accountable owner, deletion/anonymization job, access, legal hold/review/expiry and positive/negative verification. No new job/table or schema is authorized here. Restrict held data to its legal purpose; do not keep unrelated records.

Restore before serving: reconcile a separately protected minimized deletion/revocation ledger, publication withdrawals/suspensions and external state; fail closed if reconciliation unavailable. The ledger itself needs a basis, access and retention limit. Expiring restricted backups may retain deleted records until justified expiry but must not silently reintroduce them. Verify actual restored application behavior, not only backup success. Full mechanism remains future HA010-B/E and DEF05/06.
