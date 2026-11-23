# Independent penetration testing policy

> HA-010-A documentation contract, 21 November 2026. [Authority](https://github.com/Shiva721254/HappyAlmere/issues/33#issuecomment-7065334379); [phase evidence and dispositions](../phases/HA-010-A.md). Documentation is not production certification, an operational exercise, or permission to process customer data.

PROD-A06, PROD-OPEN-19. Before applicable high-risk production releases and material security changes, commission qualified independent testers. ChatGPT architecture review and CI are not professional penetration testing. No tester, budget or active test engagement exists under this documentation authority.

A separate written authorization must identify exact assets and ownership, candidate digest, environments/accounts/synthetic fixtures, dates/time limits, allowed techniques/rate limits, prohibited destructive/social-engineering/third-party activity, provider permission, contacts and stop conditions. Verify isolation and cleanup/retention before execution. Production and real data require separate explicit approval; prefer isolated staging with synthetic positive controls.

Scope by implemented functionality: authentication/MFA/recovery/session revocation; object/field/function authorization; city/organization delegation; injection/XSS/CSRF/redirect; uploads/private evidence; API DTOs/limits; verified events/refunds/entitlements; native storage/TLS/deep links; ports/proxy; privileged operations/recovery. Preserve all [master security scenarios](../phases/HA-010-A.md), don't count future absent workflows as tested.

Report exact source/artifact/environment/tool versions, observed evidence, intended-denial reason, positive control, severity/exploitability, scope omissions and failures/skips. Use safe reproductions, no raw secret/dump/customer artifacts. Stop on scope drift, unsafe target, unexpected access, availability harm or sensitive disclosure. Escalate through incident owner; do not autonomously expand scope.

A fix requires independently reproduced retest against exact resulting candidate and regression review. Original failure persists in history. Unresolved applicable critical/high blocks release. Engagement acceptance, milestone acceptance and production deployment remain separate owner decisions.
