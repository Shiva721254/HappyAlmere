# HA-001 acceptance criteria

Authoritative criteria supplied by the product owner as an image on 19 November 2026 (Europe/Amsterdam), transcribed below. These replace the provisional audit checklist. Supplying criteria is not acceptance sign-off.

| ID | Acceptance criterion | Conditions for passing |
|---|---|---|
| AC01 | Correct project root and solution configuration | The correct workspace opens in VS Code. All projects are included in the solution and build commands work from the project root. |
| AC02 | Correct architectural dependencies | MVC, DRY, SOLID and separation of concerns are respected. Project references follow the approved dependency rules without circular dependencies. |
| AC03 | Successful compilation | The entire solution builds successfully with zero compilation errors and zero unexplained warnings. |
| AC04 | Application running over HTTPS | The application starts successfully using HTTPS, with valid local development certificate configuration. |
| AC05 | Homepage and navigation verified | The homepage renders correctly. Navigation links, buttons, menus and existing pages function as intended. |
| AC06 | Search and filtering verified | Existing search and category filters return appropriate results. Empty, invalid and unexpected inputs are handled safely. |
| AC07 | Responsive design verified | The website functions correctly at 320px, 375px, 430px, 768px, 1024px, 1440px and 1920px without unintended horizontal scrolling or broken layouts. |
| AC08 | Security controls reviewed | Applicable security headers, HTTPS, HSTS, CSP, CSRF, input validation, output encoding, rate limiting and error handling are inspected and tested. |
| AC09 | Dependency vulnerabilities checked | Direct and transitive dependencies are scanned. No unresolved critical or high-severity vulnerabilities remain. |
| AC10 | Automated tests executed | Relevant unit and integration tests are implemented and executed. Mandatory tests pass, with actual results documented. |
| AC11 | Project documentation completed | README.md, AGENTS.md, SECURITY.md, CHANGELOG.md and required architecture and development documentation are available and accurate. |
| AC12 | Git recovery checkpoint established | A recoverable Git checkpoint exists, without losing existing work or exposing sensitive information. |
| AC13 | Implementation report reviewed | The HA-001 implementation report includes actual evidence, file changes, test results, architectural decisions and security findings. |
| AC14 | Remaining risks accepted or resolved | Critical and high-priority findings are resolved. Remaining risks are documented and explicitly accepted where appropriate. |
| AC15 | Product owner approval | You review the architect’s recommendation and formally approve V0.1 before development proceeds to V0.2. |
