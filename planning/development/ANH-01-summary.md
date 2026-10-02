# ANH-01 Delivery Summary

Status: **ANH-01 Partial**, revision 2. Metric producer through manual partial
baseline is implemented and tested; the remaining gates below prevent Done.
Branch: `anh-review`. Base: `1ecae797caaed1ab912b02b2372a1940d1e05375`.
HEAD: the delivery commit containing this summary, reported in the handoff.
Initial dirty paths: none. No unrelated tracked changes were present.

## Anh shared integration for HUY-01 - Part B

Status **PARTIAL**. Named shared writer: Anh (owner continuation2026-10-02),
Huy retains domain/module business ownership. Integration base/initial HEAD
`5d6ecb5c6c4498c4803ddec735a00b153d0967b5`; initial dirty none after separate
Part A commit. Final integration HEAD is `959d0080de26911795055f46538819a819a24c9c`;
this summary is included in that delivery commit. No unrelated path was changed.

Huy source handoff `8676226cf9d1b99adf83beb0391f147dfa4b112e`; common base
`1ecae797caaed1ab912b02b2372a1940d1e05375`. Fetched exact SHA and selectively
restored Reports/Cases/Candidates domain directories, their two unit tests and
HUY-01 spec. Domain/test27files were compared against that SHA unchanged.
Auth transport/refresh and label files were excluded. Huy's external domain
PASS at `ad52f4cae1d2f52f030ea23d59866004001c5aa6` is historical evidence;
external review of this integration is PENDING.

**Frozen/implemented producer v1:** exact signatures and record inputs/outputs
are in `RoadGuardSystem.Services/Interfaces/Integration/IAnhHuyProducerService.cs`
(namespace Services.Integration). HUY-01's shared reservation table records
producer Anh / consumer Huy, authoritative SQL/service, owner/project/recipient
scope, server checksum/bytes/version/provenance, exact failure mapping and
fixtures. Methods: ResolvePrivateEvidenceAsync, ResolvePublicationEvidenceAsync,
ResolveGeometryAsync, ResolveCandidateSourceAsync. They are real read facts,
not transaction locks or Huy command adapters. Publication checks exact
recipient/evidence projection and its persisted file version. Geometry emits
actual ordered segment adjacency and package hash/current refs. Report source
version covers Report/Case/head/geometry and verified immutable evidence refs.
AI/FIELD provenance absent -> SourceNotReady; no fabricated facts/GPS/CRS.

**Shared model/migration:** reviewed domain types mapped directly; new support
rows in Repositories/Models/Huy01/HuyIntegrationRows.cs carry FK relationships,
not competing aggregates. Migration `20261002120000_AnhHuySharedIntegration`
is additive and ordered after Part A. Legacy project FileScopes unchanged;
null project only for private REPORT_PHOTO/target-null. Original report fields,
evidence/supplements, conclusions/publications/recipients/history/decisions and
file scope are immutable. Typed evidence/source/correction composite FKs,
Report-owner equality and evidence identity namespace checks fail closed;
filtered single active Report link and single current disposition head.
Down refuses received Report/Case/decision/privatefile data. Raw migration
FK/trigger constraints are intentional SQL extensions to EF model snapshot.
Shared/deployed migration history was not queried/applied.

**DI/HTTP/Postman:** only real ReporterEvidenceService/PersistenceService and
AnhHuyProducerService/FactsRepository registered. Seven private producer HTTP
routes adopted in existing contracts/http/anh01.local-contract.md and new
Postman folder; all previous folders/identifiers unchanged (JSON equality
checked). Active SQL Reporter before private commands/replay; other-owner404,
pending/failed/version-drift content409 source_not_ready, stale complete412.
Triage keeps scope private; selected publication recipients alone download.
No new Huy business route/DI, event, label reader/exporter or FE lock activation.

**Huy next implementation order:** exact shared shadow fields/support rows and
signatures are recorded in HUY-01 "Shared mapping and consumer handoff v1".
Consume real producers -> implement owned module seams -> compose one SQL
transaction -> register actual module DI -> execute HTTP/SQL atomicity tests
-> adopt implemented Huy routes/agreed events. Re-resolve/lock facts and
current authorization before receipt replay/commit. Populate typed decision
shadows and update current head with RowVersion; increment Report/Case Revision
for every child mutation. Sync snapshot collections and FK refs atomically;
close/create active links with history once by Id. LinkHistory requires query
FromCaseId OR ToCaseId and an explicit Huy materialization hook if a reloaded
command needs the history; reviewed domain was not modified to add it.
Validate classification segment belongs to resolved route/project; independent
FKs alone do not prove that relation. Compose CasePublicationRecipientFacts
per Reporter, persist immutable recipient evidence refs, never case-wide rights.

