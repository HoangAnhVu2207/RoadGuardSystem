# Reporter routing map — Supervisor intake

Owner decisions: **26/11/2026**, recorded in [assigned spec](../../planning/development/REPORTER-ROUTING-MAP.md). Implementation and executed verification status are recorded separately below. This is a BE contract; MapLibre/UI acceptance is not implied.

## Authority and source

The current system is single-organization: `ProjectScopeGuard.AuthorizeAsync` grants Supervisor global project read authority; the inspected Project/user models have no tenant/company boundary. Every request rechecks active user, role and session. PM retains current Case project membership; Reporter/Crew/Operator receive no organization map grant. Internal intake decisions/history are Supervisor-only. No private image, Reporter PII, storage key or signed URL appears on the map.

Use existing `GET /cases/{caseId}/reports` to choose a Report and **the exact evidence**, including supplements. Its private `contentPath` remains the authorized image path. New endpoints never borrow another image's GPS. Missing GPS stays null; no marker at 0,0 or a route/slab centre. Capture/Exif/Manual and accuracy are preserved; `capturedAt` is client claimed, not verified event time.

All paths below start `/api/v1`. Web: login through `/auth/web/*`, send `credentials: "include"`. Obtain `requestToken` from `GET /auth/web/csrf`; send **`X-CSRF-TOKEN`** for POST. GET needs no CSRF. Bearer remains an alternative. Keep both choices distinct in client configuration.

## A — organization map

`GET /company/road-map?bbox=west,south,east,north&limit=20&cursor=...&includeHistorical=false`

Supervisor only. `limit` 1–100, default 20; bbox is finite WGS84, west < east and south < north, within world bounds, no antimeridian wrapping. GeoJSON uses `[longitude,latitude]`.

`features[]`: `type: "Feature"`, stable route-version `id`, `geometry: {type:"LineString", coordinates:[[longitude,latitude],...]}`, and `properties`:

- project ID/name/status, road-section ID/code/name, route-version ID/number/current flag/effective time;
- `geometryHash` identifies the immutable geometry; **it is not the geometry-package aggregate `geometryVersion` used by triage**;
- CRS profile ID, segment-set IDs/status/version/hash; historical obligation geometry pins (source location version and route version);
- `geometryUsage: "DISPLAY_ONLY"`; no permission or business conclusion is implied.

Default scans current and obligation-referenced historical route versions, including closed projects. `includeHistorical=true` scans every stored version. A version is never silently replaced by a current version. Pages use SQL uniqueidentifier order. Each page scans at most `limit` route heads; therefore `features` may be empty **while `nextCursor` is non-null**. Continue every page to finish the selected registry/filter. Bbox tests line intersection, not endpoint containment. Technical limits: 4096 vertices/route and 20000 display vertices/page; over-limit/unsupported routes are explicitly unavailable, not outside scope. Metadata has bounded historical pins with a truncation indicator.

`unavailableRoutes` covers only scanned heads on that page. `bboxRelation: "UNKNOWN"` means a route lacking a supported location cannot be certified inside/outside the box. It does not certify absence of company responsibility. EPSG:32648/32649 use the checked WGS84 UTM adapter. Native profiles, SAMPLE transforms, unsupported CRS or invalid coordinates are unavailable; source SRID is never relabeled as WGS84.

Opaque cursor binds actor, filters, page size and source snapshot. The registry snapshot uses immutable route IDs/current flags and authoritative metadata/version pins; the existing SQL trigger prevents editing route geometry/CRS/version in place. Geometry bytes are fetched only for the bounded page. Registry metadata is still read conservatively across the authorized organization; unrelated authoritative metadata changes may require a reload. Large deployed registries have not been benchmarked. Snapshot changes return **409 `routing_cursor_stale`**; discard all cursor state and reload. Filter/actor mismatch or malformed cursor returns **400 `invalid_cursor`**. `snapshotVersion` / quoted ETag describe the specific page/filter; `If-None-Match` can return 304 only after current authorization. Revoked sessions cannot reuse cached authority.

## B — selected evidence versus canonical routes

`GET /cases/{caseId}/routing-candidates?reportId=...&evidenceId=...&limit=20&cursor=...`

