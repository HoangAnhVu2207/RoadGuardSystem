# HUY-01 — Identity transport → Reporter/case → PM candidate/label

Ngày: 2026-10-02. Writer: Huy / Codex local. Nhánh: `huy-review`.

## Anh shared integration reservation — owner continuation 2026-10-02

TARGET_CONFIRMED: Anh is named writer for shared model/configuration,
DbContext, migrations/snapshot, DI, canonical adoption/Postman and Anh file/
geometry/source producers. This integration runs on `anh-review`; Huy keeps
Report/Case/Candidate module command service/repository/controller ownership.
Source handoff is fixed `8676226cf9d1b99adf83beb0391f147dfa4b112e`, with
domain-core external review at `ad52f4cae1d2f52f030ea23d59866004001c5aa6`.
Common base is `1ecae797caaed1ab912b02b2372a1940d1e05375`. Integration starts
after the independent ANH root-scope correction commit `5d6ecb5c6c4498c4803ddec735a00b153d0967b5`.
Selective `git restore --source=<handoff> --worktree` imports unchanged
Reports/Cases/Candidates domain directories, their two domain test files and
this spec only. No branch merge, auth transport/refresh or label import.
Domain PASS is historical evidence, not SQL/HTTP/adapter acceptance.

The concrete producer signatures are version 1 in
`Services/Interfaces/Integration/IAnhHuyProducerService.cs`; Anh produces,
Huy consumes. Implemented/adopted status and test evidence are recorded in
the existing ANH-01 summary. Private intake wire uses separate
`/api/v1/reporter-evidence/uploads` create/read/part-urls/complete and
`/reporter-evidence/files/{fileId}` metadata/content. Create accepts only
fileName/mediaType/sizeBytes/checksumSha256; principal owner and REPORT_PHOTO
purpose are server-resolved, project/target remain null, JPEG/PNG <=20 MiB.
Current active Reporter/owner is required before every new command/replay;
other owner and unauthorized publication projection return 404. File content
and evidence resolution return 409 source_not_ready until VERIFIED.
Complete uses If-Match (stale 412); command replay precedes fresh version
check after current authorization. Existing project upload contracts stay
project-scoped and do not acquire Reporter authority.

| Producer v1 | Authoritative source/scope/provenance | Exact failures and fixture |
|---|---|---|
| ResolvePrivateEvidenceAsync(actor,role,file,evidence,expectedFileVersion?,ct) | Files + immutable FileScopes + UploadSessions; active Reporter/owner, private project=null scope. Reference version is terminal upload rowversion; checksum/long bytes/MIME/uploadedAt are server facts. Capture metadata remains null, never inferred from upload GPS. | hidden ownership/missing 404; pending/failed 409 source_not_ready; expected file-version drift 412 concurrency_conflict. Owner/other/pending/failed/verified fixtures. |
| ResolvePublicationEvidenceAsync(actor,role,publication,report,evidence,ct) | Huy-owned immutable publication-recipient-evidence relation + recipient Report owner, actual original/supplement evidence, Files/UploadSessions. Anh checks exact projection/file readiness; Huy validates relation for every recipient when composing publication. | missing/other recipient/projection 404; pending/failed 409 source_not_ready. Two Reporters on one case with different selected recipient evidence; never a case-wide permission set. |
| ResolveGeometryAsync(actor,role,project,route,set,expectedVersion?,requireCurrent,ct) | Production geometry package, actual project/current membership, immutable route/set/segment refs and ordered adjacency. Package hash/version includes route current flag/set rowversion; no guessed CRS/offsets. | role/membership 403; scoped wrong project/ref 404; incomplete 409 source_not_ready; old route/set or expected-version drift 409 candidate_stale. Historical read permitted only with requireCurrent=false. |
| ResolveCandidateSourceAsync(actor,role,project,kind,id,expectedSource?,expectedGeometry?,expectedDisposition?,ct) | PM membership; REPORT via actual Report, active case link, assigned case project and server-persisted case geometry refs; report/case/head versions and verified files compose opaque source version. Active disposition comes from SQL head/decision, never client metadata. | untriaged/missing geometry/provenance and unavailable AI/FIELD producer 409 source_not_ready; wrong project 404; source/geometry drift 409 candidate_stale; expected active disposition drift 412 concurrency_conflict. Real SQL Report source + positive/negative/version fixtures. |

Huy must re-resolve facts and hold/check authoritative rows within its atomic
repository transaction. Preflight producer reads do not establish command
atomicity. Candidate AI/FIELD facts, ApprovedTrainingLabel reader/exporter,
event schema/consumer agreement and Huy business DI/HTTP remain PENDING until
their real implementations exist. No fake-success adapter, label mapping,
new event emission or Report/Case/Candidate command is activated by this section.

## Shared mapping and consumer handoff v1

Anh integration migration: `20261002120000_AnhHuySharedIntegration`, after ANH
file/geometry and forward scope corrections. Only fixture-owned SQL is applied.
No existing domain type is altered. EF8 generated owned relationship metadata
omits its Restrict DeleteBehavior; the shared designer/snapshot preserve it
explicitly. Preserve the two documented declarations when regenerating. Reports own original evidence; supplements
own separate evidence; neither rewrites source. Composite Report/owner FKs,
typed source/evidence FKs, correction identity FK, one active report-case link
(filtered index) and one source head (composite PK/FK) prevent ambiguous refs.
Append-only history/evidence/conclusions/publications/recipient rows reject
UPDATE/DELETE; Report source fields reject binary/datalength changes. Active
links can only be closed, not rewritten/reopened. Down rejects received data.
FileScopes keeps nullable project only for private REPORT_PHOTO/target-null,
with unchanged legacy project data; scope mutation/deletion is rejected.

Huy implementation order and exact persistence responsibilities:

1. Consume `Services/Interfaces/Integration/IAnhHuyProducerService.cs` v1
   (actual namespace `RoadGuardSystem.Services.Integration`). Evidence resolution
   gives VerifiedEvidenceReference + server checksum/bytes/version. Geometry
   gives real package and segment adjacency; CandidateFacts gives source,
   project, geometry and current disposition versions. Resolve every current
   authorization/fact before receipt replay, then re-resolve/lock/check them
   within the command transaction. Public 409/412 mappings are frozen above.
2. Build real Report/Case/Candidate module service/repository/controller seams
   on Huy-owned paths; add business DI only after real implementations exist.
   Shared read producers are already registered. No no-op command repository.
3. Use actual imported domain via DbContext Reports/ReportSupplements/
   IncidentCases/SourceDecisions, with support rows in
   `Repositories/Models/Huy01/HuyIntegrationRows.cs`. Populate CandidateDecision
   shadows SourceKind/SourceId/ReportSourceId or AIDetectionSourceId consistently
   with Source_Kind/Source_Id. Atomically insert decision/correction and update
   HuyCandidateSourceHead.DecisionId, checking head RowVersion/current disposition.
   FIELD schema/facts remain not-ready; AI facts remain not-ready without the
   required provenance producer. Classification must verify segment belongs
   to selected geometry route/project (independent catalog/route/segment FKs
   alone do not prove that relation).
4. Increment Report shadow `Revision` on each supplement/evidence append, and
   Case `Revision` on each child/link/state/geometry mutation (both cases for
   link/split). EF rowversion alone does not change when only children change.
   Save the parent bump with all child/history writes in one transaction.
   Populate Case geometry shadows GeometryRouteVersionId/GeometrySegmentSetId
   from the resolved geometry. Pair/composite FK enforces route-set pairing.
5. Atomically synchronize `_activeReportIds` snapshot with CaseReportLinks
   close/create. Persist CaseReportLinkHistory once by Id and its
   HuyLinkHistoryReport FK refs; query history FromCaseId OR ToCaseId. The shared
   object occurs in both domain collections, so EF LinkHistory is ignored.
   If a reloaded aggregate needs historical navigation for a command, Huy must
   add an explicit materialization hook/signature; this package does not mutate
   the reviewed domain to add one silently.
6. Persist conclusion CaseId and HuyConclusionDefect/HuyConclusionEvidence
   refs, publications plus HuyPublicationRecipient/HuyPublicationDefect and
   HuyPublicationEvidence per recipient. Domain JSON snapshots and relational
   refs require atomic sync; SQL FKs alone do not prove snapshot equality.
   `CasePublicationRecipientFacts.Create(reportId,visibleDefectIds,
   visibleEvidenceIds)` must be composed individually from that Reporter's real
   visibility. Never replace it with a common case evidence set. Recipient
   content producer checks exact projection + immutable evidence FileVersion.
7. Prove Report/supplement, triage/link/split, conclude/publication and candidate
   decision/correction atomic commit/rollback including audit/receipt/outbox
   using real Huy repositories. Then adopt the implemented Huy HTTP routes and
   agreed events and run producer-to-real-consumer HTTP/SQL. Until then business
   DI/HTTP/event/label reader-exporter remain PENDING; no new Huy event emitted.

Schema tests are fixture persistence/constraints/recovery evidence, not Huy
business orchestration PASS. Runtime counts and known limitations are in the
existing ANH-01-summary; external review of this integration is PENDING.

The checkpoint entries below are HISTORICAL at the fixed Huy handoff. Their
shared-pending statements are superseded only by the implemented Anh shared
reservation above; Huy business/consumer gates remain pending.

## Implementation checkpoint - 2026-10-02 11:09 +07:00

- `deliveryStatus`: `PARTIAL`; branch `huy-review`; base `1ecae797caaed1ab912b02b2372a1940d1e05375`; initial worktree status clean. The caller's original checkout on `huy` had the untracked source path `planning/development/HUY-01-spec-and-codex-prompt.md`; it was preserved and copied here rather than modified.
- Huy confirmed `D1=A`, `D2=A`, `D3=A`, `D4=A` on 2026-10-02. That approves this package's documented behavior, but does not reserve or integrate shared files.
- `CURRENT_VERIFIED`: `RoadGuardSystem.Services/Implementations/Authentication/AuthService.cs` creates replacement refresh credentials; `RoadGuardSystem.Repositories/Implementations/Identity/IdentityRepository.RefreshTokens.cs` enforces active parent session during rotation; `UserSessionConfiguration.cs` and migrations remain current mapped schema evidence.
- `TARGET_DOCUMENTED`: Sections 5.2 and 8.2 require refresh expiry never to exceed the parent session's absolute expiry, without sliding that absolute expiry. `PROPOSED_DELTA`: the new transport, Reporter/case, candidate and label schemas/routes still require the shared integration named in section 9.2.
- Slice contract: existing `POST /api/v1/auth/refresh`; authenticated refresh-token holder and current active user/session; existing `RefreshCommand`; existing success token response and existing auth error statuses; no new HTTP contract; replacement refresh expiry is `min(now + configured lifetime, session.ExpiresAt)`; rotation remains repository-atomic and does not add sensitive data/audit behavior.
- Source evidence: `AGENTS.md` "Endpoint Contract And Delivery" and "V2 Ownership And Shared Hotspots"; `planning/V2/README.md` "Ownership va tranh xung dot"; this document sections 5.2, 8.2, 9.2 and 10; `tests/RoadGuardSystem.UnitTests/Authentication/AuthServiceTests.cs` for current auth unit seam. The V2 canonical OpenAPI is `NOT_ENABLED` for HUY-01 routes.
- Shared reservations/dependencies remain `PENDING`: Anh is named writer for `RoadGuardDbContext`, migrations/model snapshot, shared DI/configuration, canonical HTTP/events contracts, and integrated Postman. ANH-01 must provide verified Reporter-private evidence upload/download; geometry and processing provenance producers need their agreed interfaces. No `DbContext`, migration, snapshot, shared DI, canonical OpenAPI or integrated Postman file has been changed.

