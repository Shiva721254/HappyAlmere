# City context foundation — HA-006-A

Implementation pending verification under CITY-G04 revision1.2. See [phase record](phases/HA-006-A.md) and canonical Issue29 for authority and current results.

## Request boundary

Application ICityContext exposes immutable authoritative UUID, slug, name, country, language, timezone, requested/canonical host and local/redirect flags. Web holder rejects uninitialized reads and repeated initialization. DI supplies the same holder through its interface per request; different scopes are separate. Domain/Application remain HTTP-independent.

Only selected HomeController.Index is CityScoped. Middleware follows routing and existing socket-peer search limiter. Platform Business/Privacy/Error/static/unmatched endpoints do not resolve city. Existing security controls remain unchanged.

Existing PostgreSQL resolver stays authoritative. Registered .test ASCII DNS names require active domain/city/primary in one parameterized statement. Only explicitly configured localhost/127.0.0.1 bypass the registry, still requiring active city. No forwarded/header/query/cookie authority or arbitrary local-map bypass.

Aliases redirect GET only to trusted registry HTTPS authority without incoming port. Bounded validated q/category/cursor values survive; duplicate recognized parameters and malformed escapes reject. Unknown parameters drop. Existing filter/cursor binding reused; navigation is not authorization. Discovery retains independent active/public/category eligibility.

## Ownership

Accounts/taxonomy global; domains/districts/locations/public discovery city-scoped. Future organization and account resources require their own authority. City context grants no private permission. No branding, private account activation, organization placeholders, cache, cross-city search or API implementation.

## Verification and recovery

New test lifecycle invokes verify-city-foundation.py once per applicable mode and verifies actual restored PostgreSQL/HTTP results. Test-only observer forces distinct scopes/connection objects to overlap. Original regression/performance fixtures remain separate.

Helper selectively reuses accepted provisioning/inventory, never historical main routines. At most two fresh instances, owned network, independent volumes/credentials and loopback VerifyFull. Independent reference uses protected EF/literal SQL in a transaction rolled back before actual archive restore; source equality alone is insufficient. Restricted archives, complete restored-state comparison, independent authority, temporary-authority retirement and actual restored application required.

Sanitized versioned evidence uses unchanged forward wildcard. Private files stay outside uploads. Stopped owned containers remain inspectable for CI fallback. No original/preserved database or volume targets. Upload precedes workflow fallback cleanup, requiring independent later cleanup verification.

## Limitations

Behavior unverified until actual runs recorded. Shared runtime credentials are not physical tenant isolation. DEF-01–08 preserved; no production readiness. Independent CITY-G10, owner candidate acceptance and integration remain separate.

