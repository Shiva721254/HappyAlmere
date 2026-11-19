# HA-001-R02 evidence

Fresh execution on 19 November 2026, based on reviewed R01 commit 7b6e082.
See [the R02 report](../../HA-001-R02-independent-audit-remediation.md) for exact commands,
failures, results, environment, scope and pending independent/owner approvals.

- Build/restore/test logs: fresh five-project verification, 33 tests passed.
- npm/nuget audits and npm inventory: no known vulnerabilities, development-only Node packages.
- submitted-ci.json: actual GitHub success for R01; not evidence of the unpushed R02 workflow.
- responsive/functional/rate results and per-suite network JSON: actual local browser/security/binding checks.
- formatting comparisons: whitespace-only source and 21 identical before/after screenshot bytes.
- baseline-browser.log and browser-port-probe-failure.log: retained failed harness attempts.
- browser-final.log: complete passing corrected harness on Node 25.6.0.
- browser-node24.log and node-runtime-verification.json: exact CI-pinned Node 24.22.3 verification, also passing.
- static-results.json: graph/config/document and bounded credential review, no finding values logged.
- verified-source-sha256.json: normalized source/config/test hashes; evidence-sha256.json covers this package.

Private keys, certificates, raw TRX, machine interface addresses and server logs are excluded.
Ignored raw files remain under artifacts/r02. These records are not public-deployment,
production-load, full accessibility or physical-device certification.