### 2026-10-02 - IN_PROGRESS

- Scope/result: Began the independent D1 refresh-boundary slice. A focused unit regression now requires a rotated refresh token not to outlive its parent session; `AuthService` clamps the replacement expiry. Reporter/case, candidate/defect and training-label work remains pending the named shared contracts and persistence integration.
- Files: `RoadGuardSystem.Services/Implementations/Authentication/AuthService.cs`; `tests/RoadGuardSystem.UnitTests/Authentication/AuthServiceTests.cs`.
- Acceptance criteria: Unit behavior passed after a verified red run. This is not HTTP, SQL, migration, mapping, producer/consumer, or deployment evidence.
- Verification: red command `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --filter FullyQualifiedName~AuthServiceTests.Refresh_SessionExpiresBeforeConfiguredLifetime_ClampsReplacementExpiry --nologo -v q` executed 1, passed 0, failed 1. Green: `dotnet build RoadGuardSystem.Services/RoadGuardSystem.dServices.csproj --nologo -v q -clp:ErrorsOnly` passed; `dotnet build tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --nologo -v q -clp:ErrorsOnly` passed with 133 pre-existing analyzer warnings; `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-build --filter FullyQualifiedName~AuthServiceTests --nologo -v q` executed 18, passed 18, failed 0, skipped 0.
- Reused/invalidated evidence: no prior execution evidence reused. Any later service edit invalidates the focused authentication result. SQL Server, API smoke, migration, Postman and shared integration are `NOT_RUN`.

### 2026-10-02 - PARTIAL

- Scope/result: Added the independent `TrainingLabelRevision` domain invariant for normalized BBOX annotations and a single terminal PM review outcome. A revision begins `PENDING`; an `APPROVED` or `REJECTED` review retains actor, timestamp and reason; a second decision is rejected. This is not an HTTP endpoint, persistence model, approved-reader implementation, or export integration.
- Files: `RoadGuardSystem.BusinessObjects/Labels/TrainingLabelReviewStatus.cs`; `RoadGuardSystem.BusinessObjects/Labels/TrainingLabelRevision.cs`; `tests/RoadGuardSystem.UnitTests/Labels/Huy01TrainingLabelRevisionTests.cs`.
- Acceptance criteria: unit evidence covers the terminal review proof and invalid BBOX boundary. The remaining D3 acceptance criteria (PM project authorization, current revision reader, revision creation, SQL filtering, migration and exporter consumer) are blocked by shared persistence/DI/canonical contract coordination.
- Verification: red command `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --filter FullyQualifiedName~Huy01TrainingLabelRevisionTests.Create_BboxExceedsImageBounds_IsRejected --nologo -v q` executed 1, passed 0, failed 1 after BBOX validation was intentionally removed. Green: `dotnet build tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --nologo -v q -clp:ErrorsOnly` passed with 5 analyzer warnings; `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-build --filter FullyQualifiedName~Huy01TrainingLabelRevisionTests --nologo -v q` executed 2, passed 2, failed 0, skipped 0.
- Reused/invalidated evidence: the prior authentication focused result remains valid because its source path was unchanged. SQL Server, API smoke, migration, Postman, real producer/consumer and deployment evidence remain `NOT_RUN`.

### 2026-10-02 - PARTIAL

- Scope/result: Added the repository-level D1 backstop so refresh rotation clamps any supplied replacement expiry to the authoritative parent session expiry inside the SQL transaction.
- Files: `RoadGuardSystem.Repositories/Implementations/Identity/IdentityRepository.RefreshTokens.cs`; `tests/RoadGuardSystem.IntegrationTests/Identity/P110AuthenticationPersistenceTests.cs`.
- Acceptance criteria: SQL Server test proves the durable replacement row and returned fact do not outlive the parent session. Web/Android transport routes, cookie/CSRF, session schema changes, and full HTTP smoke remain outside this slice.
- Verification: red `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build --filter FullyQualifiedName~P110AuthenticationPersistenceTests.RefreshRotation_ReplacementOutlivesSession_ClampsPersistedExpiry --nologo -v q` executed 1, passed 0, failed 1. Green build `dotnet build tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --nologo -v q -clp:ErrorsOnly` passed with 47 analyzer warnings; green SQL test executed 1, passed 1, failed 0, skipped 0 using the disposable `IdentitySqlServerFixture`.
- Reused/invalidated evidence: prior authentication and label tests remain valid; HTTP/Postman/migration/producer-consumer/deployment gates remain `NOT_RUN`.

### 2026-10-02 - PARTIAL

- Scope/result: External review P2 fixed in `TrainingLabelRevision.Review`: every fallible review input is normalized before terminal state/proof mutation, so a rejected reason leaves the revision pending for a later valid review.
- Files: `RoadGuardSystem.BusinessObjects/Labels/TrainingLabelRevision.cs`; `tests/RoadGuardSystem.UnitTests/Labels/Huy01TrainingLabelRevisionTests.cs`.
- Verification: red focused test executed 2, passed 0, failed 2; fresh unit build passed with 5 analyzer warnings; green focused label tests executed 4, passed 4, failed 0, skipped 0. HTTP/SQL/schema gates are unchanged and `NOT_RUN` for this entity-local fix.

### 2026-10-02 - PARTIAL

- Scope/result: Implemented the approved independent Report/IncidentCase/Candidate domain core. Reports preserve immutable owner/original evidence and append-only supplements; cases implement `UNASSIGNED`, `OPEN`, `AWAITING_EVIDENCE`, `CONCLUDED`, `LINKED`, triage, supplement reopen, project-safe link/split, conclusion and immutable publication selection; candidates implement immutable `KEEP_NEW`, `LINK_EXISTING`, `REJECT` decisions plus matching active-disposition correction prerequisites. There is no inspection task, duplicate detector, auto-routing, auto-merge, fake producer, endpoint, or persistence activation.
- Files: `RoadGuardSystem.BusinessObjects/{Reports,Cases,Candidates}/`; `tests/RoadGuardSystem.UnitTests/{Reports,Candidates}/Huy01*DomainTests.cs`; this spec's §§5.3 and 9.4.
- Source evidence: `planning/development/HUY-01.md` §§6–8 and §9, `AGENTS.md`, current `BusinessObjects/Defects/Defect.cs` and `BusinessObjects/Surveys/SurveyRequest.cs`. The initial red command stopped at missing Report/Case/Candidate namespaces as expected. The later regression red run executed 9 reporter/case tests: passed 7, failed 2 for duplicated evidence and no-defect prerequisites; both were fixed before mutation occurs.
- Verification: `dotnet build RoadGuardSystem.BusinessObjects/RoadGuardSystem.aBusinessObjects.csproj --nologo -v q -clp:ErrorsOnly` passed with 5 pre-existing analyzer warnings and 0 errors; `dotnet build tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --nologo -v q -clp:ErrorsOnly` passed with 0 warnings/errors; focused Reporter/Case/Candidate test filter executed 13, passed 13, failed 0, skipped 0. Unit tests prove only entity-local invariants, not current authorization, true file verification, SQL uniqueness/rowversion, atomic durability, HTTP, Postman, or external consumers.
- Self-review: pass 1 found/fixed direct mutable collection exposure and discarded triage provenance; pass 2 confirmed no `DbContext`, mapping, migration, shared DI/config, canonical contract, integrated Postman, API or HUY-02 file changed. `deliveryStatus` remains `PARTIAL`; shared integration requirements are recorded in §9.4.

### 2026-10-02 - PARTIAL

- Scope/result: Addressed external ChatGPT review P2-1/P2-2 from domain-core commit `8ad937aea0c893fdc5e9dc7ae60dcfa742a944d3`. `SplitReports` now rejects its source ID before link/history mutation. Publication prerequisites now use server-resolved `CasePublicationRecipientFacts` per report; every selected defect/evidence ID must be present in every selected recipient's authoritative facts before a new immutable snapshot is appended.
- Files: `RoadGuardSystem.BusinessObjects/Cases/{IncidentCase,CasePublicationPrerequisites,CasePublicationRecipientFacts}.cs`; `tests/RoadGuardSystem.UnitTests/Reports/Huy01ReporterCaseDomainTests.cs`; this spec §§5.3 and 9.4.
- Verification: P2-1 red focused test executed 1, passed 0, failed 1; P2-2 red compile stopped at the intentionally absent recipient-scoped type. Fresh focused Reporter/Case tests executed 12, passed 12, failed 0, skipped 0. Fresh BusinessObjects and UnitTests builds passed with 0 warnings/errors; full `Package=HUY-01` trait executed 20, passed 20, failed 0, skipped 0. The final documentation checkpoint `git diff --check` passed with no whitespace errors.
- Self-review: privacy pass confirmed a case-wide verified/permitted set cannot establish the report-recipient relation and the replacement fails missing recipient/relation before `_publications.Add`. Partial-mutation pass confirmed self-ID split rejects before source mutation/history creation and failed publication leaves `Publications` unchanged. External ChatGPT review of this domain-core slice is `PASS`: P2-1 and P2-2 are `CLOSED`, with no new P0/P1/P2. `deliveryStatus` remains `PARTIAL` and SQL/HTTP/shared-integration gates remain pending.

### 2026-10-02 - PARTIAL integration-preparation checkpoint

- Scope/result: Documentation-only readiness checkpoint from base `ad52f4cae1d2f52f030ea23d59866004001c5aa6`. Revalidated §9.4 handoff: Anh is the named writer for `RoadGuardDbContext`, migrations/snapshot, shared DI/configuration, canonical HTTP/event contracts and integrated Postman; Huy's domain inputs remain inactive until producer facts, schema and composition are integrated under that reservation. No domain, label, API, persistence, schema, DI, contract, Postman, or HUY-02 file changed.
- External review: ChatGPT `PASS` for the domain-core slice at `ad52f4c`; self-ID split and recipient-scoped publication authorization are closed. HUY-01 is not `DONE`: the next blocker is Anh's shared integration, including SQL/HTTP producer-consumer fixtures, not missing domain implementation.
- Evidence reuse: no code changed, so builds and full `Package=HUY-01` test evidence (20 executed, 20 passed, 0 failed, 0 skipped) is reused from `ad52f4c`; no new compilation/test claim is made for this documentation-only commit. Final `git diff --check` passed with no whitespace errors.
- Source prompt: `planning/development/HUY-01-spec-and-codex-prompt.md` remains absent from this checkout. It was not recreated from this spec or memory; recovery requires the original external source.

## 1. Assignment, baseline và giới hạn

