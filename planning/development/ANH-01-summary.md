# ANH-01 Delivery Summary

Status: **ANH-01 Partial**, revision 2. Metric producer through manual partial
baseline is implemented and tested; the remaining gates below prevent Done.
Branch: `anh-review`. Base: `1ecae797caaed1ab912b02b2372a1940d1e05375`.
HEAD: the delivery commit containing this summary, reported in the handoff.
Initial dirty paths: none. No unrelated tracked changes were present.

## External Review Correction — 2026-10-02

### Final Root Scope Correction — Part A

Owner continuation accepted external closure of the other four fixes. Actual
correction base/initial HEAD: `7e73e872634942cd8590b2685258fb204de733c6` on
`anh-review`; dirty paths none. The final correction HEAD is the separate
Part A commit reported in the handoff (before shared Huy integration).
Migration `090000` remained unchanged; its applied state is explicitly tested.
New `20261002100000_Anh01RequestScopeRootCorrection` inspects the raw Request
root object and requires exactly one binary exact `scope` key of array type.
It revokes unsafe BAND_V1 adoption, preserving JSON, relational references,
receipts and wrapper fields. Guarded Down returns 51027.

Runtime reproduction: SQL migration 090000 adopts first-valid/last-empty and
duplicate-identical scopes, while JsonElement selects the last property.
Reverse order, root array/object scope, wrong key case, legitimate accessPoint
wrapper and leading JSON whitespace are covered. Red: 1 passed/1 failed;
final green: 2 passed/0 failed/0 skipped on fixture-owned SQL Server.

```powershell
dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~Anh01ScopeAdoptionCorrectionTests -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=anh01-root-scope.trx'
dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-restore -v quiet -clp:ErrorsOnly
git diff --check
```

Self-review 1 fixed valid leading JSON whitespace handling and added a test.
Self-review 2 checked exact key/type, forward-only history, source preservation,
recovery and scope. This is BE SQL evidence, not external review PASS or shared
DB rollout. ANH-01 remains Partial; CRS/Huy/MinIO/8GiB/demo gates unchanged.

External ChatGPT source review received via the owner's five findings. Review
base: `1ecae797caaed1ab912b02b2372a1940d1e05375`; reviewed HEAD and correction
base: `663ff691b83b868cc7be9421e54f675af64ea8a8`. Resume observed that exact
HEAD on `anh-review`, with no dirty paths or intervening delta; no reset.
Correction HEAD is the new commit containing this update, supplied in the
handoff/compare URL. Status remains **Partial**; D1-D5 and all remaining
gates below are unchanged. The next external correction review is PENDING.

| Finding | Runtime reproduction and correction | Acceptance evidence |
|---|---|---|
| P1 ended assignment lookup | HTTP decline ended the assignment; PM GET returned 404. Lookup now includes ended history for management. Operator read/accept/submit require active assignment. The latest declined Operator can reach decline receipt only while still a member and not replaced. | A17: `Anh01SurveyCorrectionTests`: PM read/reassign, new Operator accept; same-key replay, different payload/fresh-key conflict; revoked membership and old Operator after reassignment deny before replay. SQL has two assignments/one active and one decline receipt. |
| P2 no-op set edit | Same definition/new key returned new segment IDs with unchanged parent ETag. No-op now keeps IDs/ETag; changed definition changes parent version. | A13: `Anh01GeometryWorkflowTests`: IDs/ETag, same-key replay, changed edit/stale publish 412, restoration/publication/history. |
| P2 unsafe BAND_V1 adoption | Isolated SQL explicitly applies the old migration: relational `[A,B]`, snapshot `[A,A]` adopts BAND_V1 for both Plan/Request. New forward correction verifies array/object/property shape, tuple/ID uniqueness and equality in both directions, plus real scoped references. Invalid prior adoption becomes null; JSON, IDs and relational rows remain unchanged. | A16/legacy: `Anh01ScopeAdoptionCorrectionTests`, two theory cases with valid/reordered and malformed/duplicate/mismatched/legacy fixtures; guarded Down 51026 leaves migration applied. |
| P2 stale new-work route | Real V2 confirm/publish followed by new V1 task returned 201. ResolveScope admission now requires current route as well as published set. | A15: new V1 plan/task rejected without SQL rows; historical task read/work/accept preserved. `Anh01SurveyFlowTests` replaces route through APIs before historical parent supplement, child accept/upload/submit. |
| P2 incomplete legacy read | Baseline legacy rows migrated unchanged; geometry-package returned 422. Set/package reads now preserve actual nullable data, refs and sequence, with metadataStatus/missingMetadata. Geometry commands requiring completeness remain blocked. | Legacy/A13/A16: `Anh01LegacyGeometryCorrectionTests`, baseline migration → authenticated HTTP 200/incomplete → preview 422, no synthesized metadata, geometry, offsets or receipt. |

