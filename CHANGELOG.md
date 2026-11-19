# Changelog

Only observed Git history and actual local changes are recorded. Dates use Europe/Amsterdam.

## HA-002-A forward migration - 2026-11-19

- Implement owner-approved six-table catalog and preserved discovery UUID migration without rewriting initial history or modifying the original source database.
- Add bounded filter-bound keyset pagination and explicit catalog/inactive-city constraints; preserve Dutch discovery content and publication behavior.
- Rehearse failure rollback, fresh recovery, restricted backup/restore and separated database roles; add relational/performance evidence and CI coverage.
- Pin original initialization to its baseline; require explicit isolated environment for new application tests. Retain volumes and isolate governance PR #9.
- Independent acceptance remains pending; no identity/business implementation, merge or deployment.

## HA-002-A recovery/design - 2026-11-19

- Add protected synthetic-only PostgreSQL inventory/backup/isolated restore with exact reconciliation and negative input/archive checks; preserve the source and existing migrations.
- Verify 68 existing tests and HTTPS/browser regressions against the restored database; add the same recovery gate to CI.
- Document non-destructive migration mapping for independent review; forward migration, UUID conversion, role changes and pagination remain pending.
- Correct CI baseline-reference portability and record successful Linux recovery/regression workflows for c238b36.
- Record owner-confirmed unavailability of standalone HA-002-ARCH-01; use repository specifications without inventing findings.

## Shared handoff policy - 2026-11-19

- Record GitHub as the shared source of truth, independent ChatGPT review and final owner approval.
- Add root HANDOVER.md with implementation commits, existing evidence, unresolved gaps and the review boundary; extend existing instructions and closure progress record.
- Synchronize only approved scoped changes; preserve requirements/history and prohibit unapproved main merges or production deployments. No runtime changes.

## HA-001-CLOSE - 2026-11-19
- Merge verified foundation through protected PR #2 and preserve an approved checkpoint.
- Reconcile existing PostgreSQL history on integration/ha-001-v02, retaining security/layout/tests; no new database schema or feature.
- Map the supplied HA-002 master against the existing implementation; additional work awaits independent gap review.
- Record the owner's limited residual-risk acceptance and conditional V0.1 approval.
- Correct the unattended Windows CI certificate trust prompt without weakening TLS validation.
- Authorize protected foundation integration and history-preserving reconciliation; additional database features remain out of scope.

## Unreleased - HA-001-R02 - 2026-11-19
- Verify actual loopback listeners and local non-loopback connection rejection; clarify that Host filtering and noindex do not isolate a network service.
- Pin development-only Playwright/Chromium tooling with an npm lockfile and repeatable trusted-HTTPS Windows harness.
- Preserve responsive/security/rate coverage and add a Windows browser CI job with immutable action pins and read-only repository permissions.
- Format compressed Razor, CSS and configuration without changing application behaviour.
- Record the independent audit's 15 findings, future milestones and acceptance decisions in a permanent risk register.
- Verify the actual successful GitHub run for submitted R01 commit 7b6e082; revised R02 remote execution remains pending authorized push.
- No HA-002/HA-003, merge, public deployment or final V0.1 approval.

## Unreleased - HA-001-R01 - 2026-11-19
- Re-execute builds, 33 foundation tests, live security/rate checks and expanded 42-layout browser regression; publish separate fresh evidence and preserve failed sandbox attempts.
- Verify the HappyAlmere GitHub remote and retain independent review/owner approval gates.
- Correct long accepted text wrapping and intrinsic flex/grid sizing without hiding page overflow.
- Centralize category identifiers and reuse the application search-length limit in Razor.
- Partition discovery rate limits by normalized transport IP, preserving 120/minute and zero queue; supply Retry-After when available and ignore spoofed forwarding headers.
- Add peer-isolation, mapped-IP, forwarding-header and valid/tampered CSRF regression tests; expand browser coverage to all seven required widths and long-category boundaries.
- Add permanent engineering guidance, root security documentation, development instructions and acceptance/remediation evidence.
- Pin existing CI actions to verified immutable v6 checkout/v5 setup-dotnet refs and select ubuntu-24.04; remote CI execution is not claimed.
- This entry is not a release or architect/product-owner acceptance.

## V0.1 foundation - 2026-11-19 - ebc99a0
- Added root solution, SDK pin, shared build settings, VS Code tasks/debug profile and CI.
- Established service/repository boundaries, bounded discovery filter and typed view model.
- Strengthened security headers and antiforgery configuration; preserved HTTPS and HSTS.
- Added 31 foundation tests and original five-width browser smoke script.
- Added architecture, security and verification documentation.

## Original source checkpoint - 2026-11-19 - 9637633
- Preserved the original MVC workspace before foundation modifications.

## Branch provenance (not included in this remediation)
The existing develop branch contains bc79fb9 (branch workflow documentation) and
c2964ee (V0.2 database discovery), both dated 2026-11-19. HA-001-R01 branches from
ebc99a0 and does not include or modify those changes. Their existence does not
establish formal V0.1 acceptance. No development history or approval is inferred.

- HA-001-CLOSE reconciliation: 68 local and remote PostgreSQL-backed tests, 42
  responsive cases, 190 security assertions and five rate assertions passed; NUL
  input now returns 400 before database I/O. Draft PR #3 awaits independent review.

- Persisted the supplied independent ARCH-03 decision and consolidated HA-002 database
  specification/18 acceptance criteria at the requested paths. Updated the gap analysis
  with per-criterion source and recorded runtime evidence; no database feature changes.
