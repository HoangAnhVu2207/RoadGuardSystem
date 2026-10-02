# ANH-01 Additive HTTP Adoption

Status: ACTIVE_LOCAL_DEVELOPMENT, PARTIAL. Adoption authority: assigned
[ANH-01 revision 2](../../planning/development/ANH-01.md), D1-D4, supplied by
the owner on 2026-10-02. This record does not activate the historical V2
OpenAPI, FE lock, deployment migration or external compatibility.

The sole wire specification is ANH-01 sections 3.2, 5.1, 6 and 7. Implemented
controllers/DTOs are the serialization source. New routes remain `/api/v1`;
legacy routes remain available. Commands require Idempotency-Key, and the
specified mutations require If-Match. Error responses use ProblemDetails
with code and correlationId. All size fields use int64.

## Local Adoption Evidence

- GeometryWorkflowController: metric draft/confirm, segment publication,
  immutable historic references and `anh01.geometry.v1` package.
  Segment-set/package reads expose `metadataStatus` and `missingMetadata`;
  legacy definition/hash and segment offsets/stations/length/metricGeometry
  remain nullable. Segment IDs/sequence and any existing values are retained.
  Commands requiring complete geometry return `geometry_metadata_incomplete`.
  A no-op draft set edit preserves child IDs and ETag; a changed definition
  changes the parent ETag, so stale publish fails with 412.
- SurveyV2Controller: plan/task/detail reads, work package, pairing and
  supplement child. `BAND_V1` is the new-write format; incompatible legacy
  mutations fail closed with `survey_scope_incompatible`.
  Management reads retain ended assignments. Operator reads/accept/submit
  require current membership and an active assignment; the latest declined
  Operator may replay decline until reassignment or membership revocation.
  New plan/task admission requires current route and published set; assigned
  historical task and supplement scopes keep their original references.
- SurveyAssessmentsController: immutable `pm-evidence-review.v1` assessment
  and eligible segment/band baseline selections with compare-current guards.
- UploadsController: purpose/MIME limits, int64 metadata, replay mapping,
  bounded streaming and durable verification recovery.
- Executable evidence: Anh01GeometryWorkflowTests, Anh01SurveyFlowTests,
  UploadApiTests, UploadPersistenceSqlTests, Anh01FileMigrationTests and
  focused unit policy tests. Final counts and commands are in the delivery
  summary. Fake object storage is mock evidence, not live MinIO evidence.

## Remaining Adoption Gates

CRS transforms/GPX/WGS84 require the dependency decision and authoritative
transform fixtures in ANH-01 section 5.2. Metric-only previews explicitly
report `WGS84_TRANSFORM_NOT_CONFIGURED`. Crew access and Huy consumption
need a task-scoped consumer contract/integration. Device provisioning and
initial handover attachment have no newly approved producer contract.
SQL migrations are candidates tested on isolated databases only. External
AI/Android/client and deployed compatibility are not verified.

## Owner-assigned Anh/Huy producer adoption - 2026-10-02

ACTIVE_LOCAL_DEVELOPMENT/PARTIAL: owner continuation assigns Anh shared
integration for HUY-01 D2 and sections9.3-9.4. Producer v1 exact signatures,
SQL authority, privacy and stale mapping are frozen in HUY-01's shared
integration reservation and IAnhHuyProducerService. Huy transactions PENDING.

ReporterEvidenceController adds seven routes below /api/v1/reporter-evidence:
POST uploads (201), GET uploads/{id}, POST uploads/{id}/part-urls (200),
POST uploads/{id}/complete (202), GET files/{id}, GET files/{id}/content,
GET publications/{publicationId}/reports/{reportId}/evidence/{evidenceId}/content.
Create accepts only fileName/mediaType/sizeBytes/checksumSha256; JSON scope/
owner extras fail400. Principal owner, null project/target, REPORT_PHOTO,
JPEG/PNG <=20MiB. Idempotency-Key required for commands; If-Match required
for complete. Current SQL Reporter/owner checked before replay. Other private
owner or publication recipient/projection missing404; pending/failed or
snapshot-version drift content409 source_not_ready; complete stale412
concurrency_conflict. Metadata never exposes storage URIs/object keys.
Triage does not change private scope. Publication content uses exact immutable
recipient/evidence relation and current Reporter, not case-wide IDs.
Existing project/Operator upload and Postman identifiers are preserved.

