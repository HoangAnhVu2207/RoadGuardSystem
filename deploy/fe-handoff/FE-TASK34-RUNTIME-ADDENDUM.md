# Task34 runtime acceptance addendum

Historical V2 snapshot below. Later Task34 continuation verified opt-in HTTPS/browser behavior. The owner subsequently approved AI-frame reuse of existing files routes; the current implementation and approved-frame addendum supersede the Browser HTTPS and AI frame gate observations below. Original V2 results remain unchanged evidence, not claims about the final candidate.

Status: **FE_INTEGRATION_PREVIEW / PARTIAL**. This addendum supersedes the pre-runtime statements in Task34 provenance, handoff outputs1267–1268 and the documentation snapshot inside the immutable V2 ZIP. It does not claim independent review or GitHub publication.

Tested distribution: `RoadGuardBackend-Task34-V2.zip`, SHA256 `a4a31783cf5e58729856148af0faf7e990a364b619deebc73f8f914665053125`. The extracted source built and initialized a fresh `rg-task34-final` project with six new named volumes. No inherited SQL/MinIO volume was reused. The ZIP remains unchanged; distribute this addendum alongside it. Remote `huy-review` at `dd94aa697c411bb11d305973580055216f65ba73` does not contain the uncommitted candidate.

## Executed checks

- Final distinct .NET cases: **75 PASS, 0 FAIL, 0 SKIP, 0 ABORT** across five final TRX files. Earlier RED runs remain historical evidence; overlapping runs are not added.
- Python: **195 PASS, 0 FAIL** across the disjoint KnowledgeSpace136 and operations59 suites. Runner remains **DENY_ALL**.
- Extracted-stack API script:78 assertions PASS; OTP/Mailpit:5 PASS; candidate/result proof:26 PASS; direct authenticated MinIO frame checks:6 PASS; persistence after a controlled restart:20 PASS; SQL postconditions:7 PASS; bounded safety checks:9 PASS. These are script assertions, not additional .NET test cases or API FULL_CONTRACT counts.
- Project A/B isolation, G01 parent chain/paging/invalid inputs/current membership revocation and restoration executed against the fresh owned DB. Six persisted AI results cover road/empty/multiclass for A/B with1/0/2 detections. Six PNG objects have the expected1021 bytes and SHA256. Both PDF exports downloaded successfully. Database has192 tables;47 populated after OTP registration, not complete table coverage.
- Disposable SQL regression covers active-attempt callback fencing/replay, TTL300/revocation/private-read boundaries, recipient-scoped publication, signed Offline/Handover, notification scope, native geometry, FIELD and repair/Fast Track corrections. Export regression uses real disposable SQL/MinIO for normal execution and an injected object-write failure followed by a fresh service scope. Snapshot/artifact/audit/current-authority assertions pass. An injected write exception is not proof of arbitrary host/crash recovery.

## Remaining FE gates

| Gate | Observed result / decision needed |
|---|---|
| Browser HTTPS | HTTP `/api/v1/auth/web/csrf` returned500. Secure antiforgery cookies require HTTPS; CORS reflected the approved Origin. Cookie/CSRF browser login is **NOT_PASS** for this HTTP distribution. Configure a trusted development HTTPS path and verify it; do not weaken Secure/CSRF settings. Browser/phone UI E2E NOT_RUN. |
| AI frame access | Result/candidate APIs return frame identity/proof and stored PNG bytes are verified directly in MinIO. Generic `/files/{frameId}/content` returned404: its repository joins upload sessions, whereas generated AI frames have no upload session. An approved FE read/download contract is still needed; no new route or authorization policy was invented. Direct S3 harness access is not an FE download PASS. |
| Discovery | G01 is approved and the tested collection works. G02 repair, G03 Fast Track, G04 exports, G05 manager survey/AI and G06 retention/offline histories remain owner-gated. Known-ID APIs are not fresh-session catalogs. |
| Policy | A1 external AI issuer/protocol, B2 recipient/timing/dedup and C1 public-access parameters remain unresolved. Internal synthetic AI is not real YOLO. D1 automatic orphan deletion stays disabled. |
| Coverage |241 canonical operations,243 rendered including G01 and `/ready`. This task promotes **zero** APIs to FULL_CONTRACT. Successful focused assertions do not establish every mandatory dimension or all W01–W10 journeys. |
| Publication | No commit/push/tag/release/fresh GitHub clone was performed. Owner approval and a reproducible remote commit are required before GitHub delivery claims. |

## Host-specific startup and preservation

Default Docker address pools on this host were exhausted. The test used an **external, host-only** Compose override selecting the checked-free `10.254.34.0/24` subnet for the internal network. A clean FE host may use the packaged default Compose. On this host, every Compose invocation must include the same base and override files. Omitting the override in an early test helper caused network configuration drift; only the three fresh Task34 API/SQL/MinIO containers were recreated to restore connectivity, preserving their volumes. The failed attempt is retained separately from the final successful runs.

Owner authorized stopping exactly `rg-task33-zip-api-1`, `rg-task33-zip-sql-1`, `rg-task33-zip-minio-1`, `rg-task33-zip-mailpit-1`; all four remain stopped and their named volumes remain present. Restore with `docker start` followed by those exact four names. No `down`, prune, inherited volume deletion, Docker reset or WSL configuration change occurred. Tests used fresh owned Task34 SQL and disposable fixture SQL only; old FE Session DB and external SQL were not queried.

Docker remained responsive during final bounded batches; current data mount was rw and the bounded kernel snapshots contained no matching new I/O/JBD2 errors. This is not a filesystem integrity certification. Generated secret values were absent from the inspected last300 log lines/service and ZIP text files; this bounded check is not a universal secret audit. Detailed commands, timestamps, TRX, source preservation hashes and raw evidence are indexed in the final external MASTER-HANDOFF/EVIDENCE-INDEX.
