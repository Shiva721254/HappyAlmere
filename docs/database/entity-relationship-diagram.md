# Implemented catalog relationships - HA-002-A

Review candidate; owner authorized implementation in Issue #4 comment 7014717337.
The original integer/public migration is preserved; CatalogForward appends these
relations. No Identity, business, live hostname resolver or customer records.

```mermaid
erDiagram
    cities ||--o{ city_domains : configures
    cities ||--o{ districts : contains
    cities ||--o{ locations : contains
    districts o|--o{ districts : parent_same_city
    districts o|--o{ locations : same_city
    categories o|--o{ categories : parent
    categories ||--o{ category_translations : translates
    cities ||--o{ discovery_entries : scopes
    categories ||--o{ discovery_entries : classifies
```

UUID PKs; category_translations uses (category_id, language_code). District alternate
key (city_id,id) supports composite FKs from child districts and locations. Serialized
hierarchy writes reject cycles; higher isolation levels are explicitly rejected for
hierarchy writes rather than relying on stale snapshots. Runtime is read-only.

Discovery retains the original district display string; no inferred relationship to
districts. Domain/district/location tables start empty. City domains are configuration
only: no host activation or city resolver is implemented. See the [dictionary](data-dictionary.md)
and [migration policy](migration-policy.md) for preservation/default semantics.
