# 04 Implementation Status

**CURRENT_VERIFIED — source baseline `cdeb1ca1037744d06995058af13d0582f6273c0a`, `huy-review`, 2026-10-13.** Source observation is not all-path runtime execution. Expected business behavior uses owner-confirmed decisions and assigned specs; implementation/test evidence has its own limits.

| Phân hệ | CURRENT_IMPLEMENTATION | Verification boundary |
|---|---|---|
| Auth/accounts/staff/projects | Implemented; focused current HTTP/security tests include these surfaces | Actual browser/proxy/keyring/mobile adoption NOT_RUN |
| Roads/GIS/pavement/survey | Source-traced versioned geometry, assignment, datasets, explicit CRS/profile/source | Official control-point/accuracy/real coverage acceptance pending |
| Reporter/Case/candidate/AI | Implemented source workflows; partial matching/callback/pre-Defect gates remain | Live FastAPI/video model NOT_RUN; synthetic path separate |
| FIELD/repair/FT | PM policy, measurements, normal/FT execution/review/correction/lifecycle implemented | R03 final information producer gap; additional FT after expiry deferred |
| Offline | Device/snapshot/provenance/signed batch/receipt/package/grant/artifact/reconcile implemented | Actual Android/device key/decryption/restart pending; many runtime branches NOT_RUN |
| Reporting/notifications/retention/export | Source producers/consumers and history, scoped reads, evaluation/export flows present | Registered source type is not proof a producer exists; deployed workers/storage pending |
| Database/API | Fresh EF model and runtime Swagger captured safely; no physical SQL query in this task | EF_LOGICAL_MODEL/MIGRATION_DERIVED_PHYSICAL_MODEL; LIVE_SQL_VERIFIED NOT_RUN |

Method existence is SOURCE_VERIFIED only. Tests are PASS only within fresh executed cases; DOC-AUDIT-01 counts are historical source-revalidated evidence. No whole-system completion or all-route security certification is claimed.
