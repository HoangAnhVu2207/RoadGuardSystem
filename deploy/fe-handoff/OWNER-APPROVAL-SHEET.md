# Exact owner parameter decisions still required

A1/B2/C1/D1 option selections are OWNER_CONFIRMED_OPTION; the rows below are PROPOSED choices only. Source ground: docs/backend/OWNER-POLICY-TASK31.md; existing callback, H6 dispatcher, publication/private access and retention repositories. No option below is silently defaulted.

| Feature | Required decision | Source-grounded alternatives / effect |
|---|---|---|
| A1 | service registration/minting, lifetime, custody/rotation, issuer/audience, failed callback wire | Owner-operated mint service vs reviewed external identity issuer; select explicit trusted keys/lifetime and job-attempt credential binding. FE user sessions never authorize callbacks. Preview remains denied. |
| B2 | exact native FastTrack emitting transition | Choose the actual confirmed workflow transition and immutable source identity; existing NORMAL producer is not substituted. |
| B2 | PM/Supervisor recipient selection and time | All currently authorized vs explicitly assigned; snapshot at emit vs re-resolve at dispatch; choose dedup identity/no-recipient fallback. Both PM and Supervisor remain required by confirmed option. |
| C1 | approver/audience and public representation schema | Specify authorized approving role, public/restricted audience, exact field/media allowlist, redaction/precision and withdrawal/cache handling. Original private objects never become public by default. |
| D1 | manual delete/retention policy | Specify deleting authority, duration, legal/recovery holds and quarantine/regeneration. Detection alone does not delete. |

These gates do not block independent local profile/project/evidence/intake/export/OTP use. TTL300 private capability rule is already confirmed; do not ask to reconfirm it. No additional file writer permission is required.
