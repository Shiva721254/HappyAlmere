# Security policy and release boundary

HappyAlmere V0.1 is a proprietary, local, read-only prototype with synthetic content.
It is not approved for real customer data or production deployment. Authentication,
authorization and persistent storage are not implemented in this remediation branch.

The canonical control details and residual risks are in [docs/SECURITY.md](docs/SECURITY.md).
Verification evidence is in [the HA-001-R01 report](docs/reports/HA-001-R01-remediation-report.md).
Report suspected vulnerabilities privately to the repository owner through an
existing private channel. Do not post credentials, personal data or exploit details
in public issues. No response SLA or dedicated security mailbox is claimed.

Never disable HTTPS, certificate verification, security headers, validation or CSRF
to resolve errors. Audit direct and transitive dependencies before acceptance and
review .NET security patches regularly. Production requires an independently
reviewed threat model, identity/authorization, secure database operations, trusted
proxy configuration, monitoring, privacy controls and recovery testing.