- **ASSIGNED có checkpoint** theo yêu cầu Huy trong cuộc trao đổi này: triển khai trọn HUY-01, tận dụng source; phần đủ căn cứ ở §3 làm ngay. Các lựa chọn nghiệp vụ/public compatibility ở §11 vẫn **PROPOSED**, không được coi im lặng là chấp thuận.
- GitHub đã kiểm tra `compare(base=1ecae797caaed1ab912b02b2372a1940d1e05375, head=huy-review)`: `identical`, ahead=0, behind=0. Remote `huy-review` đã có đúng guidance. Không cần cherry-pick hay cập nhật từ nhánh Anh.
- Revision dùng để đọc source: `1ecae797caaed1ab912b02b2372a1940d1e05375`. Baseline RF-11 tham chiếu: `21223aa1d18f510d012f9f25081b8c4bcf87b7a8`. Local HEAD/dirty paths của Huy: **UNKNOWN**, Codex ghi lúc bắt đầu.
- ChatGPT viết spec và review diff; Codex local triển khai, hai lượt self-review/fix, test, commit/push. Không amend/force-push commit đang review. Không merge develop, chạm main hoặc sửa nhánh Anh.
- Một spec này và một PR summary là đủ. Không RF audit/remediation mới, ZIP, audit package hay coordination document riêng. Manifest `skills: []`; không dùng RoadGuard skills retired, kể cả khi tài liệu V2 cũ yêu cầu.
- Không triển khai HUY-02. Không provider AI thật, AI processing retry/late-attempt remediation, compatibility callback A08-01 hoặc remediation A09. Idempotency/retry của command nghiệp vụ vẫn bắt buộc. HUY-01 bàn giao interface cho sync/inspection/notification, chưa xây các flow đó.
- Mục tiêu 1–2 ngày là timebox điều phối, không là bằng chứng hoàn thành. Điểm kiểm soát theo luồng chạy được và SQL evidence; không bỏ chức năng vì hết giờ.

## 2. Nguồn và kết luận reuse

Đã đọc `AGENTS.md`, `.agents/manifest.json`, `.agents/modules/README.md`, bốn rules evidence/delivery/safety/review-and-coordination, `planning/development/README.md`, `spec-template.md`; product confirmed-decisions/workflows/data-and-quality, các đoạn FR-11..14/36, BR-29/30/47/48 trong historical-fr-br; backend README/persistence-and-operations; contracts README/events; decision register và D-AUTH-proposed. Tài liệu historical được dùng tìm yêu cầu, không tự nâng thành quyết định mới.

| Bằng chứng tại revision trên | Kết luận và cách dùng |
|---|---|
| **TARGET_CONFIRMED** PR-33A | Candidate cùng project; ưu tiên segment giao/lân cận; dùng vị trí/sai số/lịch sử; thiếu GPS dùng scope+ảnh; PM được mở rộng trong project; không auto-merge. Không có ngưỡng khoảng cách/recall đã duyệt. |
| **TARGET_CONFIRMED** PR-34A | PM đúng project duyệt/từ chối nhãn; chỉ approved xuất training; AI không tự duyệt. Không hỏi lại quyền duyệt nhãn. |
| **TARGET_CONFIRMED** PR-36A/37 | Web cookie + server session, idle 30 phút/max 12 giờ; Android access 15 phút, refresh rotation max 30 ngày từ login; OTP 10 phút/5 lần, resend ≥60 giây, ≤3 lần/15 phút. Compatibility với bearer cũ còn mở. |
| **CURRENT_VERIFIED — source inspection** `API/Controllers/AuthController.cs`, `ReporterRegistrationsController.cs`, `MeController.cs`, `ProfileController.cs`; `Services/Implementations/Authentication/{AuthService,IdentityOnboardingService,AuthoritativeSessionValidator}.cs` | Có login/refresh/logout/password flows, Reporter OTP, session và authoritative validation. Giữ và mở rộng; không xây lại identity. Đây không phải kết quả test mới. |
| `API/Extensions/ServiceCollectionExtensions.cs`, `Authentication/JwtBearerConfiguration.cs` | Default bearer; có AI scheme riêng, chưa có cookie flow. Không đổi default toàn API khiến route cũ đột nhiên nhận cookie. Bearer có ngoại lệ replay hẹp cho logout/change-password; không mở rộng ngoại lệ này sang command nghiệp vụ. |
| `AuthService.RefreshAsync`, `Repositories/Implementations/Identity/IdentityRepository.RefreshTokens.cs` | Replacement expiry đang tính `now + RefreshTokenLifetimeDays`; parent session vẫn có expiry riêng. Chưa thể coi đã đạt Android max 30 ngày từ login. `UserSession` chưa có transport/last-activity. Phải xử lý cả issuance từ onboarding/invitation, không chỉ Login. |
| `IdentityOnboardingOptions.cs` | Default OTP khớp PR-37; cần kiểm effective config và HTTP/SQL, không đổi số đã chốt. |
| `BusinessObjects/Defects/{Defect,DefectVerificationLog}.cs`, `Processing/AIDetection.cs`, mappings và tests P232 | Có defect/detection/log, immutable AI detection, unique `Defect.SourceAIDetectionId`. Dùng lại catalog, audit, reference và invariants. Chưa có production Report/IncidentCase/TrainingLabel aggregate/controller trong tree đã đọc. |
| `DetectionReviewPersistenceService.PersistAsync/PersistAggregateAsync` | Primitive hiện tạo **mới** detection + defect + field inspection task + log/audit/outbox cùng transaction. Không gọi nguyên trạng để review detection đã có, link defect cũ hoặc quyết định không cần task. Không biến việc giữ candidate thành tự giao inspection. Giữ contract cũ cho caller/test cũ; thêm seam mới đúng HUY-01. |
| `ProcessingV2PersistenceService` result ingestion | Detection được lưu với job/model, nhưng geometry/road-version có thể null. PM consumer phải nhận trạng thái thiếu dữ liệu; không bịa geometry từ RawPayload, không sửa callback bị loại khỏi đợt. |
| `Services/Implementations/Files/UploadService.cs` | Có purpose REPORT_PHOTO nhưng `IsSupportedRole` loại Reporter, Create yêu cầu project. Đây là dependency ANH-01 thật cho report chưa rõ project. Không lách bằng cấp Reporter role staff hoặc nhận tùy ý fileId. |
| `ProjectScopeGuard.AuthorizeAsync` | Có live membership/effective date; Supervisor bypass project membership. Vì PR-34A là PM, label command phải kiểm role PM **trước** guard; không suy Supervisor/Admin được duyệt nhãn. |
| `IdempotencyOperationService` + `IdempotencyRecordConfiguration` | Reuse SQL receipt, scope unique `(actor, project, operation, key)`, replay recovery sau commit acknowledgement failure. Không dùng in-memory receipt. Handler retry không được chứa gửi email/storage/provider. |
| API auth/identity tests, `Rf1006ReporterDefectCharacterizationTests`, P232 SQL tests | Có nền regression, nhưng test RF1006 hiện kiểm processing-job read/scope, không chứng minh Reporter/case đã có. Test file tồn tại không là PASS. |

External Web/Android client implementation và consumer sign-off: **UNKNOWN**. FE authentication guide trong `docs/diagram/V2/09_Frontend/` là tài liệu lịch sử, không chứng minh client triển khai. Mọi test/build trong phiên viết spec này: **NOT RUN**.

## 3. Phần giao làm ngay và checkpoint

Giao **một gói HUY-01**, không chia ticket theo từng API:

1. Reuse/hoàn thiện identity core: policy thời gian theo PR-37, authoritative user/session checks, rotation/revocation concurrency, OTP boundaries; thêm unit và regression trên luồng hiện có. Giữ wire/behavior compatibility hiện tại cho đến D1; policy mới có thể chạy qua service/test mà chưa đổi live route/config. Không sửa shared DI/migration.
2. Candidate matching core theo PR-33A: internal service/query trên nguồn project/version hợp lệ, trả lý do xếp hạng, thiếu GPS không loại mất nguồn, không auto-merge. Làm các test scope, deterministic order, stale version, thiếu provenance. Geometry/file producer chưa có thì báo dependency cụ thể; mock chỉ chứng minh consumer contract.
3. Label authorization/domain core theo PR-34A: PM project-scoped, decision có actor/revision/source, approved-only projection, không AI auto-approve. Chưa bật HTTP hoặc mapping chưa được Anh tích hợp; lifecycle mới ở D3 phải được chốt trước phần phụ thuộc.
4. Chuẩn bị source/entity/config/tests của Huy theo §9 sau checkpoint tương ứng; các thay đổi schema là additive đề xuất cho Anh, không tự generate migration/snapshot. Không dùng `EnsureCreated`/test-only mapping để tuyên bố production persistence xong.
5. Gửi ngay trong spec/PR delta file chung và dependency upload Reporter tới Anh. Khi D1–D4 được trả lời và interface/writer được thống nhất, tiếp tục hoàn tất nguyên luồng §4–§8, không yêu cầu approval lại từng endpoint.

Nếu chưa có câu trả lời, vẫn commit phần độc lập có giá trị; PR ghi `PARTIAL — decision/integration pending`, không nhận HUY-01 DONE. Không cài branch chưa được duyệt rồi che bằng response thành công giả. Không hỏi lại quyết định 32–44.

## 4. HTTP conventions cho phần mới — chờ D4 adoption

Các bảng dưới là thiết kế **PROPOSED** cụ thể của spec này, không phải mô tả API đã tồn tại, cũng không tự kích hoạt toàn bộ OpenAPI V2 lịch sử. D4 chốt adoption cho đúng HUY-01; Anh là writer canonical contract.

- Giữ `/api/v1` và camelCase như current controllers. UUID không rỗng; UTC ISO-8601; enum wire là chuỗi explicit, không serialize số enum persistence. Không đổi serializer toàn ứng dụng. Không trả EF entity, storage key/URI, raw AI payload hoặc secret.
- New list: `pageSize` mặc định 20, khoảng 1..100; opaque `cursor`; response `{items,nextCursor}`. Order ổn định `(createdAt,id)`; matching dùng thứ tự riêng §7. Cursor gắn filter/project/actor và version nguồn khi cần; cursor sai trả 400. Đây là limit phân trang kỹ thuật, không giới hạn số report/bằng chứng hợp lệ.
- New resource GET/mutation trả strong `ETag: "<base64-rowversion>"`; body `version` là cùng opaque value không quotes. Mutation tài nguyên đã có cần `If-Match`; không chấp nhận wildcard/weak tag. Missing 428, malformed 400, stale 412. Create không cần If-Match.
- Command mới cần `Idempotency-Key` 1..200 ký tự printable ASCII, trim; new UUID command identity không thay business dedup. Same key/request trả cùng status/body/Location/ETag đã commit; key khác payload 409. Chi tiết ordering ở §8.
- HTTP errors `application/problem+json`: `{type,title,status,detail,instance,code,correlationId}`; validation có `errors` theo field. Giữ lỗi lowercase current API, không đổi sang catalog uppercase draft.

| Status | Code/điều kiện cho API mới |
|---|---|
| 400 | `validation_error`: JSON/UUID/enum/header không hợp lệ, mutually exclusive fields, unknown fields trên DTO mới. |
| 401 | `auth_unauthorized`, `auth_session_revoked`, `refresh_token_invalid`, `refresh_token_expired` tùy auth; cookie không redirect HTML. |
| 403 | `access_forbidden`: sai role hoặc live project membership; `csrf_failed` cho cookie write sai antiforgery; `auth_password_change_required` cho tài khoản buộc đổi mật khẩu. |
| 404 | `not_found`: không có resource hoặc Reporter truy cập report/file không thuộc mình. PM sai project route trả 403 trước đọc resource; ID nằm ngoài project đã được authorize trả 404. Không lộ object metadata. |
| 409 | `idempotency_key_reused`; `invalid_state_transition`; `source_not_ready`; `reference_scope_conflict`; `candidate_stale` theo nguyên nhân. |
| 412 / 428 | `concurrency_conflict` / `precondition_required`. |
| 413 / 415 | Payload vượt request-body limit hiện hành / `unsupported_media_type`; không biến multipart bytes thành report JSON. |
| 503 | `dependency_unavailable` khi dependency cần cho request không khả dụng; không tạo ACK/receipt success. |

