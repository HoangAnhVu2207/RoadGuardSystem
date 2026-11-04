# Expo architecture and local data security

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

## Proposed client layers

`PROPOSED_EXPO_UI`: navigation/auth boundary → typed API client → bearer/session interceptor → feature repositories → protected draft/upload queue → screens. Keep Reporter, account and upload data partitioned by authoritative user ID. API client must preserve exact method/path, request DTO, `Idempotency-Key`, `If-Match`, response ETag and correlation ID from the catalog.

Use Expo camera/image-picker/location permissions only at the edge. Normalize final file bytes before upload. Use SecureStore or an equivalent OS protected secret facility for refresh tokens and encryption key material. Access tokens should be memory-first. Do not use AsyncStorage/plain SQLite for tokens, raw private photos, presigned URLs or unredacted ProblemDetails.

## Local data policy

The Backend’s encrypted handover/storage does not prove that an Expo SQLite database, thumbnails, cache or file URI is encrypted on the device. SQLCipher/equivalent database encryption, encrypted files, key generation, biometric/device binding, migration, backup exclusion and uninstall behavior are **PROPOSED / NOT_VERIFIED_EXPO_E2E** and require a mobile architecture decision. If the MVP is online-only, minimize local persistence and delete temporary bytes after the server has accepted and the UI no longer needs them.

Logout must revoke the server session through API-007 and clear access/refresh secrets, cached own projections and temporary media according to an owner-approved retention policy. Do not silently delete an unsent report/draft; present the user with its state and recovery choice. Device loss, uninstall and app reinstall are not a Crew enrollment/key-loss workflow for Reporter.

## Offline boundary

Reporter MVP may retain a protected pending queue for network retry, but it does not use Crew device enrollment, signed canonical batches, snapshots, per-item receipts or ECDSA. Offline report creation semantics, conflict policy and durable encrypted queue are not source-confirmed; mark them `SOURCE_GAP` until the owner defines them. Online API idempotency and resumable upload recovery remain the supported boundary.

**Evidence:** Phase 2B offline/security chapters, `ReporterEvidenceService`, `AuthService` and the source handoff. No Expo repository/native module/deployed device was supplied.
