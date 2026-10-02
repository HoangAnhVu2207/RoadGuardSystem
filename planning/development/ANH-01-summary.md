# ANH-01 Delivery Summary

Status: **ANH-01 Partial**, revision 2. Metric producer through manual partial
baseline is implemented and tested; the remaining gates below prevent Done.
Branch: `anh-review`. Base: `1ecae797caaed1ab912b02b2372a1940d1e05375`.
HEAD: the delivery commit containing this summary, reported in the handoff.
Initial dirty paths: none. No unrelated tracked changes were present.

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
additional issue. This is not external ChatGPT review, which is **PENDING**.

## Executed Verification

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
