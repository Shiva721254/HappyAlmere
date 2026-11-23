# HappyAlmere engineering instructions

## Scope and architecture
- Use ASP.NET Core MVC on .NET 10 LTS, C# and Razor Views. Do not replace this stack without explicit architectural approval.
- Maintain Controllers, Application Services, Repository contracts/implementations, Domain Models and appropriate typed ViewModels. Introduce public DTOs when an approved API requires them.
- Approved project dependencies: Domain -> none; Application -> Domain; Infrastructure -> Application + Domain; Web -> Application + Infrastructure (composition root); FoundationTests -> Web. No cycles or inward infrastructure dependencies.
- Apply DRY, SOLID, KISS and separation of concerns. Centralize business rules and identifiers, use constructor DI, reuse Razor layouts/partials. Avoid speculative abstractions or dependencies.

## Security, privacy and content
- Preserve HTTPS, HSTS, host validation, CSP, security headers, CSRF protections, output encoding, bounded input and rate limiting. Never bypass certificate validation to pass tests.
- Keep secrets out of code, logs, reports and Git. Inspect only task-relevant files. Never include private tokens or credentials in evidence.
- No real customer data until storage security, authentication and resource authorization are implemented and tested. All current development uses synthetic data only.
- Do not present sample content as verified businesses or current offers. Preserve Dutch text, provenance, privacy notices and accessibility semantics. Do not invent historical changes or verification results.
- Use the socket peer for current rate limiting; never trust arbitrary forwarded headers. Proxy deployment requires a separately reviewed trusted-proxy policy.

## Frontend, backend and future evolution
- Use reusable mobile-first CSS. Verify 320, 375, 430, 768, 1024, 1440 and 1920 px, long accepted text, navigation and search. Do not conceal overflow globally.
- Keep request handling bounded and cancellation-aware. Await actual asynchronous I/O; do not use Task.Run or sync-over-async for database work. Measure performance with scope and limitations stated.
- PostgreSQL/EF Core are the accepted development storage foundation. Preserve indexed, bounded, projected, cancellation-aware reads and least-privilege verified TLS connections. No API/identity/database changes under governance reconciliation.
- Keep reusable services city-independent for future HappyUtrecht; future city-owned data must have tested isolation. Naming alone is not multi-city support.

## Verification and Git
- Inspect source, applicable instructions and Git status before edits. Preserve unrelated work; establish a recoverable checkpoint before changing existing source.
- Work on a scoped branch. Do not reset or rewrite unrelated branches. No remote push without user authorization; no production deployment without approval.
- Restore, build and execute relevant unit/integration and HTTPS browser tests after corrections. Check transitive vulnerabilities and dependency direction. Record exact commands, counts and failures; never reuse old results as fresh evidence.
- Keep README.md, SECURITY.md, CHANGELOG.md and architecture/development documentation accurate. Record remaining risks and unexecuted checks explicitly.
- Major architectural changes, destructive operations, expenses and deployment require explicit approval. Ordinary scoped corrections remain authorized by the assignment.
- Prior HA-001-CLOSE and HA-002 execution authorizations are historical. Current task authority and stop gates below control this branch; never approve governance gates on the owner's behalf.

## Shared source of truth and task handoff
- GitHub is the shared source of truth for Codex, ChatGPT and the project owner. Inspect repository state, HANDOVER.md, existing progress records and relevant GitHub reviews/checks before continuing work; do not infer completion from chat alone.
- After every development task, update the existing project documentation and progress records. Inspect the repository structure first and reuse equivalent documents instead of creating overlapping reports. Preserve existing documentation and architectural requirements.
- Maintain root HANDOVER.md with the latest implementation summary, completed work, modified files, actual test results and evidence, architectural decisions, outstanding issues, branch name and relevant implementation commit hashes. Link to detailed existing reports rather than duplicate their full contents.
- Attribute results to the exact tested commit and distinguish newly executed tests from historical evidence, queued CI, unverified claims and approval decisions. Never fabricate tests or acceptance. The containing documentation commit is identifiable through Git history; record its exact final SHA and remote verification in the PR/handoff response after pushing.
- Review changes for credentials, secrets, private customer data and environment secrets before staging. Stage only scoped approved work; never include sensitive values in source, logs, reports or Git.
- Commit and push approved changes to the appropriate authorized development/remediation branch; verify the remote SHA. This standing workflow authorizes synchronization of approved task changes, not unapproved features, unrelated changes or history rewriting.
- ChatGPT independently reviews implementation through GitHub. The project owner retains final approval authority. CI success and agent-authored reports are not substitutes for either review or owner approval.
- Do not merge into main or deploy to production without explicit approval. Preserve main, develop and other histories; never bypass required branch protection. Existing task-specific stop/review gates remain in force.

