# HA-001-R01 evidence

This is the sanitized record of the fresh remediation verification, not historical baseline results.
Source checkpoint: c13f2232e331be5ba5d4ab7ab7cec84783a3eed1.

- Build/restore/test logs are actual captured stdout/stderr, normalized to UTF-8.
- test-cases.json contains the 33 actual TRX test results; the raw TRX remains in ignored artifacts/r01/test-results.
- responsive-results.json contains 35 geometry cases; home/boundary PNGs cover all seven widths.
- functional-results.json contains 190 checks, response headers and performance samples.
- rate-results.json contains five live same-peer checks; distinct-peer tests are in the .NET suite.
- static-results.json records the bounded source/history scan and reference graph.
- ide-result.json is the final real VS Code probe; earlier attempts retain their failures/cancellations. No private key or debugger variable values are included.
- verified-source-sha256.json hashes LF-normalized executable source/config/test content and asserts it matches the implementation checkpoint.
- evidence-sha256.json hashes the packaged evidence (excluding itself), normalizing CRLF to LF in text files.

For reproduction, use the commands in the remediation report and docs/DEVELOPMENT.md.
Copy supplemental audit-browser.cjs/audit-static.py into ignored artifacts/r01 before
running them from the worktree root; do not overwrite the archived evidence in place.
The supplemental browser harness expects the Production server at HTTPS7243/HTTP5243.
Use the existing trusted development certificate's PUBLIC certificate in
NODE_EXTRA_CA_CERTS if the Node client does not use the Windows trust store. Do not
export its private key or set ignoreHTTPSErrors. Restart the owned server between
functional and rate modes so the real 120/minute policy is not altered for tests.
The independent rate probe intentionally treats two browser contexts on localhost
as one transport peer; separate-IP isolation is exercised by the integration suite.

CI action refs were checked read-only, but no new remote workflow was run.
This evidence is not architectural approval, owner sign-off or production certification.
