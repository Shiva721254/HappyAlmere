# DB-ADR-001 - Shared PostgreSQL catalog foundation

Source: owner-supplied HA-002 master architecture (consolidated 19 November 2026).
Implementation disposition: HA-DB-D01 through D10 approved by owner and published in
[Issue #4](https://github.com/Shiva721254/HappyAlmere/issues/4#issuecomment-7014717337).
Implementation review/acceptance remains pending; original decision date is not invented.

Context: preserve existing PostgreSQL/EF/MVC discovery and support later cities without
duplicating services. Decision: shared PostgreSQL with catalog/discovery schemas,
UUID relationships, explicit city scope and separated database roles. Public city
scope is not private authorization. No Identity/business schemas are introduced.

Alternatives: separate per-city databases, destructive rebuild of existing fixtures,
or speculative storage technologies would add complexity or lose continuity; the
approved forward migration instead preserves the initial migration and all legacy
identity/content/publication. Domain remains independent of EF/PostgreSQL.

Consequences: composite same-city constraints, reviewed legacy UUID namespace,
controlled schema ownership and recovery rehearsals are required. A shared schema
requires resource authorization if private workflows are introduced later. See
[migration policy](../database/migration-policy.md), [ERD](../database/entity-relationship-diagram.md)
and [implementation evidence](../reports/HA-002-implementation-report.md).
