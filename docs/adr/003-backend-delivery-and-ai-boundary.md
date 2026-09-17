# ADR 003: Backend acceptance now, real AI integration later

## Status and authority

Date: 2026-09-18. Task: P2-03. Owner: Person 2 / Huy branch; shared documentation correction requested by the repository owner.

Accepted scope clarification from this conversation: develop and accept the backend; Android belongs to FE; AI will be attached later. The requested target is approximately two weeks. This ADR implements that clarification without declaring future production tasks complete. Detailed endpoint names and new schema decisions remain in their assigned tasks; unresolved decisions below are not silently approved.

This supplements [ADR 001](001-backend-boundary.md) and [ADR 002](002-authentication.md). It does not alter published enums, existing field nullability or approved role/session rules. The Data Dictionary retains precedence over ERD/Domain Model, Use Cases and User Stories. Exactly two execution plans remain under `planning/`.

## Acceptance boundaries

| Deliverable | Backend acceptance in this iteration | External follow-up |
|---|---|---|
| MVP workflows | Implement all backend acceptance criteria for US-01–US-20, including authorization, versioning, audit, retries and SQL constraints; verify through API/SQL tests and exported contracts | FE Android/Web implementation and whole-product UI integration |
| Processing / AI | Durable job orchestration, versioned request/result contract, deterministic fake and result validation; provenance identifies mock source/model | Python transport adapter, trained models, GPU/inference deployment, actual vision/DSM processing and model accuracy |
| Research Validation | Import and validate pairs, compute error metrics, preserve uncertainty metadata, version runs and export reproducible reports using controlled fixtures or available external data | Field sampling, physical measurements and actual derived measurements; scientific interpretation of real-world accuracy |
| Offline work | Scoped work-package reads, resumable uploads, retry-safe commands, current permission checks and durable server acknowledgement | Local database, background queue, connectivity/UI behavior and deleting device copies |

Operational field verification remains mandatory: mock detection -> PM retains -> OPEN Defect and required inspection task -> submitted measurement -> PM VERIFIED/REJECTED. Deferring AI does not remove US-20 or permit mock processing to verify defects automatically.

Backend software acceptance can be completed before a real dataset/AI service exists. Mock/golden fixtures prove software behavior only; they must not be presented as field-trial findings. Full product/AI research acceptance is a different, later integration claim.

## Chosen approach and alternatives

Use the existing ASP.NET Core / SQL Server solution with one adapter boundary and a deterministic fake. Replacing the fake later must not change domain transitions, public job identities or evidence history. Keep the existing local storage boundary and generic worker/outbox design; this ADR does not introduce a broker, microservice or cloud vendor dependency.

Rejected for this iteration: developing the real Python/vision stack, which the owner explicitly excludes. Also rejected: removing processing/research contracts entirely, which would leave the backend unable to exercise its required workflows or integrate AI later.

## Backend contracts to implement in assigned tasks

### Processing and result provenance — P1-31 / P2-31 / P2-32

- Input identifies the job, project, RoadSectionVersion, SurveyDataVersion, immutable file/checksum manifest, model version, contract schema version and correlation/idempotency identity.
- Result refers to the same job/input/model versions, declares source provenance, and contains validated detection data or a classified failure. Store the original result immutably. Synthetic fixtures use an explicit mock model/source designation, not a claim that a real model ran.
- Validate project/version/model references before persisting any result; reject malformed, cross-project, duplicate conflicting or stale-attempt results. Lease ownership and deduplication prevent a late worker from overwriting a newer outcome.
- HTTP admission and long-running completion are separate. For accepted asynchronous work, return `202 Accepted`, a scoped status URL and documented polling guidance. A repeated admitted request resolves to the existing job; a pending status is never treated as completed work.
- Job input/result DTO details and transport mapping are owned by P1-31; durable schema/leases by P2-31. Define and test the boundary before adding a real transport. Do not add an unauthenticated callback endpoint merely to anticipate AI integration.

### Offline synchronization — P2-02 / P1-12 / P1-30 / P1-40 / P1-53

