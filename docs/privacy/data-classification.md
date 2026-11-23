# Data classification

> HA-010-A documentation contract, 21 November 2026. [Authority](https://github.com/Shiva721254/HappyAlmere/issues/33#issuecomment-7065334379); [phase evidence and dispositions](../phases/HA-010-A.md). Documentation is not production certification, an operational exercise, or permission to process customer data.

PROD-OPEN-07; PROD-A04. Classify by actual content/context, including public information about a person. Classification is an engineering handling rule, not a lawful basis or a claim of anonymization.

| Class | Meaning | Handling contract |
|---|---|---|
| P0 | Approved public projection | Publish only reviewed fields with provenance; personal data still subject to privacy duties |
| P1 | Internal nonsensitive planning | Authorized team access; version and ownership |
| P2 | Confidential personal/business information | Purpose-limited access, TLS, protected storage, minimized exports/retention |
| P3 | Restricted auth/security/financial/private evidence | Strong least privilege, separately audited access, encrypted storage/backup, approved exceptional access |
| P4 | Critical secrets/key material | Dedicated protected custody/recovery; never ordinary Git/logs/artifacts or shared chat |

Backups/archives inherit the highest class present, including P4 if key material exists; separate custody and deletion authority are required. Hashes/pseudonyms can remain P2/P3 personal data. Anonymous aggregate classification requires an actual reidentification assessment (small groups/location/linkage); replacing names alone is insufficient.

Inventory DATA01–20 in [processing register](processing-register.md). Unknown classification defaults restricted until assessed. Synthetic fixtures may imitate P2/P3 structures but must not be actual customer records. No routine BSN/ID copying, private home address publication, precise tracking or unrestricted evidence dumps. Current .test records are sample content, not verified businesses.