Các code mới để trong `Huy01ErrorCodes.cs` do Huy sở hữu; thay central constants phải qua Anh. Error payload không chứa token, email người khác, raw request, SQL exception. Header/shape v1 cũ giữ nguyên; bảng này không normalize API cũ.

## 5. Identity transport — D1

### 5.1 Compatibility cụ thể được đề nghị

Giữ nguyên bearer `/auth/login`, `/auth/refresh`, `/auth/logout`, password routes, `/profile`, `/me`, invitation và Reporter registration hiện có. Không đặt ngày xóa route/client khi chưa có owner. Không auto-relock FE snapshot. Bổ sung transport rõ ràng, không suy từ User-Agent:

| Endpoint | Request | Success / effects |
|---|---|---|
| `GET /api/v1/auth/web/csrf` | Anonymous hoặc web session | 200 `{requestToken,headerName:"X-CSRF-TOKEN"}` + antiforgery cookie; `Cache-Control: no-store`; không tạo authenticated session. |
| `POST /api/v1/auth/web/login` | `{email,password}` + antiforgery pair | 200 `{user:{id,displayName,role,version},mustChangePassword,issuedAt,absoluteExpiresAt,idleExpiresAt}`; Set-Cookie session mới; không access/refresh token trong body. Wrong credential 401 generic. |
| `GET /api/v1/auth/web/session` | Cookie | 200 cùng projection; cập nhật last activity theo quy tắc dưới. |
| `POST /api/v1/auth/web/logout` | Cookie + CSRF + Idempotency-Key, không body | 204; atomic revoke SQL, xóa cookie đúng attributes. |
| `POST /api/v1/auth/android/login` | `{email,password}` | 200 reuse `AuthTokenResponseDto` chính xác: accessToken, refreshToken, tokenType, expiresIn, mustChangePassword, user. |
| `POST /api/v1/auth/android/refresh` | `{refreshToken}` | 200 cùng token DTO; rotation atomic, hết absolute session 401. Không thêm idempotency receipt chứa plaintext credential. |

Android sử dụng logout/change-password bearer hiện có. Web được dùng các identity endpoints hiện có cần thiết (`/me`, `/profile`, change-password) sau khi thêm cookie acceptance + CSRF **có chủ đích** và regression bearer; các staff/onboarding endpoint chưa tích hợp cookie được ghi rõ chưa tương thích, không đổi default ngầm.

Reporter verify/invitation accept đang trả token: giữ response legacy. New Web client có thể hoàn tất onboarding hiện tại rồi đăng nhập Web; không đưa token đó vào cookie và không coi đó là web session. Nếu muốn one-step cookie onboarding, đó là delta compatibility riêng ở D1, không tự thêm. Android mới đăng nhập Android sau onboarding để nhận đúng transport policy.

### 5.2 Session/security semantics

- Reuse UserSession và credential verifier. Đề nghị thêm `Transport` (`LEGACY_BEARER`, `WEB`, `ANDROID`) và nullable `LastActivityAt`; session absolute boundary dùng `IssuedAt`/`ExpiresAt` sẵn có. Existing rows backfill `LEGACY_BEARER`, không đoán platform, không kéo dài expiry. Legacy issuance giữ semantics cũ trong cửa sổ compatibility; target Android issuance explicit dùng 30 ngày từ login.
- Web: `ExpiresAt=IssuedAt+12h`; hết hạn khi `now >= ExpiresAt` hoặc `now >= LastActivityAt+30m`. Touch last activity chỉ sau xác thực, CSRF và authorization thành công, kể cả GET bảo vệ; không touch cho anonymous/failed request, OPTIONS hoặc rejected replay. Update nguyên tử với điều kiện vẫn active và chưa timeout; request cũ không hồi sinh session đã logout/timeout. Cookie expiry/renewal không vượt absolute SQL expiry.
- Cookie `__Host-RoadGuardSession`, HttpOnly, Secure, Path=/, không Domain; đề nghị SameSite=Lax cho topology same-site. Cookie chứa protected ticket/reference `sid`, server kiểm user/session hiện tại mỗi request; không dùng ticket claims làm authority. Antiforgery token rotation sau login/logout, token gắn session/identity. Không log token.
- D1 phải chọn topology: same-site dùng cấu hình trên; nếu Web/API cross-site thì SameSite=None; Secure, CORS allowlist chính xác + credentials và antiforgery bắt buộc. Không wildcard origin; không tự điền production domain. Thiếu config giữ web transport chưa activate, không hạ Secure cho production.
- New HUY business routes nhận explicit cookie hoặc bearer; nếu có Authorization header thì chỉ dùng bearer, invalid bearer không fallback cookie. Khi credentials cookie+bearer chỉ tới hai actor khác nhau, reject 400 `validation_error`; không ghép claims. AI scheme không được vào các routes này.
- CSRF trên mọi unsafe request dùng cookie, kể cả login/logout và identity write đã opt-in. SameSite không thay CSRF; không bật cookie toàn v1 mà chỉ bảo vệ endpoints mới. Bearer routes không buộc CSRF khi không dùng cookie.
- Android access 15 phút; refresh expiry không vượt `session.IssuedAt+30d` và `session.ExpiresAt`. Clamp ở cả service và repository để caller khác không kéo dài. Absolute expiry không trượt khi rotation. Tại đúng boundary không phát token mới. Session/token không được hồi sinh khi logout/role change/password reset đua với refresh.
- Reuse replay-revokes-family semantics hiện có; hai refresh đồng thời có tối đa một rotation winner nhưng replay có thể revoke cả family (test hiện có chứng minh ý định). Không hứa retry refresh trả lại plaintext token cũ. Client single-flight; reconnect không xóa offline queue.
- User inactive, revoked session, changed role, mustChangePassword kiểm bằng current SQL, không cached claim. Must-change-password chỉ vào đúng recovery/profile-minimal/logout/password endpoints được cho phép; không tạo report/duyệt label.
- OTP effective config đúng PR-37; test đủ cả attempt 5/6, resend 59/60 giây, lần 3/4 trong 15 phút, OTP cũ sau resend, concurrent verify. Reuse sender test double, không gửi email thật; ghi email delivery `mock verified`.
- Cookie/transport state, revoke audit và receipt logout thuộc transaction SQL. Session auth errors không chứa credential. Bảo toàn replay logout hẹp sau revoke khi chứng minh đúng actor/sid/key đã commit; không mở quyền business chỉ để trả replay.

### 5.3 Internal domain contract — approved independent core (2026-10-02)

This contract is `TARGET_CONFIRMED` for the pure domain package below. It is deliberately not an HTTP, schema, DI, or cross-owner producer contract.

- **Source identity and scope facts:** `CandidateSourceIdentity={kind,id,sourceVersion}` is immutable and `kind` is `REPORT|AI_DETECTION|FIELD_OBSERVATION`. A command additionally receives server-resolved `projectId`, `geometryVersion`, and, for a correction, the active disposition `{decisionId,version}`. The service/repository must re-resolve these facts under its concurrency boundary; client hints, a client-supplied project, provenance, owner, or `verified` flag are never authoritative evidence.
- **Evidence facts:** a `VerifiedEvidenceReference` is a Huy-owned domain input with immutable `fileId`, `fileVersion`, `ownerUserId`, and verification state; the created `ReportEvidence` records its immutable report/supplement relationship. The domain accepts only `VERIFIED`; the future file adapter, not the client, resolves ownership, verification, source relation, and provenance. Capture metadata is descriptive and does not make upload GPS capture GPS.
- **Project and version facts:** `IncidentCase` stores only its already-authorized assigned project. Triage must supply a non-empty project; link/split require equal non-empty project facts. Expected report/case/source/target/disposition versions are repository preconditions, not an in-memory substitute for rowversion or atomic multi-aggregate writes.
- **Report/case commands:** Report owner and original evidence are immutable. Supplements append immutable descriptions/evidence and do not create a case. A case starts `UNASSIGNED`; triage makes it `OPEN`; awaiting-evidence and supplement/reopen transitions preserve sources and historic publications. Split must reject a new case ID equal to its source ID before it changes links/history. Conclusion needs server-resolved verified-defect/evidence facts where the outcome requires them. Publication creates an immutable selected-recipient snapshot; every selected defect and evidence item must be authorized and related for every selected recipient report, not merely present in a case-wide set. It never changes a prior publication or a defect state.
- **Candidate decisions:** `KEEP_NEW` requires classification and forbids a target; `LINK_EXISTING` requires target id/version and forbids classification; `REJECT` forbids both. Every decision has a reason. History is append-only: correction is a new decision and requires both `supersedesDecisionId` and the matching active disposition version. The domain does not create tasks, detect duplicates, route a project, merge cases, or alter the underlying source.
- **Layer boundary:** Services/repositories resolve actor authorization, project membership, source/file/geometry/provenance, catalog, verified-defect and verified-evidence facts, recipient-scoped publication relations, idempotency and expected versions. Domain entities validate only the invariant facts passed in. The repository must later enforce one active report-case link, one active source disposition, current-head concurrency, recipient-to-publication relations, and atomic multi-case mutations in SQL.
- **Cross-owner proposals, not frozen/implemented interfaces:** Anh must confirm producers for `VerifiedEvidenceReference`, `ProjectGeometryContext`, and `CandidateSourceFacts`, including `source_not_ready` versus stale/failure semantics. No fake adapter or endpoint consumes them in this package.

## 6. Reporter → case → kết luận/công bố — D2, D4 và ANH-01 file interface

### 6.1 DTO và endpoint đề nghị

`EvidenceInput={fileId, capturedAt?, location?:{latitude,longitude,accuracyMeters?}, locationSource}`; `locationSource=CAPTURE|EXIF|MANUAL|UNKNOWN`; lat [-90,90], lon [-180,180], accuracy ≥0; UNKNOWN có thể location=null. Không lấy GPS lúc upload làm capture GPS. `description/reason` trim, 1..1000 ký tự; danh sách evidence không rỗng khi tạo report. Media ownership/verification kiểm server, không tin client flags.

`OwnReport={id,description,createdAt,version,evidence:[own safe evidence],routingStatus,publicUpdates}`. Không trả internal case/defect list, PM notes, AI raw data hay report của người khác. `publicUpdates` chỉ snapshot đã publish cho report đó.

`InternalCase={id,projectId?,status,version,reportIds,defectIds,verificationMethod?,warrantyRouting,conclusion?,createdAt}`. Chỉ staff có scope thấy internal links. `warrantyRouting=UNKNOWN|IN_SCOPE|OUT_OF_SCOPE`, là kết luận có provenance, không tự tính từ upload/report date.