- Scope an idempotency key to actor, project and operation. Persist a request fingerprint and the outcome/operation identity. Same key plus same payload returns that identity; changed payload conflicts. Authorization is rechecked before replayed data is returned.
- Commit the domain change, audit and outbox intent in one SQL transaction where they share a database. Delivery can repeat; handlers deduplicate effects. No universal exactly-once delivery guarantee is claimed.
- Mutable workflow commands carry the expected version. Stale decisions fail with the documented conflict contract; retries never silently overwrite a concurrent PM decision. Unique-key insertion conflicts and stale updates require separate handling.
- Upload completion only admits a validation job. The backend worker verifies required files, checksums and server quality checks against the immutable manifest before `SERVER_CONFIRMED`. A status response identifies the confirmed version/files and failures or missing prerequisites.
- FE owns offline persistence and local cleanup. Backend exposes the confirmation evidence needed by FE; it does not delete a phone's local files. Normal authenticated resource reads support initial offline preparation; a generalized synchronization engine is not added by this decision.

### Research software — P1-63 / P2-63

- Reuse `FieldInspectionSession.purpose = RESEARCH_VALIDATION` and existing ground-truth/derived/run/sample fields. Pair explicit record identities within the correct session/dataset, sample ID, measurement type, unit and RoadSectionVersion; sample ID alone is not globally unique.
- Accept controlled fixtures or externally supplied data through the same validation path. Preserve original file/checksum, algorithm/source version, included/excluded/outlier choices and reasons. Published results and submitted source records are immutable.
- A hand-calculated fixture can use ground truth `[10, 20, 30]` and derived `[12, 19, 33]` in the same unit: errors `[2, -1, 3]`, bias `4/3`, MAE `2`, RMSE `sqrt(14/3)`, count `3`. Assert declared numerical precision and repeatability independently of the implementation. Test ambiguous pairs, wrong units, wrong project, zero valid samples, changed-key replay and excluded records separately.
- Uncertainty is distinct from bias/MAE/RMSE. Store an externally supplied uncertainty estimate with its method/assumptions where provided. Never fabricate `0`, rename RMSE to uncertainty or silently pick a confidence-interval method. A computed uncertainty method requires the decision below before that slice is implemented.
- Research APIs/reports must not create or transition Defect or Warranty. Verify absence of operational writes as well as correct metrics. Label synthetic datasets and missing real-world validation clearly in fixture/export provenance.

## Dependency and ownership corrections

1. `P2-02 -> P2-10 -> P2-20 -> P2-11 -> P1-12`, with P1-10 also preceding P1-12. Parent arrows mean prerequisite first. P2-04 File schema precedes project handover records.
2. Extract early File/storage (`P2-04`), defect catalog/rule schema (`P2-05`), DroneDevice (`P2-06`) and Notification (`P2-07`) prerequisites. Late administration tasks reuse them; they do not establish these shapes a second time.
3. P2-20 owns Project/ProjectMember/HandoverDocument. P2-21 owns road/version plus Warranty because Warranty may reference RoadSection. P1-20 therefore waits for both.
4. P2-30 explicitly owns SupplementarySurveyRequest, upload metadata and SurveyDataVersion/QualityCheck. P1-30 owns supplementary workflow and acknowledgement contracts; P1-31 owns processing policy.
5. P2-32 establishes DefectVerificationLog for preliminary decisions. P2-41 extends review constraints and tests after measurements exist, without replacing shared migration history.
6. Field measurements use their existing `evidence_file_id` FK to File. General Evidence references Defect, RepairItem or HandoverDocument and is completed in P2-53 after RepairItem exists. No new polymorphic measurement target is invented.
7. P2-60 explicitly maps DefectMatch/DefectMergeDecision before P1-60. P2-63 follows measurement/upload persistence directly and can precede repair/dashboard work; research is not held until the end merely because of its task number.
8. Extract project closure from P1-21 into P1-24 after open-work schemas exist. P1-23 cancellation waits for P2-30's confirmation state; P1-30 waits for worker infrastructure; P1-31 waits for detection-result persistence. P2-67 lists explicit terminal dependencies whose transitive graph covers the backend delivery scope.

