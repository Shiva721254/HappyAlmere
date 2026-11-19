# V0.1 security review

## Enforced and tested

- Localhost launch bindings and AllowedHosts allowlist.
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
- No customer-data entry flows, database, accounts, uploads, payments or trackers.
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