| Endpoint | Actor/input | Success và ý nghĩa |
|---|---|---|
| `POST /api/v1/reports` | Reporter; `{description,projectHintId?,roadSectionVersionHintId?,segmentHintId?,evidence:[EvidenceInput]}` | 201 OwnReport + Location + ETag; lưu Report và case tiếp nhận, original actor và source. Hint không cấp membership hay quyết định project tự động. |
| `GET /api/v1/reports` | Reporter hiện tại; pagination | 200 chỉ report của mình. Không nhận ownerId filter tùy ý. |
| `GET /api/v1/reports/{reportId}` | Owner Reporter | 200 OwnReport + ETag. |
| `POST /api/v1/reports/{reportId}/supplements` | Owner; `{description,evidence}` + If-Match | 200 OwnReport; append supplement immutable, tăng version; không tạo case mới. |
| `GET /api/v1/cases?projectId=...&status=...` | PM project; Supervisor có unassigned queue | 200 page InternalCase; PM bắt buộc project scope, không xem queue project=null. |
| `GET /api/v1/cases/{caseId}` | Scoped PM / Supervisor | 200 InternalCase + ETag. |
| `POST /api/v1/cases/{caseId}/triage` | Supervisor gán project lần đầu; PM project chọn phương thức; `{projectId,verificationMethod,reason}` | 200 InternalCase; `verificationMethod=FIELD|DRONE|EXISTING_EVIDENCE`; không tự tạo/giao inspection task. |
| `POST /api/v1/cases/{caseId}/report-links` | PM đúng project; `{reportIds,sourceCaseVersions,reason}` + target If-Match | 200 InternalCase; move active case links nguyên tử, giữ original reports và link history. Tất cả source case cùng project, đang mở. |
| `POST /api/v1/cases/{caseId}/report-splits` | PM; `{reportIds,reason}` + If-Match | 201 new InternalCase + Location/ETag; subset không rỗng và không toàn bộ; link history giữ cả cũ/mới, không nhân report/file. |
| `POST /api/v1/cases/{caseId}/conclusions` | PM; `{outcome,defectIds,evidenceIds,reason}` + If-Match | 200 InternalCase; outcome `CONFIRMED|NO_DEFECT|NEEDS_EVIDENCE`; CONFIRMED cần defect đã Verified trong project và evidence thật; NO_DEFECT phải có căn cứ; NEEDS_EVIDENCE không là kết luận hoàn tất. |
| `POST /api/v1/cases/{caseId}/publications` | PM; `{reportIds,defectIds,summary,evidenceIds}` + If-Match | 201 `{id,caseId,status:"PUBLISHED",version}`; tạo immutable public snapshot cho đúng reports, outbox intent một lần. |
| `GET /api/v1/reports/{reportId}/evidence/{evidenceId}/download` | Owner hoặc evidence trong publication gửi đúng report đó | 200 stream từ authorized file adapter; 404 nếu không thuộc projection; không lộ raw storage URI. Upload/download storage do Anh tích hợp. |

Các command mutation có idempotency theo §4/§8. File chỉ nhận VERIFIED và đúng uploader/authorized publication scope; PENDING/FAILED trả 409 source_not_ready, không nhận tạm rồi tạo report thành công. Upload ảnh trước tạo report phải có private owner scope riêng, không yêu cầu fake project/case ID.

### 6.2 Transitions được đề nghị, không dùng trạng thái draft như authority

- Report là nguồn immutable + append supplements; case trạng thái riêng. New report tạo case `UNASSIGNED`; Supervisor triage → `OPEN` với project, không mất source. Nếu muốn auto-route known project, cần D2 khác đề nghị này.
- `OPEN → AWAITING_EVIDENCE` khi cần bổ sung; supplement → `OPEN` để PM xem lại; `OPEN/AWAITING_EVIDENCE → CONCLUDED` chỉ theo kết luận có đủ căn cứ. Supplement sau CONCLUDED giữ bằng chứng và đưa lại `OPEN`; publication cũ giữ nguyên là lịch sử, không tự sửa nội dung đã công bố.
- Linking/splitting chỉ case chưa CONCLUDED; source case rỗng sau move → `LINKED` kèm target pointer. Split tạo OPEN cùng project. Phải lock/update các case theo thứ tự ID ổn định; kiểm version tất cả case nguồn/đích.
- Trong HUY-01 không reassignment case đã có project sang project khác; cần routing correction rõ từ owner trước nếu phát sinh. Cross-project link/split cấm. Không sửa project của defect/AI/file để làm cho link hợp lệ.
- Ngoài/không rõ bảo hành vẫn nhận, không xóa/không từ chối dữ liệu; Supervisor điều phối, PM xử lý theo project được giao; không tự phán trách nhiệm/bảo hành. Chưa rõ project/PM giữ UNASSIGNED, không gán ngẫu nhiên.
- `CONCLUDED` chỉ là kết luận phản ánh, **không** là sửa chữa hoàn tất hay defect Resolved. Không có closeMixedCase/repair completion API trong HUY-01; đó là handoff HUY-02.
- Publication độc lập trạng thái case: đề nghị cho publish từng defect đã Verified liên quan report, dù case còn OPEN; text thể hiện từng phần, không báo toàn case hoàn tất. NO_DEFECT được publish khi case đã kết luận NO_DEFECT. Không publish toàn bộ ảnh internal bằng wildcard; PM chọn evidence được phép. Publication không đổi Defect.Status, không tạo nhãn Approved.
- Reporter chỉ thấy own original/supplement và snapshot đã công bố cho report mình; không thấy contact/identity/reportIds của Reporter khác. Linking nhiều nguồn không mở quyền đọc lẫn nhau. Supervisor/PM internal PII projection tối thiểu: report ID + nội dung/evidence, không mặc định trả email/số điện thoại nếu không cần điều phối.

## 7. PM candidate → defect decision → training label — PR-33A/34A, D3/D4

### 7.1 Matching và review

Nguồn `sourceKind=REPORT|AI_DETECTION|FIELD_OBSERVATION`, sourceId immutable; FIELD_OBSERVATION chỉ interface/future producer HUY-02, chưa xây capture flow. Route project luôn kiểm relation từ nguồn qua case/job/observation adapter. Reporter chưa triage không có candidate project.

| Endpoint | Request | Response/effects |
|---|---|---|
| `GET /api/v1/projects/{projectId}/candidates?sourceKind=...&sourceId=...&expand=false` | PM; optional cursor/pageSize | 200 `{source:{kind,id,version},geometryVersion,algorithmVersion:"huy01-1",items,nextCursor}`. Item `{defectId,version,segmentId?,priorityGroup,distanceMeters?,accuracyMeters?,reasonCodes,evidenceRefs,historySummary}`; version là version hiện tại của target defect. Không mutation. |
| `POST /api/v1/projects/{projectId}/candidate-decisions` | PM; `{sourceKind,sourceId,sourceVersion,geometryVersion,decision,targetDefectId?,targetVersion?,classification?,reason}` + Idempotency-Key | 201 `{id,sourceKind,sourceId,decision,defectId?,version}` + Location tới decision GET; tạo decision và effect duy nhất. |
| `GET /api/v1/projects/{projectId}/candidate-decisions/{decisionId}` | PM scope | 200 decision + ETag. |
| `GET /api/v1/projects/{projectId}/defects` và `.../defects/{defectId}` | PM scope; list filter status/type/segment | 200 projection `{id,projectId,roadSectionVersionId?,segmentId?,defectTypeCode,causeCategoryCode?,severity,status,geometry?,version}`; không expose internal raw payload. |
| `POST .../defects/{defectId}/assessments` | PM; `{defectTypeCode,causeCategoryCode?,severity,reason,evidenceIds}` + If-Match | 200 Defect + ETag; append assessment history, giữ PM decision khi suggestion thay đổi; không tự xác minh/đóng. |
| `POST .../defects/{defectId}/verification-decisions` | PM; `{decision:"CONFIRM"|"REJECT",verificationMethod,evidenceIds,reason}` + If-Match | 200 Defect + ETag; Open → Verified hoặc Rejected; không yêu cầu fake inspection task khi evidence đủ; branch cần measure/field evidence mà chưa có phải chặn. |

`classification={defectTypeCode,causeCategoryCode?,severity,roadSectionVersionId,segmentId?,geometry?}` cho KEEP_NEW. Type/cause đối chiếu catalog; severity wire mới là `LOW|MEDIUM|HIGH|CRITICAL`, map explicit tới enum hiện hữu Low=1/Medium=2/High=3/Critical=4; không dùng AI confidence suy ra severity. Defect status wire mới `OPEN|VERIFIED|REJECTED|RESOLVED`, không đổi persisted values. Geometry phải qua adapter CRS/version Anh; thiếu GPS có thể geometry=null nhưng giữ project+scope+ảnh. Không tạo Point(0,0), không coi null là đã đo/xác minh. Delta factory/mapping null geometry phải được Anh tích hợp và SQL test.

Candidate decision DTO có thêm optional `supersedesDecisionId, previousDecisionVersion` khi correction; thiếu một trong hai hoặc correction không khớp active decision trả 400/412. KEEP_NEW bắt buộc classification và cấm targetDefectId; LINK_EXISTING bắt buộc targetDefectId/targetVersion và cấm classification; REJECT cấm cả hai. Mọi decision cần reason. Field `sourceVersion` opaque đại diện source + active disposition; source AI immutable vẫn phải kèm review-state version, không chỉ job ID.

Verification proposal D3: EXISTING_EVIDENCE cần PM chọn evidence VERIFIED thuộc defect/source và ghi lý do đủ căn cứ; DRONE cần thêm dataset/source-file provenance từ Anh, không coi AI confidence là xác minh; FIELD cần completed verification result đúng task/defect từ HUY-02. Branch FIELD chưa có producer trong đợt này trả source_not_ready, không tạo kết quả giả. Các trường hợp cần số đo/method riêng mà không có hồ sơ đã duyệt không được CONFIRM; không tự đặt ngưỡng. Assessment chỉ trên OPEN/VERIFIED, verification chỉ từ OPEN; thay đổi VERIFIED/REJECTED/RESOLVED sang vòng đời khác cần correction/reopen contract ở gói sau, không âm thầm thực hiện. HUY-01 phải ghi rõ dependency FIELD khi bàn giao, không tính flow đó đã end-to-end verified.

- Matching `expand=false`: ưu tiên segment giao và immediate neighboring segments theo ordered segment set/version của Anh; so overlap với vùng sai số GPS nếu có. `expand=true`: tìm toàn project, vẫn xếp assigned/neighbor trước; không mở cross-project.
- Thứ tự deterministic theo priority group, overlap accuracy, distance khi có giá trị metric hợp lệ, lịch sử/source links liên quan, rồi ID làm tie-break. Không tính mét bằng geometry degrees; metric CRS chưa có → distance=null. Không bịa score confidence/threshold merge. Thiếu GPS: dùng scope + evidence thumbnail/references và history để PM so; không tuyên bố đã có image similarity model.
- Response nêu reason codes (`ASSIGNED_SEGMENT`, `NEIGHBOR_SEGMENT`, `ACCURACY_OVERLAP`, `HISTORICAL_LINK`, `PROJECT_EXPANSION`, `GPS_MISSING`) và model/geometry thiếu dữ liệu khi có. Không mất source thiếu GPS, không bỏ nguồn chỉ vì matching rỗng.
- Decision đề nghị: `KEEP_NEW`, `LINK_EXISTING`, `REJECT`; ADJUST thể hiện classification của KEEP_NEW hoặc assessment sau đó, không mutate AI source. KEEP_NEW tạo Open defect + source link, không inspection task; LINK_EXISTING giữ riêng nguồn, không tạo defect mới; REJECT giữ source/reason, không xóa evidence.
- Cần sourceVersion và geometryVersion; LINK_EXISTING thêm targetVersion. Re-resolve version và scope lúc commit; stale source/geometry → 409 candidate_stale; stale target → 412. Không yêu cầu candidateId ephemeral đã xem mới được link, nhưng server phải kiểm tất cả facts lại.
- Một source có tối đa một active disposition; sửa quyết định theo D3 bằng decision mới có `supersedesDecisionId` + expected previous version, giữ history và không tự xóa defect/task. Nếu defect cũ đã được downstream sử dụng, trả 409 invalid_state_transition và bàn giao correction workflow; không âm thầm chuyển link. Đây là checkpoint nghiệp vụ, không tự cascade.
- PR-33A cho phép nhiều report cùng một defect. Unique SourceAIDetectionId hiện có chỉ biểu diễn một nguồn tạo defect; thêm source-link relation cho nhiều source, không bỏ unique hoặc sửa AIDetection immutable. Do đó không dùng primitive cũ tạo lại detection đã tồn tại.