**Two self-review passes:** pass1 checked source import/ownership, nullable
private scope vs existing project upload, owned EF hydration/FKs, version and
replay authority, migration ordering and recovery. Runtime tests found EF
Concat client-record translation failure; fixed separate SQL queries, plus
tracked geometry stale reads; fixed explicit AsNoTracking read route/set query.
Pass2 checked privacy/version drift, binary immutability, correction/link
identity and unsafe Down. Fixed wrong-project check before incomplete facts,
publication snapshot-version/owner/private-scope check, current SQL Reporter
MustChangePassword denial, Report case/trailing-space immutability,
closed-link rewrite/reopen guard, same-source/project correction FK and
SourceDecisions Down guard. Focused tests rerun after affected fixes.
Delegate source reviews are internal; no external PASS claimed.

### Part B verification - CURRENT_VERIFIED

Final affected unit: **76 passed/0 failed/0 skipped**, including16 unchanged
Huy domain tests. SQL: **19 distinct passed/0 failed/0 skipped** (18 combined
focused +1 new empty integration Down/Up). New schema has4cases, all included
in19. Fresh/baseline upgrade, hydration, typed/owner/identity FKs, singlelink/
head, two-context stale versions, immutable binary source/history and SQL
rollback/recovery verified. Downgrade testing reproduced SQL5074 index/FK
nullability dependency; migration now drops/recreates those dependencies and
removes the fabricated Guid-empty default. Combined18 rerun passed; additional
empty Down->Up1 passed, preserving actual prior schema. Earlier failing
query/cache/fixture/down runs are superseded, not counted as PASS.

Affected API: **18 passed/0 failed/1 skipped** (unconfigured live MinIO smoke).
Eight producer cases are included in18, not additional tests. Report/Case/
publication relation setup is explicit fixture SQL, not Huy commands. Actual
HTTP+SQL BE verified; object storage verification/download is mock verified.
Postman7requests added +10 empty/public environment placeholders; previous
folders/items preserved by JSON equality. Actual Postman runner NOT RUN.

```powershell
dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-restore --filter 'FullyQualifiedName~Huy01|FullyQualifiedName~Projects|FullyQualifiedName~Surveys|FullyQualifiedName~Files|FullyQualifiedName~Anh01' -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=anh-huy-focused-unit.trx'
dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-restore --filter 'FullyQualifiedName~Huy01SharedSchemaTests|FullyQualifiedName~Anh01ScopeAdoptionCorrectionTests|FullyQualifiedName~Anh01FileMigrationTests|FullyQualifiedName~UploadPersistenceSqlTests|FullyQualifiedName~Anh01StorageRecoveryTests|FullyQualifiedName~FileSchemaContractTests' --nologo -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=anh-huy-focused-sql.trx'
dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~Huy01SharedSchemaTests.Empty_integration_down --nologo -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=anh-huy-empty-recovery.trx'
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-restore --filter 'FullyQualifiedName~Anh01|FullyQualifiedName~ReporterEvidence|FullyQualifiedName~AnhHuy|FullyQualifiedName~UploadApiTests' -v minimal -clp:ErrorsOnly --logger 'trx;LogFileName=anh-huy-affected-api.trx'
dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~Huy01SharedSchemaTests -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=anh-huy-final-schema.trx'
dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-restore -v quiet -clp:ErrorsOnly
./.tools/dotnet-ef.exe migrations has-pending-model-changes --project RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj --startup-project RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-build
python -m json.tool docs/postman/RoadGuardSystem-V2.postman_collection.json > $null
python -m json.tool docs/postman/RoadGuard.local.postman_environment.json > $null
git diff --check
```

EF generation/check use a temporary dummy design-only connection, restored
afterward; no shared/deployed DB access. Applied migration files stay unchanged.
Final build: **0 errors/70 warnings** (recompiled metadata); no warning-clean
claim. Final EF check: **no pending model changes**. EF8 generation omitted
owned FK DeleteBehavior in designer/snapshot although the migration SQL and
runtime mapping already used Restrict. The two metadata declarations are
explicitly restored with comments; preserve them on future regeneration.
No SQL body/policy was changed for this metadata correction. New schema4cases
were rerun after metadata alignment (included in19, not added to total).

**NOT RUN / remaining gates:** producer -> actual Huy Report/Case/Candidate
command integration and audit/receipt/outbox atomicity, AI/FIELD authoritative
provenance, label persistence/read/export and event consumers; live MinIO,
real8GiB transfer, real local demo, Android/FE/external/deployment, CRS dependency
approval/WGS84/GPX. No shared DB update. Schema snapshot/ref equality and
parent child version bump require Huy's repository orchestration. AI-only
Down recovery fixture is not run (guard covers SourceDecisions; existing Report/
Case data recovery is tested). Both packages remain Partial.

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


## Shared receipt authorization seam — handoff 2026-10-03

