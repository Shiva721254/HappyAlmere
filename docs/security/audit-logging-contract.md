# Audit logging and protected evidence

> HA-010-A documentation contract, 21 November 2026. [Authority](https://github.com/Shiva721254/HappyAlmere/issues/33#issuecomment-7065334379); [phase evidence and dispositions](../phases/HA-010-A.md). Documentation is not production certification, an operational exercise, or permission to process customer data.

PROD-A07; DATA-16/17/20; THR19/23/24. Current source only establishes safe elapsed-query warnings and synthetic test evidence; no claim of a complete privileged audit system.

Future events require attributable actor or system identity, UTC timestamp/time source, target type/minimized identifier, action/scope, outcome/reason code, correlation ID, environment and immutable release reference. Cover privileged login/MFA/recovery, organization claim/delegation, publication revision/suspension, entitlement source/period, refund approval/reconciliation, deployment digest/approver, migration target/version/result, rights disposition and restored recovery point.

Do not log passwords, recovery codes, tokens, connection strings, TLS/key material, raw private evidence, raw searches or precise device location. Log a safe identifier and status instead of request bodies/query parameters. Prevent log injection with structured fields and validated bounds. A hash of a person identifier may remain personal data; classification/retention still apply.

Access: least-privilege append and independent restricted review; ordinary runtime must not rewrite historical audit. Time synchronization, tamper detection, protected transfer/storage, access audit and independent custody need actual implementation/testing under later authority. Backup inherits highest classification. Proposed privileged retention12months is conditional, not implemented or legally mandated.

[SafeQueryTimingInterceptor](../../HappyAlmere_V0.1_MVC/src/HappyCity.Infrastructure/SafeQueryTimingInterceptor.cs) currently emits elapsed milliseconds only for reads>=250ms with a60s warning throttle; it deliberately excludes SQL, parameters, search terms, hosts and identifiers. Inspect proxy/platform/framework logs separately before claiming no query leakage. Existing logging configuration reduces framework/EF command verbosity but does not prove every deployment logging sink safe.

Evidence release: minimize -> scan structured values and archives -> inspect contents/images -> redact only a review copy -> retain original protected bytes and digest -> publish sanitized result with exact provenance/limitations. Never silently mutate original failed evidence. Seven-day CI retention is temporary; owner must authorize durable custody if needed. No new external storage/service or secret collection here.
