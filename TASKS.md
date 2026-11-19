# HappyAlmere task and dependency register

Snapshot 19 November 2026. GitHub issues are the live assignment/discussion records;
this file routes authorized work and dependencies, not a duplicate acceptance ledger.
Only [PROJECT_STATUS.md](PROJECT_STATUS.md) summarizes milestone acceptance state.

| Assignment / record | Implementer / reviewer / final authority | Scope and dependency | Next action |
|---|---|---|---|
| [Issue #4 HA-002-A](https://github.com/Shiva721254/HappyAlmere/issues/4), [PR #6](https://github.com/Shiva721254/HappyAlmere/pull/6) | Codex / ChatGPT / project owner | PRIMARY; recovery/design delivered at 3287994; all twelve criteria active; no forward migration accepted | Independently dispose mapping/default/role/pagination questions in the [design](docs/database/migration-policy.md), then obtain bounded implementation authorization |
| [Issue #5 HA-GOV-001](https://github.com/Shiva721254/HappyAlmere/issues/5) | Codex / ChatGPT / project owner | Documentation only, isolated docs/ha-gov-001 from 3287994; no source/schema/runtime changes | Review GOV-AC01 through 10 evidence; owner reconcile pre-existing closed/completed UI state with pending review/acceptance |
| [PR #3 integration](https://github.com/Shiva721254/HappyAlmere/pull/3) | Codex / ChatGPT / project owner | Preserved foundation + PostgreSQL histories; remains draft, no merge approval inferred | Review without discarding original develop or bypassing gates |
| [PR #1 original develop](https://github.com/Shiva721254/HappyAlmere/pull/1) | Existing implementation / ChatGPT / project owner | Historical database feature branch, original c2964ee preserved | Do not blindly merge in parallel with reconciled PRs; future owner-approved integration must preserve fixes |
| [PR #2 foundation](https://github.com/Shiva721254/HappyAlmere/pull/2) | Recorded closure / supplied ARCH-03 review / project owner | Completed protected foundation merge at 0e7739d, local synthetic scope | Preserve checkpoint/history; not permission for later merges |

## Documentation coordination

HA-GOV-001 uses its own worktree/branch and PR based on feature/ha-002-a at a fixed
checkpoint. The primary branch remains unchanged. No concurrent HA-002-A file edits
are performed by this governance task. Overlap is limited to README, HANDOVER,
CHANGELOG, AGENTS and the existing risk register; these are edited only in the isolated
governance copy. New indexes route to existing reports/specifications rather than
editing the database architecture, migration design, implementation report or evidence.

Before resuming HA-002-A, read latest Issue #4 comments and branch state. Any later
approved integration of governance documentation must inspect overlapping hunks and
retain newer HA-002-A evidence; no automatic merge/rebase/reset is authorized here.
The governance PR is not a prerequisite for continuing work already authorized under
Issue #4. Review outcomes must be recorded in GitHub; no approval inferred from silence.

## Update rule

At task start: inspect actual remote/local heads, reviews, open/closed issues, working
changes and canonical docs. At finish: update the relevant report, status and handover,
record scoped files/commit/test/evidence links, outstanding criteria and reviewer/owner
dispositions, and synchronize approved changes. Never close an assignment solely from
Codex self-assessment. See [AGENTS.md](AGENTS.md) and [documentation index](docs/README.md).
