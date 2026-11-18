# HappyAlmere V0.1 — MVC foundation
Dutch-first local discovery prototype. No real businesses, user accounts, personal-data capture, database or payments yet.

## Run locally
Install .NET 10 SDK. From repository root:
```bash
dotnet dev-certs https --trust
dotnet build src/HappyCity.Web
dotnet run --project src/HappyCity.Web --launch-profile https
```
Open https://localhost:7143. On Linux, certificate trust may require OS/browser-specific steps.

## Architecture
Web (MVC controllers/Razor views) -> Application (service/repository contracts) -> Domain (entities). Infrastructure implements application contracts. Dependency injection in Web. Shared layout, partial card and stylesheet enforce DRY. In-memory demo repository is deliberately replaceable with EF Core/PostgreSQL. Search uses a bounded query and encoded Razor output.

## Security baseline / limitations
HTTPS redirection, production HSTS, CSP, frame protection, antiforgery filter for future unsafe MVC actions, host allowlist and a global search limiter (not per-user). No third-party scripts or tracking. The prototype is noindex and localhost-only by configuration; noindex is not access control. Do not deploy publicly as-is. No authentication, database, privacy/legal review, penetration test or production security verification yet. Search terms may appear in logs and history. Configure a trusted reverse proxy and forwarded headers before deployment. No .NET SDK was available in the generation environment, so compilation and browser tests have not run.

## Acceptance checklist
- Build successfully with .NET 10 SDK.
- Open homepage and test every navigation category.
- Search for `familie`, then a nonexistent term; verify results and empty state.
- Test at 320, 375, 768, 1024 and 1440 px; no horizontal scrolling.
- Keyboard navigation, focus visibility and contrast audit.
- Inspect security headers; verify oversized search returns 400 and rate limit returns 429.
- Confirm no real personal information is entered or retained.

## V0.2 gate
Add PostgreSQL, EF migrations, persistent content and admin authentication (Identity, MFA, resource authorization, audit logging); integration tests and threat model before real data. Public release only after security/privacy and content validation. Future modules: promotions, business accounts and payments; native clients use a versioned API later.