No Huy Report/Case/Candidate/label route or event activated. Event adoption
and actual Huy consumer runtime remain PENDING. Domain-only external review
PASS is HISTORICAL, not additive SQL/HTTP integration acceptance.


## Reporter intake activation — fixed Huy checkpoint 0e41913, 2026-10-03

Owner-assigned additive adoption on anh-review; external ChatGPT review PENDING.
Only POST `/api/v1/reports` is activated through production Reporter module DI.
Location identifies the Report; GET/list/supplement/case/candidate/label routes
are not activated. No report.received.v1/outbox emission.
Request: `{description,evidence:[{fileId,fileVersion,locationSource,capturedAt?,location?:{latitude,longitude,accuracyMeters?}}]}`.
Description trims to 1..1000 characters; nonempty evidence, unique file IDs;
fileVersion trims to 1..200. Unknown JSON fields/null evidence:400.
locationSource exactly UNKNOWN/CAPTURE/EXIF/MANUAL; UNKNOWN requires absent
location, others require both latitude/longitude (explicit zero valid), ranges
[-90,90]/[-180,180], nonnegative accuracy. Capture facts come from the request,
never inferred from upload GPS.

Current Reporter/active user/no mandatory password change; owned private
REPORT_PHOTO file, null project/target, VERIFIED upload and terminal upload
rowversion. Repeat checks under actor then FileId-sorted file/scope/upload SQL
locks before replay or changed-payload conflict, same scoped DbContext and
transaction as shared receipt. Other owner/missing/nonprivate:404 not_found;
pending/failed:409 source_not_ready; version drift:412 concurrency_conflict;
forbidden:403 access_forbidden. Global role-row deactivation after preflight is
not checked by imported Huy adapter: known Huy-owned gap, authority gate open.

Idempotency-Key absent:428 precondition_required. Reject control/non-ASCII;
trim outer ASCII spaces only, preserve internal printable ASCII, normalized
length1..200. Invalid request/key:400 validation_error with errors dictionary.
Fingerprint includes normalized description, ordered evidence/capture facts.
Authorization/evidence precede protected replay/conflict. Denied outcomes have
no Location/ETag. Same key/request:201 exact body/Location `/api/v1/reports/{id}`/
quoted report rowversion ETag. Changed payload:409 idempotency_key_reused.
Response `{id,description,createdAt,version,evidenceIds}`. Problem responses:
application/problem+json with status/title/detail/instance/code/correlationId.
Atomic immutable Report + UNASSIGNED Case + active link + sanitized audit +
receipt; no project assignment/approved label/publication authority invented.

Private upload/content retain producer contract: owned JPEG/PNG<=20MiB;
metadata ETag supplies fileVersion; content only VERIFIED; other-owner404,
not-ready409/storage503. HTTP/SQL tests use real BE and mock object storage;
live storage/deployment and external review remain separate gates.


## ANH-02 intake consumer continuation — 2026-10-03

