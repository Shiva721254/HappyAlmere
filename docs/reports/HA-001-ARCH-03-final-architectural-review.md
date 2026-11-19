# HA-001-ARCH-03 - Review provenance and owner authorization

Date: 19 November 2026 (Europe/Amsterdam).

This records the authoritative HA-001-ARCH-03 review supplied by the product owner
during HA-001-CLOSE on 19 November 2026. It is an attributed record, not a new
independent review by the implementing assistant. Reviewed branch: fix/ha-001-r02;
reviewed commit: cbe2deff565c84f775a7fecd6f298b9421bd91a6. No reviewer identity is invented.

The owner explicitly approved all four reported ARCH-03 recommendations:

1. Temporary acceptance of documented residual risks for local synthetic-data development only.
2. Conditional V0.1 approval of cbe2deff565c84f775a7fecd6f298b9421bd91a6, subject to successful remote CI.
3. Merge the verified foundation into main only after mandatory checks and branch protection pass.
4. Reconcile the existing V0.2 develop history on a dedicated integration branch, preserving both histories.

The same instruction authorizes investigation/correction of CI failures, regression
verification, closure reporting and a gap analysis of the existing database implementation.
It does not authorize additional database features, authentication, payments, public
deployment, real customer data, force-pushes or bypassing branch protection.

The original R02 CI run 44528299025 passed Ubuntu verification but its Windows job
was cancelled after an unattended trust dialog. This is not a successful exact-SHA
run. Closure corrects only CI certificate provisioning: export the ephemeral runner's
public dev certificate, import it into the ephemeral runner's machine trust store without a dialog,
and require dotnet's trust check before running browser tests. No application/test
source or TLS validation policy is weakened. Acceptance of the corrected descendant
is conditional on both jobs passing, as authorized by the owner's CI-correction instruction.

See [closure evidence](HA-001-CLOSE-final-report.md) for exact verified commits,
workflow results, merge and integration identifiers. The full review was supplied in the owner's clarification; the decision summary below
retains its acceptance conditions and limitations.

## Independent architectural findings (owner-supplied review)

- Retain .NET 10 LTS, C#, ASP.NET Core MVC/Razor, mobile-first CSS, selective JavaScript, modular layers, DI, application services and repositories; no rewrite/framework replacement.
- Approved dependencies: Domain none; Application -> Domain; Infrastructure -> Application + Domain; Web -> Application + Infrastructure; FoundationTests -> Web plus test dependencies. No cycles or infrastructure dependency in Domain.
- Review inspected the R02 report, official criteria/audit, permanent risk register, workflow, pinned npm/Node configuration, browser/network/static harnesses, Git comparison and existing V0.2 PR.
- Recorded local results were independently reviewed, not rerun: 33 .NET tests, 190 security assertions, five limiter assertions, 42 layouts at seven widths, 21 identical screenshots, loopback checks and clean scoped dependency/credential scans.
- AC01-AC13 accepted within local scope. AC14 requires formal owner disposition of temporarily accepted local synthetic risks. AC15 requires successful remote CI after conditional owner approval. No technical condition may be marked passed solely because authorization exists.
- SEC-F01/QA-F01/CODE-F01 local remediation accepted. QA-F02 remote CI still required. SEC-F02/PERF-F02 due before public deployment; SEC-F03 before public use; SEC-F04/SEC-F06 in HA-003; SEC-F05/PERF-F01/ARCH-F01 in HA-002 or relevant subsequent milestones; UI-F01/UI-F02 before commercial release; GOV-F01 in closure.
- Historical comparison: R02 four ahead/zero behind main; develop two ahead/four behind R02; common ancestor ebc99a0d9bf496821fc497222ac216388b700846. These are review-time values, not current comparisons.
- Existing PostgreSQL PR #1 reported 63 tests and persistence/TLS controls, but those capabilities were not independently accepted under ARCH-03.
- Recommendation: proceed with closure, preserve both histories and valid PostgreSQL work, regress and prepare the HA-002 gap analysis. Do not start another broad remediation cycle without a specific blocking defect or implement new database features before independent gap review.

Review limitations: static source/configuration/repository/evidence inspection only;
no independent runtime rerun, comprehensive penetration test or production-readiness
certification. The reviewer made no repository edits or merges.