Supervisor reads organization candidates; PM is restricted to the current Case project. Active Report–Case link, evidence/supplement ownership, private REPORT_PHOTO scope, VERIFIED and pinned file version are rechecked. Cross-Case sources return 404. No client coordinates are accepted.

`source` contains Case/Report/evidence/supplement/file identities and versions, whole-Case source fingerprint, exact GPS/source/accuracy, claimed captured time, server received time and `captureTimeProvenance: "CLIENT_CLAIMED"`. No selected evidence or missing GPS produces 200, empty candidates and explicit missing reasons; dependency failures remain failures.

`evaluationVersion` hashes source/link/file state, geometry/responsibility registry, algorithm and reader scope. It is not a capability and not derived from `evaluatedAt`. `algorithmVersion: "ROUTING_WGS84_UTM_GRID_V1"`. Pagination is by immutable route-version ID across all versions, without a business radius or top-N cutoff. Follow `nextCursor`; an incomplete page cannot establish outside scope.

Each candidate includes project/road/route/version, all segment-set pins, `geometryHash`, source coverage pins and nullable distance. Method `WGS84_UTM_GRID_MINIMUM_TO_CENTERLINE` is minimum distance on the original projected grid geometry, **not ellipsoid/ground distance**. Unsupported transforms/out-of-area coordinates produce null distance and specific reasons. No authoritative corridor is inferred from road widths, so spatial relation is UNKNOWN. Missing accuracy does not become zero; uncertainty remains UNKNOWN. Current data does not automatically bind the selected image to a complete responsibility scope or verified historical event time; responsibility is UNKNOWN with reasons. Coverage pins identify actual authoritative mappings and expose REAL_SOURCE/TEST_ONLY, exact obligation/location versions, origin project, current responsible project/item and current-source issues. Policy pins identify project revision/publication/revocation versions. These pins do not grant cross-project writes or prove that the selected evidence belongs to that obligation. Expired warranty alone does not imply no obligations.

Temporal basis explicitly says `SERVER_RECEIVED_AT_NOT_HISTORICAL_PROOF`. A new mapping/geometry/file/link changes evaluation. No GET writes jobs, matching decisions, links or dispositions.

## C — existing Case triage

`POST /cases/{caseId}/triage`

Keep existing body/authority. Supervisor triages an UNASSIGNED Case; PM selects method in its already assigned project. Supervisor cannot overwrite an assigned project. Additive fields: `reportId`, nullable `evidenceId`, `roadSectionId`, `routingEvaluationVersion`. A supplied source identifies **Case-level grounds**, not per-Report routing. Invalid lineage or stale sources fail; current active evidence/geometry are revalidated in the write transaction.

```json
{
  "projectId": "<selected-project>",
  "verificationMethod": "FIELD",
  "reason": "Field verification based on the reviewed road scope; selected image lacks GPS",
  "reportId": "<currently-linked-report>",
  "evidenceId": "<exact-selected-evidence>"
}
```

FIELD can use a grounded project/reason without GPS or evaluation. Supplying `roadSectionId` additionally requires the matching pinned route/segment set/geometry version, so the section cannot be asserted against an unrelated or missing geometry pin. If evaluation is actually used, supply its current `routingEvaluationVersion` and the selected project's `routeVersionId`, `segmentSetId`, `geometryVersion` from **geometry-package**; do not substitute map `geometryHash` or a segment-set rowversion. Headers: Case `If-Match`, `Idempotency-Key`, plus CSRF for cookie. Successful triage atomically appends source/supersession and bumps Case version. GET Case returns `routingSource`, recording the operation-time grounds; it does not silently retarget those pins after later source/link changes. Read current active Reports and current authority before taking another action. Old clients can continue without additive fields. Existing split rights are unchanged.

## D — intake decision and history

`POST /cases/{caseId}/routing-decisions`, Supervisor, UNASSIGNED only. Case `If-Match` and `Idempotency-Key` required; Web CSRF applies. Response 200 `{decision,caseVersion}` with new Case ETag. Persist exact body/key/If-Match before first send; retry unchanged after timeout. Changed body on the same actor/operation/key is 409 `idempotency_key_reused`. Replays recheck current authority and do not append history/audit again.

