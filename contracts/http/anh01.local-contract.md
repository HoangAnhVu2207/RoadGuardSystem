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