Migration history choice: the original `Anh01GeometrySurveyReview` file is
unchanged. It was already committed and applied in prior isolated fixtures;
the new test confirms its history entry before forward correction. Shared/
deployed application history remains UNKNOWN and was not queried. Correction
`20261002090000_Anh01ScopeAdoptionCorrection` is data-only, leaves the model
snapshot unchanged and refuses unsafe downgrade rather than restoring
ambiguous adoption. Applying any migration to shared/deployed DB is NOT RUN.

Correction self-review pass 1: auth/state/replay/version and SQL shape checks.
Fixed the completeness guard before publish, and SQL trailing-space padding
in band/GUID validation after an internal review finding and SQL reproduction.
Pass 2: scope/ownership, migration history/recovery, legacy nullable reads,
historical task/supplement compatibility, existing receipt behavior and
Postman/adoption evidence. Root is sole writer of shared migration and fixture
changes; survey delegate wrote only survey service/repository/view/tests.
Internal scoped source review is separate from external ChatGPT review.

### Correction Verification — CURRENT_VERIFIED

```powershell
dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-restore -v quiet -clp:ErrorsOnly
dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-restore --filter 'FullyQualifiedName~Projects|FullyQualifiedName~Surveys|FullyQualifiedName~Files|FullyQualifiedName~Anh01' -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=anh01-correction-unit.trx'
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-restore --filter 'FullyQualifiedName~Projects|FullyQualifiedName~Warranties|FullyQualifiedName~Surveys|FullyQualifiedName~Files' -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=anh01-correction-api.trx'
dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-restore --filter 'FullyQualifiedName~Anh01ScopeAdoptionCorrectionTests|FullyQualifiedName~Anh01FileMigrationTests|FullyQualifiedName~FileSchemaContractTests' -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=anh01-correction-integration.trx'
./.tools/dotnet-ef.exe migrations has-pending-model-changes --project RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj --startup-project RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-build
python -m json.tool docs/postman/RoadGuardSystem-V2.postman_collection.json > $null
git diff --check
```

Final build: 0 errors/0 warnings. Unit: **60 passed/0 failed/0 skipped**.
API: **35 passed/0 failed/1 skipped** (live MinIO provider smoke not configured).
Integration: **10 passed/0 failed/0 skipped**; adoption theories each cover
22 valid/ambiguous shape cases for Plan and Request, old migration application
and guarded correction Down. Final API/integration runs include the padding
fix. EF used a temporary design-only dummy connection, restored afterward,
and reported no pending model changes; it did not connect or migrate a DB.
Postman JSON parses; structural comparison with correction base confirms only
two read assertions were added and all 27 request identities/content retained.

Red evidence: geometry no-op test 1 failed on changed IDs; legacy HTTP test
1 failed on 422 instead of 200; survey correction tests 2 failed on PM 404
and stale-route task 201. Old migration's unsafe adoption is directly asserted
before applying correction in both SQL theories. Internal padding finding
was reproduced by 2 failed theories (snapshot-band-space still BAND_V1),
then corrected and included in final green integration results.

**BE verified:** authenticated HTTP → fixture-owned SQL for A13/A15/A17 and
legacy incomplete read; migration adoption/reclassification for A16 with
unchanged snapshots/refs. **Mock verified:** existing storage/video and large
metadata fixture portions of the historical supplement flow. **NOT RUN:**
live Postman runner, real MinIO/8 GiB transport/demo, Huy/AI/Android/deployment,
WGS84/GPX transforms and shared DB history/application. No new approval of
those gates, dependencies, identity/processing policy or Huy contract.

Earlier fixture failures (required identity seed columns) and concurrent test
output lock, plus initial raw-string compile errors, were repaired; they are
not runtime reproduction evidence. The test fixture can start at an explicit
baseline migration in its newly owned DB; existing default setup still uses
latest. Original migration/designer/model snapshot and D1-D5 spec are unchanged.

## Result And Scope

- Reuse existing project/road/warranty APIs; preserve legacy routes. New PM
  drafts, Supervisor confirmation, metric width buffers and PM segment
  publication preserve immutable route/source references and historic sets.
- Purpose/MIME limits, long bytes end to end, URL replay/expiry, serialized
  multipart initialization, transient verification recovery and protected
  streaming. No signed URLs are persisted in receipts/logs.
- Current membership/assignment, active shared drone registry, plan linking,
  discriminator fail-closed, dataset detail/pairs and 32 GiB bounded sum.
- Supplement child assignments; parent cannot be reopened for new source.
  Immutable PM evidence assessments, latest coverage, partial baseline,
  compare-current concurrency and durable history.
- Separate M1 widening with guarded down; combined geometry/survey candidate
  adds immutable triggers, legacy audits and conservative scope adoption.
  All migration execution used fixture-owned Testcontainers databases.