### 7.2 Training labels

Nhãn là record riêng có revision và provenance, không đồng nhất trạng thái candidate/defect. PM có thể duyệt nhãn mà không tự resolve defect; raw AI detection không là approved label.

| Endpoint | Request/actor | Success |
|---|---|---|
| `POST /api/v1/projects/{projectId}/labels` | PM; `{sourceKind,sourceId,sourceVersion,fileId,annotation,defectTypeCode,reason}` | 201 Label + Location + ETag, state PENDING. `annotation={kind:"BBOX",coordinateSpace:"NORMALIZED",x,y,width,height}`; 0..1 và nằm trong ảnh; định dạng bbox này chờ D3, không suy từ raw AI JSON. |
| `GET /api/v1/projects/{projectId}/labels` và `.../labels/{labelId}` | PM scope | 200 Label/page. |
| `POST .../labels/{labelId}/revisions` | PM; `{fileId,annotation,defectTypeCode,reason}` + If-Match | 201 Label revision mới PENDING; revision cũ immutable. |
| `POST /api/v1/labels/{labelId}/review` | PM của project được resolve server; `{decision:"APPROVE"|"REJECT",reason}` + If-Match | 200 `{id,status:"APPROVED"|"REJECTED",revision,version}` + ETag. Status này cố ý khác Ack ACCEPTED trong draft cũ, phải qua D4. |

Label projection: `{id,projectId,revision,source:{kind,id,version,jobId?,modelVersionId?,datasetVersionId?},fileId,annotation,defectTypeCode,status,reviewedBy?,reviewedAt?,reason?,version}`. Provenance do BE lấy, không cho client tự khai job/model/dataset để bypass scope. BBOX width/height phải >0, x+width và y+height ≤1; file phải là ảnh VERIFIED thực sự thuộc source. Annotation các định dạng khác không được tự convert thành bbox. Create/revision Location trỏ resource GET thuộc project; ETag là current label head sau mutation.

- D3 đề nghị PENDING → APPROVED/REJECTED; review approved/rejected lại phải tạo revision mới PENDING. Tạo revision mới không kế thừa approval. Chỉ current revision APPROVED export; revision approved cũ vẫn giữ audit và được trích dẫn trong export lịch sử, không tự sửa file export đã phát hành.
- AI adapter có thể cung cấp pending label draft qua contract Anh; không review. PM manual label cho phép flow test thật trước AI; nếu không chấp thuận manual bbox, producer nhãn phụ thuộc format Anh thống nhất và ghi BLOCKED thay vì seed giả production path.
- `IApprovedTrainingLabelReader` là interface bàn giao ANH-02: query project + cursor; trả current approved revision với approval ID/time/PM, provenance và verified file reference. Không viết export job/API của Anh. Test producer → reader bằng SQL thật; ANH-02 phải test reader → exporter thật trước nhận export DONE.
- Export query phải lọc approval trong SQL, không trả toàn bộ rồi mong exporter tự lọc. Không cho client supplied status/approvedBy; sửa annotation invalidates eligibility ngay. Race export/revision có snapshot/as-of semantics rõ trong manifest exporter do Anh quyết định; reader trả revision immutable và watermark, không hứa thu hồi export cũ.

## 8. SQL, transaction, idempotency và migration

### 8.1 Schema delta đề nghị cho Anh

Huy viết entity/config theo ownership; Anh integrate model/migrations/snapshot sau khi D tương ứng được chốt. Tên sau là đề nghị cụ thể, dùng naming hiện hành khi implement, không đổi các enum persisted cũ:

| Aggregate/tables | Dữ liệu/constraints chính |
|---|---|
| UserSession extension | Transport, LastActivityAt; rowversion đã có; check transport hợp lệ, WEB có activity, expiry cố định; legacy default/backfill không revoke hàng loạt. |
| Reports, ReportEvidence, ReportSupplements | ReporterUserId immutable, description, receivedAt; capture metadata riêng uploadedAt; file FK Restrict; unique report/evidence reference theo source, supplement append-only; rowversion Report. |
| IncidentCases, CaseReportLinks, CaseDefectLinks | nullable project chỉ UNASSIGNED; one active case link/report qua filtered unique index; link history endedAt/reason/actor; project FK Restrict; rowversion Case. Report original owner không đổi khi move. |
| CaseConclusions, CasePublications, CasePublicationRecipients/Evidence | Append-only kết luận/snapshot, revision/source versions; publication audience bằng report ID; unique publication/report recipient; không cascade xóa nguồn. |
| SourceDecisions, DefectSourceLinks | sourceKind/id + resolved project/sourceVersion; one active disposition/source, unique active source link; actor/reason/time; rowversion decision; typed FK cho Report/AIDetection, FIELD future chưa có FK/producer thì không nhận production input. |
| TrainingLabels, TrainingLabelRevisions, TrainingLabelReviews | Head/current revision rowversion; immutable source/annotation revision; unique (label,revision), một terminal decision/revision; approval actor/time; query index (project,status,current revision); FK file/catalog/actor, no cascade. |
| Defect existing | Reuse ID/status/catalog/source; thêm/expose concurrency version qua convention đúng source; optional scope/geometry cho thiếu GPS nếu D3 duyệt. Source links bổ sung, không backfill guessed project cho legacy null rows. |

Project relation phải nhất quán ở mọi link. Nếu composite FK thích hợp thì dùng alternate key `(Id,ProjectId)` sau phối hợp; nếu reference nullable/đa nguồn không thể FK trực tiếp, transaction kiểm authoritative relation và SQL tests chứng minh không có đường bypass repository. Không coi một check DTO là đủ integrity.

`RoadGuardDbContext` tự `ApplyConfigurationsFromAssembly`; chỉ thêm configuration cũng đã đổi EF runtime model, dù không thêm DbSet. Vì vậy **không** đưa mapping mới vào deployed/verified build trước migration được Anh tích hợp. Huy có thể commit source/config pending trong feature branch, nhưng phải ghi rõ model/schema drift pending, không mở production host với DB cũ và gọi đó là ready.

### 8.2 Atomic boundary

Một command commit trong cùng SQL transaction: aggregate + links/history + audit + durable idempotency outcome + outbox intent nếu cần. Create report gồm case và report links; link/split gồm toàn bộ nguồn/đích; candidate decision gồm source decision/defect/source links; review label gồm review/current state; publish gồm projection/recipients/outbox.

- Tất cả auth/project/source/version checks quan trọng lặp lại trong transaction. Dùng rowversion cho aggregates; read/lock membership/user/reference tham gia quyết định để tránh check-then-act khi revoked/reassigned đồng thời; giữ locks đến commit hoặc cơ chế equivalent được test. Retry delegate phải load entities mới, không tái add graph đã tracked sau `ChangeTracker.Clear`.
- SQL uniqueness giải quyết concurrent same-key/source, không dùng `if !exists` ngoài transaction rồi insert. Hai update cùng version tối đa một winner. Nếu client mất response sau commit, receipt trả lại outcome; trước commit fail phải rollback hết.
- Idempotency scope cho new commands: `(actorUserId, projectId?, operation:v1, key)` và fingerprint SHA-256 của normalized canonical request **bao gồm resource IDs, expected versions, source/geometry versions**. Authorization/ownership kiểm trước đọc/trả receipt; không replay dữ liệu sau khi actor mất quyền.
- Unassigned/report lifecycle có project thay đổi: các Reporter command dùng immutable receipt scope project=null + actor + operation, report ID nằm fingerprint; case-specific commands cũng dùng scope project=null + actor + operation và case ID trong fingerprint để receipt không mất khi triage. Project candidate/label commands dùng project thật. Không đổi namespace sau routing.
- Ordering: authenticate → validate request/header shape → authorize current resource/project → lookup receipt → nếu same fingerprint replay committed result → nếu key khác fingerprint 409 → nếu chưa receipt thì transaction reread auth/source/versions/transition → mutate+receipt. Nhờ đó retry với If-Match cũ của command đã commit vẫn replay; thay key nhưng stale If-Match trả 412.
- Chỉ persist success receipt cho command mới trong proposal này; validation/unauthorized/stale/error không chiếm key. Outcome lưu sanitized response/status/Location/ETag, không access token, CSRF token, password, OTP hay signed storage URL. Không tự đặt thời hạn xóa receipt/audit; retention do Anh.
- Outbox events đề nghị `report.received.v1`, `report.supplemented.v1`, `case.published.v1`, `candidate.decided.v1`, `training-label.reviewed.v1`; envelope `{eventId,eventVersion,aggregateId,aggregateVersion,projectId?,actorId,occurredAt,correlationId,payload}`. Payload tối thiểu IDs, không ảnh/raw PII. Event names/schema phối hợp Anh; chưa có consumer không được làm dispatcher hiện tại retry/dead-letter vô hạn. Integration phải route supported event types rõ; HUY-02 notification consumption chưa là DONE.
- Không SMTP/object storage/AI call trong transaction hoặc idempotency handler retry. Read file metadata/verification qua adapter trong cùng DB transaction nếu khả dụng; external byte checks hoàn tất ở upload trước đó. Không notification delivery success giả.

### 8.3 Migration/recovery

Anh audit schema/row populations trong isolated copy/fixture: sessions transport unknown; defect project/geometry null; existing uniqueness/immutable detection triggers; scopes file hiện hành. Chỉ báo aggregate counts, không xuất secrets/PII. Schema additive, nullable/backfill tương thích; không sửa migration đã áp dụng. Test migrate baseline → head và database mới → head, old row reads, indexes/checks/FKs/triggers, model snapshot matching. Recovery mặc định rollback application khi schema additive tương thích hoặc forward-fix; không destructive downgrade production làm mất reports/labels. Down/reapply chỉ trên disposable DB và chỉ khi thực sự có safe Down path. Không apply shared/production DB trong gói local này.

## 9. File ownership và interface với Anh

### 9.1 Huy được sửa