## Current bounded authority — HA-GOV-R01

- [Issue #23](https://github.com/Shiva721254/HappyAlmere/issues/23) and [owner authorization](https://github.com/Shiva721254/HappyAlmere/issues/23#issuecomment-7022146146) permit docs/ha-gov-r01 from 719dbcc6f0d05f9d2f338f74b89cec8991a3aa6f, scoped commits/push and a new draft PR into integration/ha-001-v02. Stop on moved baseline, incompatible branch, unsafe unrelated work, scope violation or failing verification; report without unauthorized remediation.
- Exactly eleven paths: README.md, AGENTS.md, CHANGELOG.md, HANDOVER.md, PROJECT_STATUS.md, ROADMAP.md, TASKS.md, DECISIONS.md, ACCEPTANCE.md, docs/README.md and docs/security/security-risk-register.md. No twelfth tracked file. Reuse the risk register as KNOWN_ISSUES; no duplicate report or architecture definition.
- No application/test/migration/model/workflow/config/dependency/evidence changes. Preserve all historical branches, accepted source and original data/volumes. No local/preserved/production database access. Only existing unchanged GitHub Actions push/PR verification against ephemeral isolated synthetic PostgreSQL is authorized, including backup/migration/recovery. No skip marker, weakened assertion, threshold change or protection bypass.
- Reconstruct documents using common checkpoint 3287994, historical governance 871e7a3 and accepted integration; do not merge/rebase old PR #9. Keep #1/#3/#9 unchanged, #3/#9 draft. No merge/deploy, permission changes, issue closure or additional feature work.
- Submit ten-task and 24-criterion evidence, exact final SHA, actual push/PR checkouts/parents, artifact counts/performance, preservation proof and independent-review request in Issue #23 and the new draft PR. GOV-G07–10 and INT-G12 remain separate reviewer/owner/integration gates.

## Governance and migration coordination

- Read [canonical index](docs/README.md), [status](PROJECT_STATUS.md), [tasks](TASKS.md), [decisions](DECISIONS.md), [acceptance](ACCEPTANCE.md) and HANDOVER at restart; refresh actual refs, comments, reviews/checks and local changes. Closed issue UI and published designs do not authorize execution.
- HA-002 development acceptance and DEF-01–08 are recorded in [Issue #21 owner decision](https://github.com/Shiva721254/HappyAlmere/issues/21#issuecomment-7021312167); partial obligations remain mandatory, affected features unavailable. Preserve the unavailable standalone HA-002-ARCH-01 provenance; never reconstruct it as authenticated review history.
- Before any separately authorized future migration, assign one accountable migration coordinator, pin the accepted baseline, coordinate shared EF context/snapshot/history and role grants. No concurrent uncoordinated migrations. Preserve InitialDiscovery, CatalogForward, SupportedRegions, designers, SQL, snapshot and verification contracts byte-for-byte; future changes forward-only with protected backup/restore, clean/upgrade/no-op/failure and regression gates.
- Published HA-003–010 and HA-MASTER-EXEC-01 proposals do not commission implementation. Preserve DRY/SOLID/KISS, public-city versus private-resource authorization boundaries, content provenance and security controls. Production assurance is cross-cutting, not postponed until release.
