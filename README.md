# HappyAlmere — accepted development foundation

ASP.NET Core MVC, .NET 10 LTS, C#, Razor and mobile-first CSS; layered modular monolith with EF Core/Npgsql/PostgreSQL persistence. Preserve controller/service/repository/domain boundaries and synthetic data only.

[Status](PROJECT_STATUS.md) · [Roadmap](ROADMAP.md) · [Tasks](TASKS.md) · [Decisions](DECISIONS.md) · [Acceptance](ACCEPTANCE.md) · [Documentation index](docs/README.md) · [Handover](HANDOVER.md)

The product owner accepted HA-002 development scope at **6da4b25e7a291e08b8fb0feab1b75ff22e55d0e0** in the [formal decision](https://github.com/Shiva721254/HappyAlmere/issues/21#issuecomment-7021312167), following [ARCH-04](https://github.com/Shiva721254/HappyAlmere/issues/21). PR #6 integrated that candidate at **719dbcc6f0d05f9d2f338f74b89cec8991a3aa6f**; both trees equal **7965cf8397c7ff5b9345da8333b5bc179424be4e**. [INT-G11](https://github.com/Shiva721254/HappyAlmere/issues/22#issuecomment-7022022047) verifies integrated CI. This is not production readiness or authorization for another feature.

Current assignment: [HA-GOV-R01](https://github.com/Shiva721254/HappyAlmere/issues/23), documentation only on **docs/ha-gov-r01**, under [recorded owner authorization](https://github.com/Shiva721254/HappyAlmere/issues/23#issuecomment-7022146146). Final review, owner governance acceptance, separate merge approval and INT-G12 remain pending. Original PR #9 is preserved; PR #3 remains draft/unmerged into main.

The [risk register](docs/security/security-risk-register.md) carries all eight approved deferrals and mandatory gates. Private authorization, live domain/proxy/TLS, production secrets, backup encryption/retention, measured RPO/RTO, concurrent capacity and operational readiness are outstanding. No real customers, Identity/business implementation, deployment or procurement is authorized.

## Development and historical records

Use [development instructions](docs/DEVELOPMENT.md), [database architecture](docs/database/database-architecture.md), [migration policy](docs/database/migration-policy.md), [backup/recovery runbook](docs/database/backup-and-recovery.md), [ERD](docs/database/entity-relationship-diagram.md), [dictionary](docs/database/data-dictionary.md) and [security](docs/database/database-security.md) for technical details. Accepted migrations and original volumes are immutable under this task. Do not point the new application at the preserved old schema. Runbooks describe capability, not authorization to execute local database operations during governance work. Only unchanged ephemeral GitHub Actions synthetic CI is authorized here.

The [implementation report](docs/reports/HA-002-implementation-report.md), [gap analysis](docs/reports/HA-002-existing-implementation-gap-analysis.md) and [R02 evidence](docs/reports/evidence/HA-002-A/r02/) retain their original review-time statements; the current decision index supersedes their pending-acceptance wording, without rewriting evidence. Original readme checkpoints remain accessible at [accepted source README](https://github.com/Shiva721254/HappyAlmere/blob/6da4b25e7a291e08b8fb0feab1b75ff22e55d0e0/README.md) and [historical governance README](https://github.com/Shiva721254/HappyAlmere/blob/871e7a35c95b05b75b047fc1cc048f29e33b7cbc/README.md).

GitHub is the shared source of truth. Codex implements authorized work, ChatGPT independently reviews, and the product owner accepts scope and authorizes integration/release separately. See [AGENTS.md](AGENTS.md). STRATO-first remains the hosting constraint; expansion needs separate approval. Microsoft business verification, Azure registration, Artifact Signing and production signing certificates are pre-release items, not internal development blockers.