- Spec: `planning/development/HUY-01.md` (copy nội dung file này; prompt appendix có thể giữ nguyên).
- Existing module paths: `RoadGuardSystem.Services/Implementations/Authentication/`, `Interfaces/Authentication/`, `Options/IdentityOnboardingOptions.cs`, `Options/JwtOptions.cs`; `Repositories/Implementations/Identity/`, `Interfaces/Identity/`; `BusinessObjects/Identity/UserSession.cs`, `Configurations/UserSessionConfiguration.cs`; relevant auth/identity DTO/controller/validator trong allowlist chỉ để transport hiện rõ, không user-admin feature mới.
- `RoadGuardSystem.API/Authentication/` cho cookie/session handler mới và bounded bearer integration sau D1; Controllers `AuthController.cs`, `ReporterRegistrationsController.cs`, `MeController.cs`, `ProfileController.cs` chỉ thay đổi đã mô tả, thêm `WebAuthController.cs`, `AndroidAuthController.cs`.
- New module folders `BusinessObjects/{Reports,Cases,Labels}/`, `DTOs/{Reports,Cases,Defects,Labels}/`, `Services/{Interfaces,Implementations}/{Reports,Cases,Defects,Labels}/`, `Repositories/{Interfaces,Implementations}/{Reports,Cases,Defects,Labels}/`.
- Controllers mới `ReportsController.cs`, `CasesController.cs`, `CandidatesController.cs`, `DefectsController.cs`, `TrainingLabelsController.cs`; `API/Constants/Huy01ErrorCodes.cs`.
- Existing `BusinessObjects/Defects/`, `Repositories/Configurations/DefectConfiguration.cs` và configuration mới đúng entities trên; không sửa AIDetection/Processing producer của Anh. Enum mới đặt file trong module, không sửa `Common/Enums.cs` tùy tiện.
- Tests mới/ảnh hưởng ở `tests/RoadGuardSystem.{UnitTests,ApiTests,IntegrationTests}/{Authentication,Identity,Reports,Cases,Defects,Labels}/`; fixture module mới không override shared isolation. Giữ tests cũ; chỉ cập nhật expected behavior đúng decision đã chốt, không xóa test để xanh.

### 9.2 Shared files — Anh writer, Huy nêu delta trước

| File/nhóm | Delta cần Anh tích hợp |
|---|---|
| `Repositories/RoadGuardDbContext.cs`, rowversion conventions | DbSets/navigation/constraints nếu cần; kiểm discovery config và query joins. Không lách shared coordination bằng dùng Set<T> rồi tự activate model. |
| `Repositories/Migrations/*`, `RoadGuardDbContextModelSnapshot.cs` bên trong migrations | Additive session/report/case/source-link/label schema, indexes/backfill; một migration writer theo thứ tự ANH-01 trước các FK HUY-01. |
| `API/Extensions/ServiceCollectionExtensions.cs` | Explicit cookie/dual policies, module controller services, CORS/CSRF wiring; AI scheme giữ riêng. |
| `Services/Extensions/AuthenticationServiceCollectionExtensions.cs`, `Repositories/Extensions/RoadGuardPersistenceExtensions.cs` | New service/repository bindings và options validators; Huy có thể viết extension module riêng, Anh gọi từ shared composition root. |
| `API/Program.cs`, appsettings / environment options | Antiforgery/middleware order, cookie settings/allowed origins; không secret, không đổi startup seeding/isolation. |
| Central `ApiErrorCodes.cs`, `BusinessObjects/Common/Enums.cs` nếu cần | Additions rõ, không đổi numeric persisted codes; ưu tiên module-owned definitions. |
| Canonical HTTP/events contracts và integrated Postman/API.http | Adopt đúng HUY routes/DTO/errors/fixtures; giữ legacy identifiers; không promote cả draft hay tự relock FE. |
| Shared test fixtures/project files/CI | Mọi change reserve với Anh; new module fixture dùng isolation hiện có. Không update package/framework. |

Reservation chưa được Anh xác nhận: **PENDING**, không tuyên bố đã phối hợp. Không tự gửi tin nhắn cho Anh từ phiên viết spec. Huy/Codex ghi delta trong spec/PR và Huy chuyển cho Anh; không cần thêm coordination artifact.

### 9.3 Interface freeze đề nghị

| Interface và owner | Input/output + failures | Fixture tích hợp bắt buộc |
|---|---|---|
| `ProjectGeometryContext v1` — Anh producer, Huy consumer | projectId, roadSectionVersionId, segmentSetId/version, segment IDs/adjacency, metric CRS, scope version; missing/stale/wrong-project phân biệt. Huy không tự chỉnh geometry. | Hai project; adjacent/nonadjacent segments; republish version; source không GPS. Read thật → candidate query thật. |
| `VerifiedEvidenceReference v1` — Anh producer, Huy consumer | fileId, ownerUserId, nullable projectId/private intake scope, purpose, verification state, checksum, long sizeBytes, safe media metadata/version. Reporter intake trước project; scoped authorization cho download qua projection. | Reporter upload private → VERIFIED → tạo report thật → owner download; Reporter khác bị chặn; pending/failed/cross-project bị chặn. Test storage fake chỉ ghi mock storage, SQL vẫn thật. |
| `CandidateSource v1` — Anh AI producer/Huy report producer | source identity+version, project, job/model/dataset provenance khi có, geometry/scope/evidence nullable có lý do; không nhận current approved label từ AI. | BE processing/source rows → Huy candidate consumer; missing provenance trả not-ready, không fabricate. Không sửa A08/A09 hoặc retry/late-attempt. |
| `ApprovedTrainingLabel v1` — Huy producer, Anh consumer | Immutable label revision, approval proof/PM, project, source/file/checksum/provenance, watermark/cursor; approved-only. | Pending/rejected bị loại, new revision vô hiệu eligibility cũ, wrong project bị chặn; SQL producer → reader thật. Exporter integration do Anh. |
| `CaseDefectRead v1` — Huy producer, HUY-02 consumer sau | Stable case/defect IDs, source/decision versions, states, verification evidence; không tự quyền repair/close. | Chỉ contract fixture trong HUY-01; ghi HUY-02 consumer NOT IMPLEMENTED. |

### 9.4 2026-10-02 domain-package delta for Anh

- **Producer facts and failure semantics to confirm:** `VerifiedEvidenceReference(fileId,fileVersion,ownerUserId,VERIFIED,captureMetadata?)` must be resolved from storage, with `source_not_ready` for pending/failed verification and a distinct forbidden/not-found result for ownership or authorized-publication mismatch. Publication composition additionally needs `CasePublicationRecipientFacts(reportId, verifiedDefectIds, permittedEvidenceIds)`, resolved server-side for each recipient report; a missing relation/permission must fail the command rather than broaden visibility across linked reporters. `CandidateSourceFacts(CandidateSourceIdentity(kind,id,sourceVersion),projectId,geometryVersion,activeDisposition?)` must be resolved from Report/AI/Field records; absent Field producer or required geometry/provenance is `source_not_ready`, while source/geometry/disposition version drift is stale (409/412 at the wire contract). Huy has implemented only these domain inputs, not their cross-owner adapters.
- **Shared schema writer: Anh.** Integrate `Reports`, original/supplement evidence, `IncidentCases`, active report links plus append-only link/conclusion/publication history, and `SourceDecisions` with source identity/version, active disposition and correction links. Required constraints include a filtered unique active report-case link, one active source disposition, `(case/report/publication recipient)` uniqueness as applicable, restrict FKs, rowversion/current-head checks, and SQL-atomic multi-case link/split, candidate decision, publication and idempotency/outbox writes. No `DbContext`, configuration, migration, snapshot, index, or model discovery change was made here.
- **Composition writer: Anh.** After schema is accepted, wire Huy's Report/Case/Candidate repository interfaces and services through the existing shared composition roots, then adopt canonical HTTP/events and integrated Postman. This package intentionally adds no endpoint, DTO, controller, DI binding, API.http, or fake success adapter.
- **Label boundary:** Training-label persistence/endpoint remains pending until Anh freezes source/file/provenance producer facts; this checkpoint does not activate mapping or create a parallel label integration path.
- **Integration order and fixtures:** (1) Anh freezes producer facts and creates private Reporter upload/verified-file, project geometry and candidate-source fixture rows; (2) Anh integrates additive schema/migration and Huy supplies/aligns mappings only under that reservation; (3) compose services/contracts; (4) run isolated SQL tests for filtered uniqueness, rowversion and multi-aggregate rollback; (5) run API/Postman smoke for owner/scope/replay. Writer for every shared step remains Anh until an explicit reservation transfers it. Unit-domain evidence below does not prove authorization, actual verified files, SQL concurrency, durable atomicity, or external consumers.

Thứ tự: chốt semantic D1–D4 → Anh/Huy freeze interface + named writer → Huy commit entity/config/core → Anh integrate ANH-01 refs và HUY-01 migration/DI/contract → Huy nhận integration commit theo phối hợp (không tự merge nhánh Anh), chạy HTTP+SQL end-to-end → cả hai ghi base/head thực tế. Contract/mock có thể chạy sớm nhưng không thay gate SQL/real BE producer.

## 10. Acceptance và tests theo luồng

| Nhóm | Cases bắt buộc / durable proof |
|---|---|
| A — identity transport | Legacy bearer login/refresh/logout và profile/me không regression; Web cookie Secure/HttpOnly/Path/SameSite đúng, không token response/HTML redirect; login/logout/write thiếu CSRF 403; invalid bearer không fallback cookie; origins không được phép bị chặn. |
| A — clocks/concurrency | Web 29:59/30:00 idle và 11:59:59/12:00 absolute; touch không kéo absolute hoặc hồi sinh revoked; Android access 15 phút và refresh ngay trước/tại ngày 30; rotation không extend; duplicate concurrent refresh và logout/reset race; assert SQL session/token/audit đúng. OTP boundaries/concurrent verify đúng PR-37. |
| B — Reporter intake | Owner upload verified + report → đúng một Report/Case/evidence/source/audit/receipt/outbox; chưa project vẫn nhận; same key retry/concurrent/post-commit failure không nhân; same key changed payload 409. File của người khác/pending/bad checksum không commit; GPS upload khác capture không bị dùng sai. |
| B — scope/privacy | Reporter A/B báo cùng lỗi: chỉ own source/publication; đoán report/case/file IDs không lộ ảnh/contact. PM sai project, expired membership, inactive user không đọc/ghi/replay. Supervisor triage unassigned đúng authority, PM không tự nhận unassigned case. |
| B — case workflow | Supplements không tạo case mới; link nhiều report giữ nguồn và một active link; split phục hồi history; cross-project/stale multi-case mutation rollback toàn bộ; conclusion thiếu evidence bị chặn; publication partial không resolve defect, replays không nhân intent. |
| C — matching | Assigned/neighbor ưu tiên; expand vẫn cùng project; no GPS có scope+ảnh; geometry CRS/version không hợp lệ không fake distance; changed candidate/version bị chặn; matching GET không mutate/auto-merge; repeated report không tự tăng severity. |
| C — decisions/labels | Keep-new đúng một defect, link-existing không tạo defect/detection/task; reject giữ source. Không task giả. PM đúng project mới review; Supervisor/Admin/AI không được quyền chỉ vì role cao hơn. Label revision PENDING không export, REJECTED không export, APPROVED mới reader trả; edit tạo revision mất eligibility, history nguyên vẹn. |
| D — SQL risks | Fresh/baseline migration, old rows compatible, rowversion/unique/check/FK/immutable triggers; same-key two connections và same-version two commands; inject precommit failure rollback, postcommit ack loss replay; test actual HTTP status/body/headers và SQL effects. Không in-memory/SQLite thay SQL Server. |
| E — integration | Real BE geometry/file/source producers và Huy consumers; real Huy approved reader và Anh exporter khi Anh tích hợp. Mock AI/Android không được gắn external verified; HUY-02 downstream chưa triển khai. |