Checkpoint A 715ade2c20f652b77c8c7e995c76bb5d47ead966 activates only received
Reports/UNASSIGNED Cases, not Huy triage/approval/lifecycle commands. Project
summary/drilldown now reads Report via canonical active CaseReportLink and
current Case.ProjectId, period ReceivedAt in [from,to); distinct case stock
uses current actual state and ignores period. Both return PARTIAL with
HUY_CASE_LIFECYCLE_NOT_INTEGRATED, including supported zero. Unassigned Reports
are excluded; absent relational link is never inferred from aggregate JSON.
Any geometry filter yields null/UNAVAILABLE REPORTER_SPATIAL_SCOPE_UNAVAILABLE
for these two metrics; no segment attribution guessed. Redacted IDs/rowversion
refs only, no Report description/contact/private bytes. Dossier uses the same
admission snapshot, preserves these partial sections/source revisions and keeps
reporterEvidence UNAVAILABLE; current project membership does not grant private
photo download. Defect/repair/dossier/timeline readers remain unavailable.

Retention named REPORTER_INTAKE contributor reads immutable original/supplement
source refs plus all case links (including closed links) and case-head versions.
It is complete only for that bounded table slice, not a replacement for HUY
full inventory. Aggregate inventory still requires Huy candidate/publication/
label/repair obligations, otherwise incomplete/WAITING. Private file projection
stays hidden to PM; Supervisor FILE/PROJECT holds and evaluation preserve every
known project obligation. No deletion/backfill/source policy changes.

Internal approved-label boundary remains Services/Interfaces/Integration/
Anh02Contracts.cs, additive Anh contract; Huy exact producer adoption PENDING.
CaptureApprovedAsync(actor,role,project,TrainingLabelFilterV1,ct) returns null
(unavailable,503 producer_unavailable), empty approved list (422 no_eligible_labels)
or immutable ApprovedLabelSnapshotV1(schemaVersion,snapshotId,hash,capturedAt,labels).
Capture must share exporter scoped SERIALIZABLE/SNAPSHOT transaction or use an
accepted durable producer snapshot. Labels carry label/revision/approval IDs,
project/type/bbox, file version/hash/int64 bytes/media, source identity/version,
approver/time, job/model/dataset/mode/segment provenance. One current revision
per LabelId; null annotation, duplicate heads, foreign project/malformed facts
fail422 producer_invalid. Producer declares approval/currentness; Anh does not
implement approval policy. Historical export retains admitted revision; original
bytes and download recheck ITrainingSourceAccessReader and current user/role/
password/project authority. Reader fixtures in tests are MOCK_VERIFIED and never
registered by production DI. No CaseDefectRead/internal dossier reader is inferred
from newer Huy controller DTOs; an exact adopted permission-aware Huy producer
checkpoint is still required, optional sections unavailable until then.


### ANH-02 independent runtime follow-up — 2026-10-03

Existing routes, DTOs, schema and approval ownership retained. The training
consumer rejects a producer label with absent/out-of-selection SegmentId when
segmentIds is nonempty (422 producer_invalid), rather than exporting beyond the
requested scope. Current-approved/current-head selection remains Huy's reader
responsibility; no fake adapter or second contract is activated. Defect filtering
remains unsupported/rejected until an exact mapping is handed off.

Dossier PDF reads only its stored admission DTO, displays metric code/name/unit,
status/band/route/set, numerator/denominator, period applicability, selected scope,
per-item versions and validation details, timeline availability and missing
sections. These are presentation additions, not changed KPI formulas or new
facts. Missing/unreadable/unmatched configured font fails export_font_unavailable;
process restart is required when changing PDFsharp's cached font. Deployment
supplies a licensed Unicode font; tests require ANH02_TEST_FONT_PATH explicitly.
Storage without required configuration fails file_storage_unavailable at its seam;
worker retains existing export_storage_unavailable recovery semantics.

Real Huy reader adoption/producer-consumer acceptance remains PENDING:
IApprovedTrainingLabelReader/ITrainingSourceAccessReader and
IMatchingCandidateSnapshotReader in Anh02Contracts.cs, plus a permission-aware
CaseDefectRead/dossier/timeline projection and complete named HUY retention
contributor using the caller's scoped transaction. No current implementation or
wire equivalence is inferred from a newer Huy tip. Reporter role-row correction
and external ChatGPT A/B review are separate pending Huy/external gates.