Replay returns the saved **operation-time** response, including its original Case version and decision state. It does not claim that an old decision is still effective after reconsideration, new evidence or triage. Always GET Case and routing history for current state and the next command's ETag. A moved/unavailable selected source or revoked authority can prevent replay access.

```json
{
  "outcome": "NEEDS_LOCATION_EVIDENCE",
  "reason": "The selected photograph has no usable position; request clarification",
  "reportId": "<current-report>",
  "evidenceId": "<selected-evidence-or-null>"
}
```

Allowed outcomes are NEEDS_LOCATION_EVIDENCE / OUTSIDE_COMPANY_SCOPE. Required reason, current Report, optional evidence only when truly absent, optional current evaluation when used. The decision stores server actor/time, original evidence snapshot, source fingerprints, reason, attestation and superseded decision ID. Neither outcome closes Case or automatically publishes/notifies/creates work.

Technical request limits: decision reason and each attestation reason are at most 2000 characters; `reports` contains 1–100 entries and each `sourceReferences` contains 1–20 entries. These are request bounds, not evidence of complete outside-scope coverage.

OUTSIDE additionally requires `attestation` with nonempty `scopeReason`, `conflictResolution`, `temporalBasis: "SERVER_RECEIVED_AT"` and `reports[]` covering **every active linked Report exactly once**. Every entry has `reportId`, optional selected `evidenceId`, explicit `locationReason`, `responsibilityReason`, and `sourceReferences[]`. References include supported valid `ROUTE_VERSION` with its map `geometryHash` and current `COVERAGE_MAPPING` with its returned coverage-pin `version`; matching obligation/Defect/route lineage, current real sources and receipt-time applicability are checked. No TEST_ONLY upgrade is permitted. The saved statement is the Supervisor's source-backed attestation, not machine verification of outside scope or a historical event. Missing/stale/unknown/unresolved sources block OUTSIDE; NEEDS or grounded FIELD remains possible.

`GET /cases/{caseId}` adds Supervisor `routingDisposition: {decisionId,outcome,state,decidedAt}`. On source changes: **`outcome: null`, `state: "REVIEW_REQUIRED"`**, never inherit an old OUTSIDE onto new evidence. Read immutable details through Supervisor `GET /cases/{caseId}/routing-decisions?limit=20&cursor=...`, ordered SQL ID ascending. History `state` distinguishes EFFECTIVE, REVIEW_REQUIRED and SUPERSEDED. Reconsideration appends a new decision and supersedes the old one. Triage appends a supersession in its transaction and removes effective disposition. PM/Reporter gain no internal attestation/history access. Supervisor Case-list items expose the same current disposition for an internal queue. Existing list query filters remain unchanged; filtering loaded items must follow all list pages before claiming a complete queue. No new server-side disposition query parameter is introduced.

## Errors and integration

401 current authentication/session required; 403 `access_forbidden` or cookie `csrf_failed`; 404 source/resource outside current linkage; 400 validation/invalid_cursor; 428 missing If-Match/key; 412 stale Case/file; 409 routing_cursor_stale, routing_evaluation_stale, routing_source_stale, routing_attestation_incomplete, routing_responsibility_unresolved, routing_location_unresolved, reference_scope_conflict or case_already_assigned. Read ProblemDetails `code` and retain correlationId. Do not treat 5xx as no candidates. After stale/conflict, reload Case/evidence/map/candidates, let Supervisor reconfirm, then create a new operation key; uncertain command outcome must first be reconciled/replayed.

FE order: Case -> Reports/exact evidence -> map pages -> candidates when position exists -> grounded triage or intake decision -> GET Case/history with current session after reload. Multiple Report positions must be reviewed explicitly. This feature does not introduce Supervisor split rights or project transfer.

## Deployment and verification ledger

Additive migration `20261126090000_ReporterRoutingMap`: new immutable CaseRoutingEvents table, no backfill or legacy Case rewrite. Deploy after verified DB backup/restore rehearsal and isolated upgrade, then backend, then FE with a fresh Web session. Preserve DB/volumes; do not seed user data. CURRENT_VERIFIED local verification (2026-11-26, Windows, SDK 10.0.401, analyzers enabled):

