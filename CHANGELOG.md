# Changelog

Only observed Git history and actual local changes are recorded. Dates use Europe/Amsterdam.

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
