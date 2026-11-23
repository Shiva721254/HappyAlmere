# Release evidence register

> HA-010-A documentation contract, 21 November 2026. [Authority](https://github.com/Shiva721254/HappyAlmere/issues/33#issuecomment-7065334379); [phase evidence and dispositions](../phases/HA-010-A.md). Documentation is not production certification, an operational exercise, or permission to process customer data.

PROD-A02/A10. Evidence catalogue links canonical requirement/decision/task IDs and phase status; it does not create competing acceptance decisions. Current [phase record](../phases/HA-010-A.md) and [Issue33](https://github.com/Shiva721254/HappyAlmere/issues/33) own HA010 actual outcomes. [ACCEPTANCE](../../ACCEPTANCE.md), [DECISIONS](../../DECISIONS.md) and [risk register](../security/security-risk-register.md) remain unchanged historical/canonical records.

| Evidence ID | Source / scope | Current attribution |
|---|---|---|
| HA010-E01 | [G04 authority](https://github.com/Shiva721254/HappyAlmere/issues/33#issuecomment-7065334379), rev1.1 and owner dispositions | Owner authority, not implementer approval |
| HA010-E02 | [Fresh twelve-point preflight](https://github.com/Shiva721254/HappyAlmere/issues/33#issuecomment-7065584407) | Codex read-only execution before branch/files |
| HA010-E03 | Exact7cb61f8/tree3f06c8c/366 entries and path/blob/mode candidate comparison | Fresh preflight baseline; final comparison must identify containing candidate |
| HA010-E04 | [Official-source register](../compliance/regulatory-applicability.md), ASVS matrix | Dated retrieval/limitations, no legal certification |
| HA010-E05 | Local scope/links/identifier/source/secret validation | Actual commands/counts supplied in phase record only after execution |
| HA010-E06 | Fresh normal push CI | PENDING until candidate exists; exact run/attempt/job/checkout and logs required |
| HA010-E07 | Fresh normal draft PR CI | PENDING until draft exists; inspect synthetic checkout/parents/tree separately |
| HA010-E08 | Downloaded sanitized candidate artifacts | PENDING; original bytes/digest, expiry, parser/content inspection and limitations required |
| HA010-E09 | [Identity acceptance](https://github.com/Shiva721254/HappyAlmere/issues/45#issuecomment-7042652323) | Historical bounded development acceptance; original failed attempt/cause unresolved |
| HA010-E10 | [City stop](https://github.com/Shiva721254/HappyAlmere/issues/29#issuecomment-7064250942) | Historical active STOP10/11; later local diagnostics not recovery success |
| HA010-E11 | Final Issue33/draft PR technical handover | PENDING PROD-G09; independentG10 and ownerG11/integrationG12 separate |

## Required record fields

Requirement/decision/task/scenario/threat IDs; base/head/actual checkout/tree/parent order; artifact digest and workflow ref; environment/tool versions/synthetic scope; exact command/time/exit; pass/fail/skip counts; observed warnings/findings and source availability; reviewer/owner decisions; limitations/residual and next action. Distinguish source inspection, local execution, CI execution, professional opinion and governance approval. No self-referential future commit hash in a file; containing commit and final CI results are identified through Git history and permanent Issue33/PR ledger.

## Unchanged candidate workflow

Existing workflow handles restore, Debug/Release, isolated original/forward/Identity recovery, disabled/enabled tests, NuGet listing, npm audit/source, trusted HTTPS browser/responsive/network and sanitized artifact upload. Preserve20-minute limit and all thresholds. Normal push and one draft PR only; no manual dispatch/rerun authority. Inspect each actual checkout, not only event headSha; synthetic merge is not an integration merge. Stop on mandatory failure; keep failed logs/artifacts, no retry-to-green.

Expected105 disabled and238 enabled test executions are historical expectations, not fresh results or343 distinct tests. Existing browser42 responsive/190 functional-security/5 rate expectations likewise not results. NuGet listing exit0 alone is informational. Capture actual dependency advisory coverage and restore enforcement separately.

## Retention and disclosure

Existing artifact retention7days is not permanent custody. Download exact originals and compute digests before expiry where authorized, inspect JSON/PNG/entry list for sensitive content, and publish only sanitized evidence. Temporary local storage is not a guaranteed archive; independent review should retrieve before expiry. No new provider/service/CI artifact wildcard or protected source changes. Original failed evidence remains intact; sanitize review copies, never replace historical evidence with a passing run.