| Check | Actual result |
|---|---|
| Unit tests and TRX gate | 1312 PASS / 0 FAIL / 0 SKIP |
| Full API identity gate, current 9 lanes | 1771 PASS / 0 FAIL / 1 permitted SKIP; 1772 distinct identities |
| SQL identity gate, current 10 lanes | 897 PASS / 0 FAIL / 0 SKIP |
| Routing/migration/catalog focused runs | 13/13 PASS before and after the final service seam; these overlap full API and each other |
| EF 8.0.17 model parity + Development Seeder | Apply migrations; seed twice; identity and counts of all 204 assessed tables (including EF history) unchanged |
| Source contracts | All 14 scripted checks passed; unchanged server shell scripts passed Git Bash syntax checks |
| Format / whitespace | `dotnet format --verify-no-changes --no-restore` and `git diff --check` passed |

Builds use analyzers, including `dotnet build tests/RoadGuardSystem.ApiTests --no-restore --no-incremental` and the corresponding IntegrationTests build. Test invocations use `Invoke-ApiLane.ps1`, `Invoke-SqlLane.ps1`, `Verify-ApiLanes.ps1 -MatrixResult success` and `Verify-SqlLanes.ps1`. After integrating upstream CI balancing at 6257e61, API lanes 3/4/6 and SQL lanes 4/10 were rerun; unchanged identities/results were retained. No gate or skip list was relaxed.

The sole skipped test is `RoadGuardSystem.ApiTests.Files.UploadApiTests.UploadEndpoints_CompleteMultipartUploadAgainstConfiguredMinio`: external provider smoke is opt-in via `ROADGUARD_MINIO_SMOKE=1` and `MinioStorage__*`. Internal isolated MinIO tests were not skipped. These results are local backend evidence, not GitHub CI or UI acceptance. SQL schema inventory excludes EF history: 203 application tables, 19 migrations, signature `F98464ECC69892658D8CDA054233A02A5E648B7C73A470C4464049AB78427DD2`.

Final source SHA, remote branch verification, one-time CI status and the separately verified local image/backup/migration results are supplied with the delivery record. A Git push alone does not update a runtime. This document does not assert deployment to another environment. HTTP tests use isolated SQL and controlled metadata fixtures; no deployed document provenance or UI acceptance is claimed.

## Observed HTTP JSON (isolated fixtures)

The following complete response bodies were captured by authenticated HTTP tests against isolated SQL. IDs and timestamps belong to those fixtures, not to a project deployed for FE. Authorization tokens, cookies and operation keys are excluded.

Selected evidence with no GPS, HTTP 200:

```json
{
  "source": {
    "caseId": "8b4d6bc8-0af6-4b24-89ab-c0a44f86aff4",
    "caseVersion": "AAAAAAAAGhM=",
    "reportId": "8a3c6de9-67da-42dd-a8fd-6e7c054eb789",
    "reportVersion": "AAAAAAAAGhQ=",
    "evidenceId": "b86b0c7b-2e0e-4864-ba49-b97174dbe628",
    "supplementId": null,
    "fileId": "f5f950de-e6af-4af2-bb85-47b81329e0a5",
    "fileVersion": "AAAAAAAAGhI=",
    "caseSourceVersion": "cbfdd8e2e2f5454238198807a9c91ce558336a3e6aeb8a078a95ecf609f482a1",
    "latitude": null,
    "longitude": null,
    "accuracyMeters": null,
    "locationSource": "UNKNOWN",
    "capturedAt": null,
    "receivedAt": "2026-11-25T19:55:08.9088985+00:00",
    "captureTimeProvenance": "CLIENT_CLAIMED"
  },
  "evaluationVersion": "90e369c2af6f41e3d07d5c74ca4ce46bea4be15f3b9c4b276d692bdc9cd5a03e",
  "algorithmVersion": "ROUTING_WGS84_UTM_GRID_V1",
  "evaluatedAt": "2026-11-25T19:55:09.7905214+00:00",
  "candidates": [],
  "nextCursor": null,
  "completeness": "INCOMPLETE",
  "missingReasons": [
    "GPS_MISSING"
  ],
  "searchScope": "ORGANIZATION_ALL_VERSIONS"
}
```

Supervisor history after one NEEDS_LOCATION_EVIDENCE decision, HTTP 200:

```json
{
  "items": [
    {
      "decisionId": "9093da2e-2468-4887-9a7c-aa72506155a4",
      "outcome": "NEEDS_LOCATION_EVIDENCE",
      "reason": "Selected image has no position; request source clarification",
      "actorId": "26bedb4b-d3ab-48e2-a8c5-c66688707ed2",
      "decidedAt": "2026-11-25T19:55:06.4706378+00:00",
      "reportId": "d02bca82-35dd-4b67-a201-581a5e5f9e8b",
      "evidenceId": "2049b2fa-ba37-46d3-bb25-8f2f207a2ba5",
      "caseSourceVersion": "9e525b07cfe7478160ebd29273314deac16da1332f7f6dbcf8fa3f32e421d8b1",
      "routingEvaluationVersion": null,
      "temporalBasis": "SERVER_RECEIVED_AT",
      "supersedesDecisionId": null,
      "state": "EFFECTIVE",
      "attestation": null,
      "source": {
        "caseId": "a4cab9ae-693d-44dc-b9ec-fb1699638c5e",
        "caseVersion": "AAAAAAAAGcA=",
        "reportId": "d02bca82-35dd-4b67-a201-581a5e5f9e8b",
        "reportVersion": "AAAAAAAAGcE=",
        "evidenceId": "2049b2fa-ba37-46d3-bb25-8f2f207a2ba5",
        "supplementId": null,
        "fileId": "9c3ec8d4-affe-41ee-923a-78ed96c33d00",
        "fileVersion": "AAAAAAAAGb8=",
        "caseSourceVersion": "9e525b07cfe7478160ebd29273314deac16da1332f7f6dbcf8fa3f32e421d8b1",
        "latitude": null,
        "longitude": null,
        "accuracyMeters": null,
        "locationSource": "UNKNOWN",
        "capturedAt": null,
        "receivedAt": "2026-11-25T19:55:06.1947823+00:00",
        "captureTimeProvenance": "CLIENT_CLAIMED"
      }
    }
  ],
  "nextCursor": null
}
```

## Tin nháp gửi Hoàng (chưa gửi)

BE bổ sung bản đồ tuyến cho Supervisor, đối chiếu đúng ảnh của Report/bổ sung và quyết định tiếp nhận nội bộ. Web chọn đúng Report/evidence từ API chi tiết Case, đọc đủ các trang map/candidates, rồi triage hoặc ghi NEEDS_LOCATION_EVIDENCE/OUTSIDE_COMPANY_SCOPE. Dùng cookie qua credentials: "include"; POST cần X-CSRF-TOKEN, Case If-Match và Idempotency-Key. Sau lệnh, mở lại GET Case và lịch sử để lấy trạng thái/ETag mới.

FIELD vẫn điều phối được khi thiếu GPS nếu có lý do/phạm vi có căn cứ. Khoảng cách UTM chỉ hỗ trợ đối chiếu; UNKNOWN không phải ngoài phạm vi. OUTSIDE cần attestation có nguồn cho toàn bộ Report trong Case, không tự đóng, tạo việc hay thông báo Reporter. Nguồn đổi sẽ trả REVIEW_REQUIRED với outcome null. Không dùng map geometryHash thay geometryVersion của geometry-package.

Trang map rỗng vẫn có thể còn nextCursor. Tuyến native/CRS chưa hỗ trợ trả unavailable, không tọa độ giả. Cần kiểm UI bằng phiên Web mới sau khi backend và migration đã cập nhật; kết quả BE HTTP/SQL không chứng minh UI MapLibre, nguồn hồ sơ thực hay toàn bộ nghiệp vụ FE đã đạt. SHA/CI/runtime và kết quả kiểm cuối được cung cấp riêng trong bàn giao.

### Map and candidate excerpts

One actual `features[]` entry from HTTP 200 (an excerpt, not a complete page; retain the original page cursor):