## Recorded specification decisions still required

These gates affect future production slices, not this documentation correction. Proceed with unrelated tasks; the affected owner records the decision before schema or business behavior changes.

| Gate | Evidence / conflict | Affected tasks | Options requiring owner selection |
|---|---|---|---|
| D-01: initial road/version insertion | Data Dictionary makes both RoadSection.current_version_id and RoadSectionVersion.road_section_id non-null FKs. SQL Server checks constraints immediately, so the first pair needs an explicit insertion/schema strategy. | P2-21, then its dependents | Approve a nullable current pointer during atomic creation plus enforced completion, or approve a revised relationship/key design. Do not disable constraints or silently change nullability. |
| D-02: uncertainty method | RS05 requires uncertainty with a method, while fields allow missing values and no estimator/experimental design is approved. | Computed-uncertainty slice of P1-63/P2-63 | Accept supplied value+method and explicit unavailable status for backend acceptance; or approve a specified estimator, assumptions and fixtures. Core import/pair/bias/MAE/RMSE work can proceed. |
| D-03: reminder versioning | Plans require versioned reminder configuration; the current Data Dictionary ReminderRule table has no explicit version/history shape. | Reminder-version slice of P2-64/P1-64 | Approve a version/history schema, or formally revise the versioning requirement. Preserve existing schema until decided. |

Also confirm per-task operational values (file limits, quality thresholds, polling/retry bounds, membership effective-date timezone and retention policy) in options/fixtures rather than hard-coding unapproved business thresholds. These are not reasons to start AI development.

## Evidence, status and schedule

The current status table in each person plan is the source of scheduling state. Historical worklogs retain their original outcomes and commands. A plan can record a subsequent owner decision with its provenance, but this is not a fresh test run. P2-00 is owner-confirmed Done per the current plan; its old Ready for cross-review log is historical. P1-03's old synchronization warning is superseded by integration baseline 20ff1d3. P2-01 remains Ready for Codex review; actual hosted CI/Compose proof is still required where specified, and is not replaced by this documentation task.

Before this correction the original estimates totaled 72 person-days excluding the remaining P2-01 review. Those plans already assumed mock AI; deferring real AI is not a new 72-day reduction. New prerequisite tasks redistribute existing work, and original task estimates are historical sizing, not additive promises after splitting. Re-estimate from completed slices at the day-2 checkpoint. The two-week target does not waive any acceptance gate or authorize automatically reducing scope.

Each person plan contains its execution sequence, checkpoints and full coverage mapping. A task may be started only when its dependencies and applicable decision gates are satisfied. Integration/remote publishing follows repository-owner authorization; this correction performs neither.

## Research sources and applicability

Retrieved directly from official Microsoft Learn on 2026-09-18 after the search provider returned HTTP 500.

- [Asynchronous Request-Reply](https://learn.microsoft.com/en-us/azure/architecture/patterns/asynchronous-request-reply): supports separating job admission from completion and exposing polling/status with retry identities. RoadGuard uses the pattern at the API boundary without adopting the article's Azure Functions deployment.
- [EF Core concurrency conflicts](https://learn.microsoft.com/en-us/ef/core/saving/concurrency): supports SQL Server rowversion checks and explicit handling of stale updates. It also distinguishes insertion uniqueness violations from DbUpdateConcurrencyException. API conflict behavior remains a RoadGuard contract.
- [Transactional Outbox](https://learn.microsoft.com/en-us/azure/architecture/databases/guide/transactional-outbox-cosmos): supports atomic persistence of business changes and event intent followed by asynchronous delivery. The referenced implementation uses Cosmos DB; only the pattern informs RoadGuard's SQL Server design, not Cosmos-specific guarantees or components.

These sources support engineering patterns, not scientific validation of RoadGuard's AI or an estimate that all work fits two weeks.
