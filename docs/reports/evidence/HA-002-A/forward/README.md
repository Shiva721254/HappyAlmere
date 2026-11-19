# HA-002-A forward evidence

Local execution on the forward working tree, baseline 3287994. Source hashes are
recorded in verification-ledger.json; containing commit and remote CI are linked
in Issue #4/PR #6 after synchronization. Original gate JSON retains its actual baseline.

- baseline-recovery.json: original source unchanged, restored before schema changes.
- forward-recovery.json: enriched gate, injected failure, independent recovery, clean/
  upgrade/no-op, mappings, role negatives and complete migrated restore checks.
- performance.json: 10k synthetic rows, 30 warmed samples, 250ms local p95 threshold,
  exact full traversal and illustrative page-order EXPLAIN plan. Not production load.
- responsive/security/rate/network JSON: actual browser assertions and local listeners.
- verification-ledger.json: commands/count scope and normalized source hashes.

No archives, credentials, raw inventory or private keys are included. Independent
acceptance remains pending; CI success does not grant merge/deployment permission.