Test reuse: `AuthenticationFlowTests`, `AuthenticationSessionFlowTests`, `V2AuthenticationFlowTests`, `V2IdentityOnboardingFlowTests`, `Identity` API/SQL suites, `P232DetectionDefectSchemaTests`, `P232DomainInvariantTests`, `P202TransactionAndIdempotencyTests`. Chọn affected classes, không chạy RF audit suite chỉ vì tên trùng.

Thêm nhóm `Huy01*Tests` và trait `Package=HUY-01` trên test mới. Lệnh gợi ý (Codex xác minh SDK/global.json và test discovery trước chạy):

```sh
dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --nologo
dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --filter 'Package=HUY-01|FullyQualifiedName~Authentication|FullyQualifiedName~Defects' --logger trx
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --filter 'Package=HUY-01|FullyQualifiedName~Authentication|FullyQualifiedName~Identity' --logger trx
dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --filter 'Package=HUY-01|FullyQualifiedName~Identity|FullyQualifiedName~P232DetectionDefectSchemaTests|FullyQualifiedName~P202TransactionAndIdempotencyTests' --logger trx
git diff --check
```

API fixture hiện có dùng Testcontainers SQL Server, unique owned database và `AuthenticationWebApplicationFactory` kiểm owned connection, tắt startup seed. Reuse cơ chế đó; không dùng shared DB. Docker/SQL không sẵn → `NOT RUN`, chưa đạt persistence gate. Test filter chọn 0 tests không là PASS. Với changed clock code, unit dùng TimeProvider, SQL test có controlled fixture/boundary injection, không sleep chờ 30 ngày.

Hai self-review ngắn: (1) auth/scope/transition/transaction/replay/privacy; (2) ownership/interfaces/migration/compatibility/final diff. Fix trong scope, rerun phần bị invalidated. Ghi rõ external ChatGPT review PENDING, không gọi self-review là peer review.

Done HUY-01 chỉ khi phần được owner chốt đã chạy đầy đủ, mapping/migration/shared wiring tích hợp, HTTP+SQL cases pass có counts, open decisions không còn chặn flow. Nếu còn blocker, ghi đúng chức năng/dependency chưa xong; không tự đánh Done nhờ build hoặc mock.

## 11. Một lần gom quyết định còn thiếu

Đề nghị Huy trả lời `D1=A, D2=A, D3=A, D4=A` hoặc chỉnh đúng ý khác. Không phải xác nhận lại PR-33A/34A/36A/37; các câu này chốt phần còn mở trong docs. Không cần hỏi lại approval cho từng bước khi đã chọn.

| ID | Phương án A được đề nghị cụ thể | Lựa chọn khác / phần bị chặn |
|---|---|---|
| D1 — auth compatibility/topology | Giữ v1 bearer/profile/me/reset/onboarding hiện có; thêm explicit Web cookie + Android transport §5; legacy session không bị kéo dài/đổi platform. Web pilot same-site, SameSite=Lax, origins thực cấu hình lúc tích hợp. Onboarding Web hai bước verify hiện có → Web login. Không retire route trong đợt. | Nếu FE/API bắt buộc cross-site, cho biết topology/origins để dùng None+Secure+CSRF+allowlist; nếu muốn cutover breaking/one-step onboarding phải sửa compatibility delta. Chặn activation new transport/legacy expiry changes, không chặn identity core và regression. |
| D2 — tiếp nhận, ownership, case | Cho Reporter gửi chưa rõ project/ngoài bảo hành vào Supervisor queue; hints không auto-route. Một report có một active case; case nhiều reports/defects; PM link/split trong project giữ history. Reporter chỉ own sources + publication riêng. Cho partial publication của defect Verified; supplement sau conclusion mở lại review, không sửa publication cũ. | Nếu không cho partial publication hoặc muốn khác cardinality/routing/supplement policy, chỉ rõ. Chặn case schema/commands/public projection phụ thuộc; Reporter upload adapter là dependency bắt buộc Anh, không âm thầm bỏ. |
| D3 — quyết định PM/label lifecycle | Candidate giữ mới/link/reject, corrections có revision/history; keep không tự tạo inspection. PM verify theo bằng chứng, không giả measure. Label tách defect; manual PM bbox normalized v1 tạo PENDING; PM approve/reject; sửa tạo revision mới PENDING, chỉ current approved export. Không tự reopen/correct defect đã downstream sử dụng. | Nếu cần polygon/mask thay bbox, người khác tạo nhãn, hoặc approved revision cũ vẫn được xuất sau revision mới, chốt thay đổi trước schema/wire. Chặn branches này; matching core và PM scope theo quyết định đã chốt vẫn làm. |
| D4 — adopt wire/schema delta và phối hợp | Duyệt proposal endpoint/DTO/status/errors/transitions/schema trong §4–§8 **chỉ HUY-01** để Codex triển khai trên huy-review; Anh writer migration/DI/canonical contract/Postman theo §9. Không tự đổi consumer cũ, không apply production DB. | Có route/DTO/frontend constraint khác thì Huy đưa một lần; Anh phải xác nhận interface/version/writer slot trước shared integration. Chưa chốt không tự promote draft V2 hoặc gọi dependency fake là integrated. |

Các vấn đề ngoài HUY-01 (Fast Track threshold, repair/acceptance, notification dispatcher, encrypted handover, retention deletion, AI retry) giữ ở owner/gói tương ứng, không đưa vào vòng hỏi này.

## 12. Prompt thực thi hoàn chỉnh cho Codex local

Copy phần dưới cùng spec này vào Codex local. Nếu Huy đã trả lời D1–D4, append nguyên văn câu trả lời ở đầu prompt; nếu chưa thì chạy phần độc lập và dừng đúng checkpoint, không hỏi lặp.

```text
Bạn là Codex local của Huy, triển khai trọn HUY-01 theo spec đính kèm.
Repo HoangAnhVu2207/RoadGuardSystem; chỉ nhánh huy-review.
ChatGPT giữ vai trò định hướng/spec và external review; bạn implement,
hai lượt self-review/fix, chạy tests, commit/push. Không HUY-02.

1) Preflight:
   - Ghi git status --short, git branch --show-current, git rev-parse HEAD.
   - Preserve dirty files; nếu có thay đổi không thuộc task, dùng worktree sạch,
     không reset/clean/stash tự ý. Không sửa checkout của Anh.
   - git fetch origin huy-review
   - Xác minh guidance là ancestor HEAD bằng:
     git merge-base --is-ancestor 1ecae797caaed1ab912b02b2372a1940d1e05375 HEAD
   - Remote đã được ChatGPT quan sát identical guidance, nhưng phải recheck drift.
     Nếu local sạch đang ở huy-review và chỉ behind origin/huy-review:
     git merge --ff-only origin/huy-review
   - Nếu guidance chưa có, chỉ dùng:
     git merge --ff-only 1ecae797caaed1ab912b02b2372a1940d1e05375
     khi commit hiện hữu và ancestor cho phép. Nếu diverged, báo graph/base/head,
     không reset, force-push, amend hoặc cherry-pick làm giả ancestry.
   - Không fetch/merge tip anh-review thay cho exact integration commit đã phối hợp.

2) Đọc AGENTS.md, .agents/manifest.json, relevant rules/module map,
   planning/development/README.md, spec-template.md và spec HUY-01 này.
   Manifest skills=[]; không retired skills, không RF remediation/audit.
   Lưu spec tại planning/development/HUY-01.md và ghi base SHA/dirty paths thực.

3) Đối chiếu source mới với revision spec; reuse auth/onboarding/session,
   project guard, immutable detection, defect/catalog, audit/outbox/idempotency.
   Không copy nguyên primitive DetectionReviewPersistenceService để tạo task giả
   hay duplicate detection. Không coi Upload REPORT_PHOTO đã hỗ trợ Reporter.

4) Thực hiện một package theo flow:
   identity transport/core -> Reporter private evidence/intake/case -> PM matching,
   decision/defect -> training-label approval/approved reader.
   D1-D4 chưa chốt: làm độc lập đúng §3; không activate branch còn PROPOSED.
   Có câu trả lời rồi: thực hiện đúng semantics ấy, không hỏi lại từng API.
   Không tự cắt tính năng, invent business thresholds, role, publication policy.

5) Tuân thủ allowlist §9. Trước shared edit, ghi delta cụ thể/named writer/integration
   order ngay trong spec hoặc PR summary để Huy chuyển Anh. Anh owns DbContext,
   migrations/snapshot, shared DI/config, canonical contract và integrated Postman.
   Chưa có Anh integration thì persistence/integration PENDING, không dùng mock
   thay SQL evidence. Không tự sửa nhánh Anh, không áp migration shared/production DB.

6) HTTP controllers -> service policy -> repository SQL. Ownership/current project
   checks phải có cả new command và replay. Business+history+audit+receipt+outbox
   atomic; idempotency fingerprint gồm resource/version, replay trước fresh ETag
   check nhưng sau authorization. Same request/postcommit lost response không
   nhân effects. Cross-project, revoked actor, stale version đều có negative tests.
   Không SMTP/storage/AI call trong SQL retry delegate.

7) Chạy focused build/unit/API/SQL tests của §10 sau shared integration.
   SQL Server disposable owned fixture; không in-memory/SQLite thay SQL Server.
   Ghi executed/passed/failed/skipped/not-run counts; filter zero tests không PASS.
   Không broad suite nếu không có risk/gate cụ thể. External AI/Android dùng fixture
   thì chỉ ghi mock verified; external/deployment chưa kiểm chứng.

8) Self-review pass 1: auth/scope/privacy/transitions/transaction/retry/concurrency.
   Pass 2: file ownership/producer-consumer/compatibility/migrations/final diff.
   Fix trong scope, rerun invalidated tests, git diff --check. Nếu blocker còn,
   tiếp tục phần độc lập; ghi rõ phần còn thiếu, không tuyên bố DONE.

9) Commit mới trên huy-review và push bình thường origin huy-review; kiểm diff/staged
   để không gom unrelated files hoặc secrets. Không amend/force-push đang review,
   không merge develop/main. Không tự sửa conflict do concurrent push bằng rewrite.
   Trả một PR summary: behavior, base/head, changed scope, two self-review results,
   commands/counts, SQL migration integration SHA, open D/dependencies, BE verified /
   mock verified / external-deployment not verified. External ChatGPT review PENDING.
   Huy sẽ gửi base SHA/head SHA cho ChatGPT review đúng diff.
```

PR summary mẫu (điền kết quả thật, không thêm báo cáo riêng):

```text
HUY-01 — identity transport / Reporter-case / PM candidate-label
Base: <sha>; Head: <sha>; branch: huy-review; dirty preserved: <paths/none>
Behavior delivered: <luồng thực tế>; Pending: <decision/interface/SQL còn thiếu>
Shared integration: <Anh commit SHA + migration/DI/contract>; external review: PENDING
Self-review 1: <findings/fixes>; Self-review 2: <findings/fixes>
Checks: <commands; executed/pass/fail/skip/not-run counts; SQL fixture>
Evidence: BE verified <...>; mock verified <...>; external/deployment not verified <...>
```
