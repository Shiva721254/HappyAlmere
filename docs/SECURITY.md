# V0.1 security review

Historical V0.1 baseline. The [V0.2 database review](V0.2-DATABASE-DISCOVERY.md)
adds verified database TLS, runtime least privilege and database tests. Identity
and customer-data release gates below still apply.

## Enforced and tested

- Localhost launch bindings; actual loopback socket inventory and non-loopback local-interface probes in R02. AllowedHosts separately validates HTTP Host headers.
- HTTPS redirection; production HSTS (localhost is intentionally excluded by ASP.NET).
- CSP: self-only resources, no scripts, no objects, no framing, self-only form actions.
- X-Content-Type-Options, X-Frame-Options, restrictive Permissions-Policy.
- No-referrer policy to reduce search-query leakage through referrers.
- X-Robots-Tag and HTML noindex; neither is authentication or access control.
- Security headers registered before exception handling using OnStarting; tests
  cover normal responses, static assets, 400, 404, 429 and production exceptions.
  Host filtering can reject a request before application middleware.
- Global AutoValidateAntiforgeryToken MVC filter, secure/HttpOnly/Strict antiforgery
  cookie configuration. Test-only actions exercise missing, valid and tampered tokens; production has no token-probe route.
- Query/category length bounds and default Razor encoding of untrusted input.
- Fixed-window discovery budget: 120 requests/minute/normalized transport IP per process, no queue; Retry-After on rejection. Missing peer addresses share an unknown bucket. Forwarding headers do not select the bucket.
- Generic production error output with no-store caching. Detailed diagnostics
  remain restricted to Development.
- Kestrel server-identification header disabled.
- No customer-data entry flows, accounts, uploads, payments or trackers; the integration branch preserves only the existing synthetic PostgreSQL catalog.
  The search field explicitly asks visitors not to enter personal information.

No existing protection was disabled to make the build or browser tests pass.

## Boundaries and outstanding risks

This is a local read-only demo, not a secure production service. There is no
authentication/authorization implementation. Do not add real customer data or
public write endpoints until identity, permissions and storage protections are
implemented and tested. Antiforgery is not authorization.

Search is GET-based: terms remain in browser history and may appear in access
logs. Users can still type personal information; the UI warning cannot prevent it.
No request-body or query logging feature was added.

The limiter isolates distinct transport peers; clients behind the same NAT or proxy still share a budget. IPv4 and IPv4-mapped IPv6 use one bucket. This is not distributed or a DDoS defense; many IPs can consume resources and create partitions. Public hosting needs reviewed account/proxy/edge controls and load testing. Never trust arbitrary forwarded IP headers.

Before any production deployment, review TLS termination, trusted proxies,
forwarded headers, hostnames, secrets, persistent encrypted Data Protection keys,
log redaction/retention, backups and restore tests, least-privilege database roles,
database TLS, migration authorization, security patching and monitoring.

Before real accounts/data, implement an approved identity system, MFA for admins,
resource-level authorization, rate limits, session/cookie protections and audit
events. Extend token tests to actual state-changing flows when those are approved.
Plan GDPR/privacy and content moderation/verification with qualified reviewers.
Only synthetic sample content is present; no real business claims were introduced.

Browser smoke checks and pipeline tests are not a penetration test, comprehensive
accessibility audit, load test or certification of OWASP compliance.

## Reference material

- [ASP.NET Core integration testing](https://learn.microsoft.com/aspnet/core/test/integration-tests?view=aspnetcore-10.0)
- [ASP.NET Core rate limiting](https://learn.microsoft.com/aspnet/core/performance/rate-limit?view=aspnetcore-10.0)
- [ASP.NET Core antiforgery](https://learn.microsoft.com/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0)

## SEC-F01 local deployment boundary

The normal launch profile and VS Code launch use `https://localhost:7143` and
`http://localhost:5143`. Kestrel binds localhost to loopback (127.0.0.1 and ::1).
The repeatable browser harness inspects every listening socket of the test server
process and fails on any non-loopback address. It also tests this machine's non-loopback
IPv4 interfaces; this is local boundary evidence, not an external firewall audit.

AllowedHosts is an HTTP Host allowlist, **not a network access control**. A remote
client could send an allowed Host value if the listener were exposed. X-Robots-Tag
and HTML noindex are crawler instructions, **not access control**. HTTPS encrypts
traffic but does not authorize users. The local boundary depends on loopback sockets
and absence of forwarding/tunnels; other local users/processes can reach it.
Do not override URLs with `0.0.0.0`, `[::]`, `*`, `+` or a LAN/public address. No public
exposure, customer data or production use is authorized.

Permanent findings, classifications, milestones, ownership and acceptance decisions:
[security and engineering risk register](security/security-risk-register.md).
Future identity, secret storage, Data Protection and database requirements in that
register are not represented as implemented V0.1 capabilities.

Reference: [Kestrel endpoint binding](https://learn.microsoft.com/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0).

## Current reconciliation boundary

The integration branch combines the accepted V0.1 HTTP controls with the preserved
PostgreSQL implementation. Runtime reads use VerifyFull TLS and read-only credentials;
no runtime demo fallback or app-startup migration. Admin/migration variables are
removed from browser/web child environments. NUL-bearing query/category input now
returns HTTP 400 at the shared filter boundary rather than reaching PostgreSQL and
causing HTTP 500. This is a reproduced integration compatibility correction, not a
new database feature. Existing schema/migrations and synthetic data remain unchanged.
The HA-002 master gaps and temporary risk acceptance are documented separately.
