# HappyAlmere engineering instructions

## Scope and architecture
- Use ASP.NET Core MVC on .NET 10 LTS, C# and Razor Views. Do not replace this stack without explicit architectural approval.
- Maintain Controllers, Application Services, Repository contracts/implementations, Domain Models and appropriate typed ViewModels. Introduce public DTOs when an approved API requires them.
- Approved project dependencies: Domain -> none; Application -> Domain; Infrastructure -> Application + Domain; Web -> Application + Infrastructure (composition root); FoundationTests -> Web. No cycles or inward infrastructure dependencies.
- Apply DRY, SOLID, KISS and separation of concerns. Centralize business rules and identifiers, use constructor DI, reuse Razor layouts/partials. Avoid speculative abstractions or dependencies.

## Security, privacy and content
- Preserve HTTPS, HSTS, host validation, CSP, security headers, CSRF protections, output encoding, bounded input and rate limiting. Never bypass certificate validation to pass tests.
- Keep secrets out of code, logs, reports and Git. Inspect only task-relevant files. Never include private tokens or credentials in evidence.
- No real customer data until storage security, authentication and resource authorization are implemented and tested. This V0.1 uses synthetic data only.
- Do not present sample content as verified businesses or current offers. Preserve Dutch text, provenance, privacy notices and accessibility semantics. Do not invent historical changes or verification results.
- Use the socket peer for current rate limiting; never trust arbitrary forwarded headers. Proxy deployment requires a separately reviewed trusted-proxy policy.

## Frontend, backend and future evolution
- Use reusable mobile-first CSS. Verify 320, 375, 430, 768, 1024, 1440 and 1920 px, long accepted text, navigation and search. Do not conceal overflow globally.
- Keep request handling bounded and cancellation-aware. Await actual asynchronous I/O; do not use Task.Run or sync-over-async for database work. Measure performance with scope and limitations stated.
- PostgreSQL/EF Core are the future storage direction. Database/API/identity work is not part of V0.1 remediation. Future reads require indexed, bounded, projected queries and least-privilege verified TLS connections.
- Keep reusable services city-independent for future HappyUtrecht; future city-owned data must have tested isolation. Naming alone is not multi-city support.

## Verification and Git
- Inspect source, applicable instructions and Git status before edits. Preserve unrelated work; establish a recoverable checkpoint before changing existing source.
- Work on a scoped branch. Do not reset or rewrite unrelated branches. No remote push without user authorization; no production deployment without approval.
- Restore, build and execute relevant unit/integration and HTTPS browser tests after corrections. Check transitive vulnerabilities and dependency direction. Record exact commands, counts and failures; never reuse old results as fresh evidence.
- Keep README.md, SECURITY.md, CHANGELOG.md and architecture/development documentation accurate. Record remaining risks and unexecuted checks explicitly.
- Major architectural changes, destructive operations, expenses and deployment require explicit approval. Ordinary scoped corrections remain authorized by the assignment.
- HA-001-CLOSE is authorized: repair CI, finalize conditional V0.1 acceptance only after mandatory checks, merge through protected PR, and reconcile existing V0.2 history on a dedicated integration branch. Preserve develop; do not implement additional database features. Stop after closure and HA-002 gap analysis for independent GitHub review. Never approve governance gates on the owner's behalf.

## Shared source of truth and task handoff
- GitHub is the shared source of truth for Codex, ChatGPT and the project owner. Inspect repository state, HANDOVER.md, existing progress records and relevant GitHub reviews/checks before continuing work; do not infer completion from chat alone.
- After every development task, update the existing project documentation and progress records. Inspect the repository structure first and reuse equivalent documents instead of creating overlapping reports. Preserve existing documentation and architectural requirements.
- Maintain root HANDOVER.md with the latest implementation summary, completed work, modified files, actual test results and evidence, architectural decisions, outstanding issues, branch name and relevant implementation commit hashes. Link to detailed existing reports rather than duplicate their full contents.
- Attribute results to the exact tested commit and distinguish newly executed tests from historical evidence, queued CI, unverified claims and approval decisions. Never fabricate tests or acceptance. The containing documentation commit is identifiable through Git history; record its exact final SHA and remote verification in the PR/handoff response after pushing.
- Review changes for credentials, secrets, private customer data and environment secrets before staging. Stage only scoped approved work; never include sensitive values in source, logs, reports or Git.
- Commit and push approved changes to the appropriate authorized development/remediation branch; verify the remote SHA. This standing workflow authorizes synchronization of approved task changes, not unapproved features, unrelated changes or history rewriting.
- ChatGPT independently reviews implementation through GitHub. The project owner retains final approval authority. CI success and agent-authored reports are not substitutes for either review or owner approval.
- Do not merge into main or deploy to production without explicit approval. Preserve main, develop and other histories; never bypass required branch protection. Existing task-specific stop/review gates remain in force.

## Current HA-002-A assignment
- Issue #4 and the owner's clarification authorize inventory, protected backup, isolated restore/integrity and non-destructive migration design on feature/ha-002-a. Reconcile against repository architecture; HA-002-ARCH-01 has no verified standalone reference and must not be invented.
- Record the recovery gate before any primary-key, ownership or existing-structure changes. Escalate ambiguous migration mappings in Issue #4 for independent review. All twelve acceptance criteria remain active; baseline restore is not forward-migration completion.
- Report progress, actual test results, commits and criterion-level evidence directly in Issue #4. Preserve main/develop/source data and all prior approval/deployment restrictions.


## Approved HA-002-A forward phase
- Owner-approved HA-DB-D01 through D10 and eight safeguards are recorded in Issue #4 comment 7014717337. Forward implementation and isolated synthetic rehearsals are authorized on feature/ha-002-a; this supersedes the prior design-only stop for that task.
- Preserve original database/volumes, initial migration/history and main/develop. New schema changes execute only on verified fresh clones, with exact enriched backup/restore gate, failure rollback and independent recovery.
- Keep governance PR #9 isolated; do not overwrite or merge it. No HA-003/HA-004, identity/business features, live domains, real data, merge or deployment. Final independent acceptance remains pending.