```json
{
  "id": "58d2563d-cf52-48d2-a94e-0083e9088a0c",
  "geometry": {
    "type": "LineString",
    "coordinates": [
      [
        104.99087553850588,
        9.999999874725477
      ],
      [
        105.00912446149412,
        9.999999874725477
      ]
    ]
  },
  "properties": {
    "projectId": "ec52c33b-31ca-4b1b-a7bc-ccdf65ee07cf",
    "projectName": "Routing map",
    "projectStatus": "ACTIVE",
    "roadSectionId": "f5b08a99-596a-4ebd-aa75-7631e7d76791",
    "roadSectionCode": "R1",
    "roadSectionName": "Road map test",
    "routeVersionId": "58d2563d-cf52-48d2-a94e-0083e9088a0c",
    "versionNo": 1,
    "isCurrent": true,
    "effectiveFrom": "2026-11-25T20:10:43.2355862+00:00",
    "geometryHash": "5d15cf17b4ab82f7d16bcec56547c3969e74157bba0418777b070ab9c44884ff",
    "crsProfileRevisionId": null,
    "segmentSets": [],
    "obligationGeometryPins": [
      {
        "obligationId": "cda6e592-9dc2-47d9-930b-2e4f9d31effe",
        "routeVersionId": "58d2563d-cf52-48d2-a94e-0083e9088a0c",
        "locationVersion": "ROUTING_ISOLATED_FRAME"
      }
    ],
    "metadataTruncated": false,
    "geometryUsage": "DISPLAY_ONLY",
    "geometryStatus": "AVAILABLE"
  },
  "type": "Feature"
}
```

One actual `candidates[]` entry from HTTP 200. Coverage provenance comes from controlled isolated fixtures; it does not certify deployed documents. This excerpt does not replace following `nextCursor` on the original response:

```json
{
  "route": {
    "projectId": "ec52c33b-31ca-4b1b-a7bc-ccdf65ee07cf",
    "projectName": "Routing map",
    "projectStatus": "ACTIVE",
    "roadSectionId": "f5b08a99-596a-4ebd-aa75-7631e7d76791",
    "roadSectionCode": "R1",
    "roadSectionName": "Road map test",
    "routeVersionId": "58d2563d-cf52-48d2-a94e-0083e9088a0c",
    "versionNo": 1,
    "isCurrent": true,
    "effectiveFrom": "2026-11-25T20:10:43.2355862+00:00",
    "geometryHash": "5d15cf17b4ab82f7d16bcec56547c3969e74157bba0418777b070ab9c44884ff",
    "crsProfileRevisionId": null,
    "segmentSets": [],
    "coveragePins": [
      {
        "id": "566e7ec9-d953-46d5-b0b9-bb2621ae8fcb",
        "obligationId": "cda6e592-9dc2-47d9-930b-2e4f9d31effe",
        "locationVersion": "ROUTING_ISOLATED_FRAME",
        "provenance": "TEST_ONLY",
        "sourceKind": "MAINTENANCE_BASIS",
        "sourceId": "1a17010d-ab2d-48cb-afd7-649e24e36e11",
        "sourceVersion": "ca3480882499c3f5415cea6b6ff86606121385ee44507ce4e9f8a95c6c240f7b",
        "applicableFromUtc": "2026-11-24T20:10:47.1468755+00:00",
        "applicableToUtc": "2026-11-26T20:10:47.1468755+00:00",
        "version": "41bdb3bce5a7e7487bce01e1e66e78da3f5669a73f3bf965f83579a54d36c226",
        "originProjectId": "ec52c33b-31ca-4b1b-a7bc-ccdf65ee07cf",
        "currentResponsibleProjectId": "ec52c33b-31ca-4b1b-a7bc-ccdf65ee07cf",
        "currentRepairItemId": null
      }
    ],
    "policyPins": [],
    "geometryUsage": "DISPLAY_ONLY"
  },
  "distanceMeters": null,
  "distanceMethod": null,
  "spatialRelation": "UNKNOWN",
  "uncertaintyStatus": "UNKNOWN",
  "responsibility": "UNKNOWN",
  "temporalBasis": "SERVER_RECEIVED_AT_NOT_HISTORICAL_PROOF",
  "responsibilityAt": "2026-11-25T20:10:52.6211873+00:00",
  "missingReasons": [
    "AUTHORITATIVE_CORRIDOR_UNAVAILABLE",
    "EVIDENCE_SCOPE_NOT_CONFIRMED_FOR_RESPONSIBILITY",
    "VERIFIED_EVENT_TIME_UNAVAILABLE",
    "SOURCE_OUTSIDE_SUPPORTED_CRS_AREA",
    "TEST_ONLY_MAPPING"
  ]
}
```
