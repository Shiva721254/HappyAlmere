# V0.1 architecture

## Existing workspace review

Before editing, all 23 original authored files were read: four project files,
domain model, application contract/service file, demo repository, controller,
Program, settings, launch profile, all Razor views, CSS, README, ignore rules
and workflow. Generated obj files were inventoried as build outputs, not source.
No AGENTS.md, solution, tests, editor configuration or Git repository existed.
The original app already compiled; no source compilation defect was invented.
Checkpoint commit: 9637633.

## Dependency direction

```text
Web -> Application -> Domain
Web -> Infrastructure -> Application + Domain
FoundationTests -> Web
```

- Domain: immutable discovery content with no framework dependency.
- Application: validated search filter, service and repository contracts. No MVC,
  database or infrastructure dependencies.
- Infrastructure: immutable demo data and repository filtering.
- Web: MVC controllers, typed view models, Razor, shared layout/card partial,
  static CSS and HTTP security middleware. Program is the DI composition root.
- Tests: application behavior and real MVC middleware through TestServer.

The scoped service delegates retrieval through the repository interface. The
singleton demo repository holds only immutable sample data. A future DbContext
and EF repository must be scoped. Search filtering belongs in repository queries,
not in a controller or after loading a production database into memory.

DiscoveryFilter centralizes normalization and size limits. Web translates invalid
input into HTTP 400. Cancellation travels from HTTP through service to repository.
The demo uses completed tasks, not Task.Run or pretend asynchronous I/O.
Future database calls must await EF Core async operations with the cancellation token.

DiscoveryViewModel carries results and form state; Razor encodes text by default.
The current immutable domain record is safe for this internal presentation.
Dedicated public DTOs will be required when versioned mobile API contracts exist;
there is no speculative API or duplicated DTO mapping in V0.1.

## Future storage and cities (design only)

PostgreSQL with EF Core and its Npgsql provider is the selected persistence path.
No packages, DbContext, migrations, connection secrets or database were added.
Implement them only in the separately approved next phase, after V0.1 verification.

Before persistent data:

1. Model City with a stable identifier and unique slug. Add a required CityId to
   city-owned content and constrain/query by city in every repository operation.
2. Resolve a validated city context at the Web boundary. Move Almere-specific
   branding into city configuration, then test Almere/Utrecht data isolation.
3. Add bounded pagination, indexed queries, projections and AsNoTracking for reads.
   Specify and test Dutch search/collation behavior against real PostgreSQL.
4. Use scoped EF repositories, parameterized LINQ, explicit migrations and separate
   least-privilege runtime/migration credentials. Require verified database TLS.
5. Add versioned API DTOs that reuse application services and authorization rules.

V0.1 is intentionally Almere-only. HappyCity naming and inward dependencies enable
extension; they do not constitute implemented multi-city isolation.

## Maintainability

Shared .NET build properties enable nullable references, implicit usings, warnings
as errors and transitive NuGet auditing. global.json pins a non-preview .NET 10 SDK.
The root solution contains all five projects. Editor settings define UTF-8 and
consistent whitespace. Git ignores build outputs, test results and secret formats.
The CI workflow lives at the actual repository root.

The frontend uses native Razor/HTML/CSS without JavaScript or external assets.
Small-screen rules are the base; larger layouts use a min-width breakpoint.
Shared components and framework DI remain sufficient for this scope.

## HA-001-R01 refinements

DiscoveryCategories holds stable city-independent category IDs in Domain; repository
and Razor reference those IDs. Razor derives maxlength from DiscoveryFilter.
Inherited overflow-wrap:anywhere allows accepted unbroken text to remain readable;
flex/grid children have min-width:0. No global overflow hiding is used.
Rate limiting partitions by normalized transport peer with no forwarded-header trust,
retaining the original per-bucket count/window and zero queue. See SECURITY.md for
NAT/proxy and public-hosting limitations. No dependency direction or framework changed.

## HA-001-R02 independent review remediation

The existing layered MVC architecture is accepted in principle by the independent
review; no rewrite is authorized or performed. Razor/CSS/configuration edits are
formatting only. Browser QA now has a private development-only npm package and
lockfile; no JavaScript dependency enters the application or .NET project graph.
The Windows local/CI harness preserves trusted HTTPS and verifies actual loopback
sockets. Planned multi-city and persistence requirements remain unimplemented and
are tracked in [the risk register](security/security-risk-register.md).