- 27 additive Postman requests/public environment variables, local contract
  adoption record, researched fictional PDF dossier and synthetic MP4/SRT.

Anh exclusively wrote shared migrations/snapshot, DI, adoption and Postman.
No identity/processing policies, Huy branch, develop/main, FE locks or
shared/deployed data were changed. No package/framework upgrades.

## Review Evidence

Self-review 1: contract/auth/state, transaction/replay/concurrency, immutable
source and bytes. Fixed URL replay mapping, expired receipt handling, CRS
validation, source-coordinate snapshot, alias confirmation, long metadata,
dataset overflow/rejection and current assignment checks.

Self-review 2: final scope/ownership, old API compatibility, migration
preservation/down guard, reads/producer shape, docs/Postman and demo recovery.
Fixed full-package ETags, deduplicated geometryRefs, malformed ETags, service
interfaces and demo complete using the refreshed upload version.

Internal independent review found one P2 supplement-parent reassignment
bypass. HTTP test reproduced 200 before fix and 409 after. Reassign, new
upload and dataset guards close it; scoped internal re-review found no
additional issue. This was an internal review; the later external review and
its correction are recorded above, with follow-up external review PENDING.

## Original Delivery Verification — 663ff691

Commands below ran from the repository root; tests used `--no-restore -v
quiet -clp:ErrorsOnly` and TRX loggers. No zero-selected-test PASS claims.

```powershell
dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-restore -v quiet -clp:ErrorsOnly
dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-restore --filter 'FullyQualifiedName~Projects|FullyQualifiedName~Surveys|FullyQualifiedName~Files|FullyQualifiedName~Anh01' -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=anh01-unit.trx'
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-restore --filter 'FullyQualifiedName~Projects|FullyQualifiedName~Warranties|FullyQualifiedName~Surveys|FullyQualifiedName~Files' -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=anh01-api.trx'
dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-restore --filter 'FullyQualifiedName~UploadPersistenceSqlTests|FullyQualifiedName~Anh01StorageRecoveryTests|FullyQualifiedName~FileSchemaContractTests|FullyQualifiedName~Anh01FileMigrationTests' -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=anh01-integration.trx'
git diff --check
python -m py_compile tools/demo/anh01/setup_demo.py
python tools/demo/anh01/setup_demo.py --validate-only
```

Build: passed, zero errors; last incremental run zero warnings (earlier full
compile emitted analyzer warnings, including existing code/generated migration).
Unit: 60 passed/0 failed/0 skipped. API: 32 passed/0 failed/1 skipped (MinIO
provider smoke). Integration: 13 passed/0 failed/0 skipped. Affected flow
retests passed after review fixes. EF 8.0.17 local tool confirmed no pending
model changes using the design-only factory connection, without DB writes.
Postman JSON parsed: 27 new requests; existing identifiers retained. Demo
facts/hashes/script validated; dossier render/media checks are documented in
the demo README. Earlier red/fixture failures were fixed and superseded.

**BE verified:** real authenticated HTTP/isolated SQL geometry publication,
plan/task, dataset, assessment, partial baseline/supplement, stale/replay,
dataset/baseline/publication races, revoked membership and old API regression.
M1 preserves existing metadata/trigger; unsafe down is blocked. Concurrent
multipart initialization/resume/expired URL receipt tested against SQL.

**Mock verified:** object storage verification/recovery, synthetic media and
8 GiB/32 GiB metadata fixtures. These do not prove real large-file transport.
**External/deployment not verified:** live MinIO/AI/Android/Huy consumption,
deployed schema/readers and owner acceptance.

## Remaining Gates And Limitations

- CRS dependency decision asked under spec 5.2: ProjNet 2.1.0 approval is
  pending. Only genuine metric coordinates matching EPSG:32648/32649 are
  supported; WGS84/GPX not implemented and no fabricated transforms returned.
- Crew geometry access/Huy producer-consumer integration requires the scoped
  task interface and actual consumer; no mock consumer is called PASS.
- MinIO provider smoke skipped; real 8 GiB streaming/transport not run.
  Crash after external initiate but before active ID save can leave an unknown
  orphan multipart upload. No exactly-once storage/retention claim.
- Registry/device and Operator membership provisioning are fixture prerequisites;
  no new provisioning contract was authorized. Clip interval validation is
  structural only because trusted media duration is unavailable.
- Demo setup against a real local API/MinIO not run. Initial handover attachment
  has the existing project/upload ordering gap; dossier attaches privately as
  warranty source. No unknown-date/multi-document workflow was invented.
- The complete A01-A23 matrix is not fully evidenced by focused tests; external,
  large-transfer and remaining CRS acceptance must close before ANH-01 Done.
  DB shared/deployed rollout still requires its separate authorization.