**Shared seam CURRENT_VERIFIED; external ChatGPT review PENDING. Reporter P1
remains OPEN/PENDING; ANH-01/HUY-01 remain Partial.** This is the explicitly
assigned shared dependency follow-up after ANH-02, not activation of a Huy
consumer or acceptance of its real Reporter authorization flow.

Preflight: branch `anh-review`; exact initial base/local HEAD and remote
`origin/anh-review` both `530d2655bf9ccd07654132490549363fc3bfeb22`; dirty
paths none. Read-only `origin/huy-review` and reviewed Huy checkpoint both
`b8ec845d69ba2c71249cec6fb15e13a5bad8126f`. Read AGENTS, active manifest,
four rules/module route, current summaries/callers, ANH-02's existing
Serializable delta, and HUY-01 **at that exact SHA**, section "Reporter intake
receipt-authorization and validation delta". Its two validation/test-isolation
P2s are already CLOSED; its receipt authorization P1 is not. Retired RoadGuard
skills were not activated. Anh is the expressly assigned shared service/test
writer; Huy retains its adapter and Reporter module.

**Exact implementation/integration SHA:**
`f626ea595420c3f28a35f2b3f4c6f196d313a7be`.
[Shared code/test diff for review](https://github.com/HoangAnhVu2207/RoadGuardSystem/compare/530d2655bf9ccd07654132490549363fc3bfeb22...f626ea595420c3f28a35f2b3f4c6f196d313a7be).
This handoff is a subsequent documentation-only commit; its exact delivery HEAD
is supplied in the final handoff. The integration target is the immutable SHA
above **after ChatGPT review**, not an automatic merge of either branch.

Changed files (entire package):
`RoadGuardSystem.Repositories/Idempotency/IdempotencyOperationService.cs`;
`tests/RoadGuardSystem.IntegrationTests/Persistence/ReceiptAccessGuardSqlTests.cs`;
this handoff section only. ANH-02 source/summary and earlier ANH-01 evidence and
limitations are preserved. No Huy source/spec/DI, schema/migration, event/outbox,
identity/processing module, shared database or new RF/ZIP/summary was changed.

The original **seven-parameter overloads** are retained, including positional
`CancellationToken`, `default` literals and existing reflection consumers. A
caller without a guard retains prior behavior. Both methods additionally expose:

```csharp
Task<IdempotencyOperationResult> ExecuteAsync(
    Guid? actorUserId, Guid? projectId, string operation, string idempotencyKey,
    string requestFingerprint,
    Func<CancellationToken, Task<(Guid OperationId, string OutcomeJson)>> operationHandler,
    CancellationToken cancellationToken,
    Func<CancellationToken, Task>? receiptAccessGuard);
// ExecuteSerializableAsync exposes the identical additive parameter list.
```

The callback is optional through overload selection (or explicit null). Huy
passes `operationHandler, cancellationToken, receiptAccessGuard: Guard`.
On **every found receipt**, initial lookup, each execution-strategy attempt,
duplicate-key recovery and durable commit/acknowledgement recovery call the
guard before mapping replay **or conflict**; a found receipt never invokes the
create handler. Guard + mapping + commit run in a short service-owned transaction
inside the execution strategy. The guard must use the **same scoped DbContext**,
fresh authoritative reads and ordered locks (`UPDLOCK,HOLDLOCK` in the SQL
harness); it must not open/complete transactions, save business writes or reuse
preflight authority. Locks last through mapping/commit; result returns after
completion. New-write isolation/atomic business + audit + receipt remain intact.

After failed commit the old transaction is disposed before durable lookup and
before a new guard transaction. Durable discovery still uses `CancellationToken.None`
to determine SQL durability, but the guard/its transaction receive the **original
command token**; cancellation cannot turn into a protected success. Callback
exceptions/cancellation are rethrown as the same exception instance with preserved
stack semantics. A private non-retryable boundary prevents a guard's transient or
unique-constraint exception entering provider retry/duplicate recovery. Infrastructure
retry can invoke a successful read guard again, so it must remain free of write
side effects and reread current authority on every invocation.

**Self-review 1 (actual):** traced all four paths, guard-before-outcome/conflict,
short transaction/locks, strategy boundary, old-transaction disposal, original
cancellation token and exception propagation, duplicate recovery and atomicity.
Found the existing `P202ServiceContractTests` reflection consumer requiring exactly
seven arguments; replaced the initial signature extension with retained old and
additive new overloads. Added conflict during durable recovery, transient/SQL-unique
guard errors and recovery cancellation checks. No receipt path skips the guard.

**Self-review 2 (actual):** reviewed final service/test diff, ANH-02 Serializable
callers and legacy reflection/positional callers, per-actor effect/audit/receipt
counts, fixture-owned SQL databases and per-context fault interceptors, writer
allowlist and preserved summaries. Fixed a test indentation issue; no additional
behavior finding. No Huy adapter, DI binding, fake consumer or event was activated.
These are Codex self-reviews, not external ChatGPT approval.

Fresh commands/results (from repository root; SQL writes only fixture-owned
Testcontainers databases; counts are cases, not requests):

| Exact command | Executed / pass / fail / skip |
|---|---|
| `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-restore --disable-build-servers --filter 'FullyQualifiedName~ReceiptAccessGuardSqlTests.Every_receipt_path' -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=receipt-guard-red-corrected.trx'` | Corrected red on old source: 16 / 0 / 16 / 0, missing guard (no exception or guard count zero) |
| `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-restore --disable-build-servers --filter 'FullyQualifiedName~ReceiptAccessGuardSqlTests' -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=receipt-guard-green.trx'` | Initial green: 25 / 25 / 0 / 0 |
| `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-restore --disable-build-servers --filter 'FullyQualifiedName~ReceiptAccessGuardSqlTests\|FullyQualifiedName~P202ServiceContractTests\|FullyQualifiedName~P202TransactionAndIdempotencyTests\|FullyQualifiedName~Anh02Retention\|FullyQualifiedName~Anh02Export' -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=receipt-guard-regression.trx'` | Before additional cases: 60 / 60 / 0 / 0 |
| `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-restore --disable-build-servers --filter 'FullyQualifiedName~ReceiptAccessGuardSqlTests\|FullyQualifiedName~P202ServiceContractTests\|FullyQualifiedName~P202TransactionAndIdempotencyTests\|FullyQualifiedName~Anh02Retention\|FullyQualifiedName~Anh02Export' -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=receipt-guard-final-sql.trx'` | Final: **67 / 67 / 0 / 0** = shared guard32 + existing service contract16 + atomicity6 + ANH-02 retention9 + export4 |
| `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-restore --disable-build-servers --filter 'FullyQualifiedName~Anh02\|FullyQualifiedName~IdempotencyPerCommandCharacterizationTests\|FullyQualifiedName~P120ProjectCreationTests\|FullyQualifiedName~P120WarrantyCreationTests' -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=receipt-guard-api.trx'` | **24 / 24 / 0 / 0** = ANH-02 HTTP6 + per-command10 + project5 + warranty3 |
| `dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-restore --no-incremental --disable-build-servers -v quiet -clp:ErrorsOnly` | Exit0, 0 errors, 98 existing warnings |
| `git diff --check`; `git diff --cached --check`; selective final diff review | Exit0, no whitespace errors |

SQL evidence: all four receipt paths allow current access and deny after stale
preflight/revoke before outcome; replay/conflict have identical protection.
Acknowledgement loss recovers its own matching receipt; the same durable-recovery
branch also guards a conflicting competing receipt after failed commit (the first
transaction is asserted disposed before the lookup). Same-key concurrent contexts
plus acknowledgement loss leave exactly one durable Project effect, one audit and
one receipt. Precommit failure leaves zero of all three. Ordinary and every recovery
path propagate guard cancellation without handler reinvocation or new durable rows;
transient and actual SQL-unique guard exceptions escape unchanged. The lock-race
case observes a real `LCK_M_%` wait, blocks revoke through receipt transaction commit,
then denies the next replay after revoke. Serializable and legacy no-guard callers
and real ANH-02 HTTP/SQL callers pass regression. The final **91 distinct cases**
pass; earlier overlapping runs are not added to that count.

Failure history: the first red command (`receipt-guard-red.trx`) initially compiled
zero cases due to a missing Storage import and two cancellation-forwarding analyzer
errors; fixture fixes then ran16/failed16, mixing missing-guard failures with an
incorrect string assumption about the real tinyint `Users.Status`. The corrected
red above eliminates that fixture error. First green compilation selected zero
cases due to CA1068; private token parameters were reordered and the two additive
public overloads have narrow, documented compatibility suppressions. Final runs
have no unresolved failure or skip. The red-only reflection shim was removed;
committed tests call the production overload directly.

**NOT RUN / remaining ownership:** real Reporter controller/service/repository
adapter and authenticated Reporter revoke/file-owner/scope/VERIFIED/version/race/
recovery acceptance. The SQL guard above is an explicit shared test callback on
real authority rows and Project/audit effects; it does not prove Report/Case/link
producer→consumer integration or Huy's privacy-safe HTTP errors. Huy must supply
its same-context, ordered-lock adapter, integrate the reviewed exact SHA, pass the
callback at its service call, and run those real acceptance cases before Reporter
P1 can close. No Reporter composition-root activation or `report.received.v1`
emission is authorized by this handoff. Full solution tests, live MinIO/8GiB/CRS/
demo/deployment, shared/deployed migration and external ChatGPT review were NOT RUN
here; all prior Partial gates and D1–D5 remain unchanged.
