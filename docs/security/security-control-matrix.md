# Security controls and evidence mapping

> HA-010-A documentation contract, 21 November 2026. [Authority](https://github.com/Shiva721254/HappyAlmere/issues/33#issuecomment-7065334379); [phase evidence and dispositions](../phases/HA-010-A.md). Documentation is not production certification, an operational exercise, or permission to process customer data.

PROD-A05; HA010-PRE-F01. Source pin: accepted commit7cb61f839aa0a2940e7349fc81cd13e035906475. Relative source links below identify preserved files; candidate source equality must be verified before relying on them.

## Pinned ASVS selection

Official [downloads](https://asvs.owasp.org/downloads/) and [5.0.0 requirement CSV](https://raw.githubusercontent.com/OWASP/ASVS/v5.0.0/5.0/docs_en/OWASP_Application_Security_Verification_Standard_5.0.0_en.csv) refreshed2026-11-21; stable5.0.0. IDs below were checked against that version. This is a bounded foundation mapping, not exhaustive L2 completion. Remaining applicable requirements require independent release-specific assessment; nothing omitted is implicitly N/A.

| ASVS5.0.0 ID / focus | Present evidence or scope | Verification / disposition |
|---|---|---|
| V1.1.2 / final encoding | Razor views and no raw HTML output | Existing Web/Remediation/browser tests; fresh CI pending |
| V1.2.1 / contextual encoding | Razor default encoding | PROD-SEC08; test hostile text in actual output; not all contexts certified |
| V1.2.4 / parameterized queries | PostgresDiscoveryRepository EF query composition | PROD-SEC07; query evidence and hostile input regression |
| V2.1.1 / validation rules | DiscoveryFilter100 query/30 category/80 slug characters; max100 results | Documented source contract; Web and Discovery tests |
| V2.2.1 / input checks | NUL/length rejection before repository calls | Existing negative tests; no client-only reliance |
| V2.2.2 / trusted validation | Application filter/service boundary | Source inspected; original test coverage retained |
| V2.4.1 / automation bounds | Search120/min socket peer, no queue,429 Retry-After | Existing rate tests; production volumetric protection unresolved |
| V3.3.1 / secure cookie | Antiforgery Secure Always; no explicit secure-name prefix | PARTIAL source alignment; prefix expectation requires future authorized assessment, no edit here |
| V3.3.2 / SameSite | Antiforgery Strict | Configured, actual browser behavior checked in existing harness |
| V3.3.3 / host cookie | No explicit __Host- antiforgery name configured | Not demonstrated; future scoped hardening, not a claimed L2 pass |
| V3.3.4 / HttpOnly | Antiforgery HttpOnly | Configured; no public Identity session cookie exists |
| V3.4.1 / HSTS | UseHsts outside Development; no explicit one-year/subdomain policy | PARTIAL; accepted local behavior does not demonstrate full ASVS expectation; live domains DEF-03 |
| V3.5.1 / CSRF | Global AutoValidateAntiforgeryToken filter | PROD-SEC09; existing unsafe-method tests, future mutation workflows separately verified |
| V4.1.3 / intermediary fields | No forwarded-header authority; rate uses socket peer | PROD-SEC11; future proxy topology HA-009 requires separately trusted ingress |
| V8.2.1 / function authority | Private functions not published | Deferred DEF-01/02; no authenticated authorization implementation claim |
| V8.2.2 / object authority | Public discovery city predicate, not private authorization | PROD-SEC04/05 deferred to003/004/006 |
| V8.2.3 / field authority | Public Razor ViewModels; private API absent | Future DTO and field-level negative tests HA-007 |
| V12.2.1 / HTTPS | HTTPS redirect, development local certificate | Existing trusted HTTPS harness; no public TLS certification |
| V12.3.1 / service TLS | DatabaseConfiguration.RequireVerifiedTls | Actual PostgreSQL tests/VerifyFull negatives; future provider traffic unverified |
| V16.5.1 / generic errors | Nondevelopment /Home/Error, no-store500 | Existing error tests; source inspected |
| V16.5.3 / fail safely | Invalid database/Identity config rejected; unknown city404 | Regression checks; recovery/production failures separately assessed |

Selected stronger control V2.3.5 (L3 approval separation) is a future high-value financial/release contract, not an implemented payment mechanism or L3 certification. Sensitive session/revocation controls require full future V6/V7/V8 assessment. No native MASVS controls are asserted passed; pin actual framework version/profile and STORAGE/CRYPTO/AUTH/NETWORK/PLATFORM/CODE/RESILIENCE/PRIVACY requirements before HA-007 activation.

## Actual source inventory

[Program](../../HappyAlmere_V0.1_MVC/src/HappyCity.Web/Program.cs), [headers](../../HappyAlmere_V0.1_MVC/src/HappyCity.Web/Security/SecurityHeadersMiddleware.cs), [controller](../../HappyAlmere_V0.1_MVC/src/HappyCity.Web/Controllers/HomeController.cs), [filter](../../HappyAlmere_V0.1_MVC/src/HappyCity.Application/DiscoveryFilter.cs), [database configuration](../../HappyAlmere_V0.1_MVC/src/HappyCity.Infrastructure/DatabaseConfiguration.cs), [Identity registration](../../HappyAlmere_V0.1_MVC/src/HappyCity.Infrastructure/Identity/IdentityServiceRegistration.cs).

Headers configured: CSP same-origin resources/no scripts/no frames/no objects; nosniff; DENY; no-referrer; camera/microphone/geolocation disabled; noindex/nofollow. Kestrel server header disabled. AllowedHosts explicit synthetic hosts. Configuration and source review are not fresh HTTP execution. Current HomeController resolves city for Index only; HA006 broader context remains unaccepted. Authentication middleware/endpoints/session/MFA are absent by accepted scope.

## NuGet F01: enforcement versus enumeration

[Directory.Build.props](../../Directory.Build.props) sets NuGetAudit=true, NuGetAuditMode=all, TreatWarningsAsErrors=true. Fresh preflight MSBuild evaluation of all five projects under SDK10.0.403 additionally found Level=low, WarningsNotAsErrors empty, no NuGetAuditSuppress items and only NoWarn1701;1702. No repository override found. This is effective local configuration, not a restore result or guarantee about remote source availability.

[Unchanged workflow](../../.github/workflows/build.yml) runs restore followed by Debug/Release --no-restore. Its separate dotnet list HappyAlmere.slnx package --vulnerable --include-transitive reports findings; exit0 alone proves no absence. npm audit --audit-level=high separately enforces JavaScript severity. Record actual restore warnings/errors/advisory availability, package enumeration/findings and npm outcome independently for each candidate. No vulnerability injection, suppression, new scanner or workflow modification authorized. Remote effective settings and supply-chain state may differ; inspect actual logs rather than inherit local pass.

[Evidence ledger](../governance/release-evidence-register.md) and phase record own actual fresh results, skips, unavailable artifacts and limitations. F01 documentary correction does not itself close independent review.

## Future controls and evidence

SBOM/image provenance, qualified pentest, independent encrypted backup, deletion reconciliation, MFA/session lifecycle, secrets rotation, public ingress, production monitoring and capacity remain future. A missing production capability blocks its affected release; it is not repaired by this documentation. Tests, migration definitions, privileges, 10k/250ms,25ms and20-minute limits are immutable under this task.
