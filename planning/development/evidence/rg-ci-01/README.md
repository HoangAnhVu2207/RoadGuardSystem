# RG-CI-01 review evidence

These files are sanitized, local validation evidence for RG-CI-01 at base SHA `fc70d4714921185d9b0d93e766835aed654102d2`. They contain no connection string, password, JWT, refresh token or SMTP credential. Hosted CI and external review require separate checks on the pushed SHA.

| Evidence | Contents |
|---|---|
| `schema-equivalence.json` | Old 69-migration catalog versus one direct-baseline catalog: zero business-schema differences. |
| `old-chain-schema.inventory.json.gz` | Complete read-only SQL catalog captured from a fresh, isolated old-chain database. Decompress with `gzip -d -c` or Python `gzip.open`. |
| `schema-source-audit.md` | Model/manual-SQL differences, source paths and baseline resolution. |
| `test-risk-matrix.json.gz` | Complete 723-case source/risk/disposition/final-protection mapping, including theory rows. |
| `seed-coverage.json` | All 192 actual tables, counts, empty reasons, five synthetic `example.test` accounts and nine source-supported scenarios after the post-login rerun. |
| `validation-summary.json` | Local SQL lane, unit and API TRX counts plus per-lane executed-identity SHA-256 digests and pending hosted status. |
| `api-seed-smoke.json` | Seeded principal login and project authorization status codes; response bodies and credentials were discarded. |

Original 723 executed integration cases map to 652 retained SQL cases, 53 pure unit transfers, 14 catalog cases consolidated into two current-schema checks, and four obsolete historical-upgrade removals. The new SQL suite has 658 executed identities, assigned to four lanes (123/130/215/190). `tests/CI/sql-lanes.json` contains every expected display identity. `Verify-SqlLaneDiscovery.ps1` compares its classes with the compiled xUnit assembly, so a newly added class cannot silently miss all lanes; `Verify-SqlLanes.ps1` reconciles executed identities and rejects an added case in an existing class unless the manifest is updated.

The 191 business tables, 1,940 columns, 722 foreign keys, 899 indexes, 229 checks and 189 installed triggers match the old catalog. The EF history row is intentionally different. The 192-table seed inventory includes EF history; 65 tables are populated and 127 have explicit empty-table reasons. Signed offline device transfer and real AI inference remain source-supported GAPs, not fabricated seed states. The current-schema inventory is also in `docs/backend/data/current-schema.inventory.json`.

This evidence proves only the stated local SQL/MinIO/API conditions. The local four-lane maximum test step was 3m22s. Hosted timing, required checks and external review must be assessed on the exact pushed SHA.
