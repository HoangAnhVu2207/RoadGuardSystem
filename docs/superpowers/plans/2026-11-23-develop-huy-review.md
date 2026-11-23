# Develop/Huy integration review — 2026-11-23

CURRENT_VERIFIED review base `4934103d125be1caca4c6a588eed3768c9cafee4`; reviewed HEAD `86e9f4e25f2e310f940e45c5cf60efd8d875a61a`, branch `anh`. This is Codex source review and local backend verification, not external ChatGPT/GitHub acceptance or deployed acceptance.

## Scope and preservation

Review owner: startup-review subagent, delegated by the integrating root writer. Reserved writes: this new note, new `tests/RoadGuardSystem.ApiTests/Startup/UploadWorkerCompositionTests.cs`, and root-approved removal of the Development condition from the upload-verifier registration in `RoadGuardSystem.API/Program.cs`. No migration/schema, package, contract, shared SQL or deployment edits.

Pre-existing dirty tracked paths were `.gitmodules`, the AI submodule, `Program.cs`, `ProcessingJob.cs`, `CandidateDecisionRepository.cs`, `UploadPersistenceService.cs`, `FieldInspectionWorkflowRepository.Business.cs`, `ProcessingV2PersistenceService.cs`, `SurveyV2PersistenceService.Dataset.cs`, `RoadGuardDbContextModelSnapshot.cs`, `RoadGuardDbContext.cs`, `UploadService.cs`, `Anh02Contracts.cs`, `AiCandidateReadBoundaryAcceptanceTests.cs`, and `H3FieldInspectionWorkflowSqlTests.cs`. Pre-existing untracked AI worker/telemetry/migration/contract/test/spec/local-host files were retained. The existing `AddAiRealWorker` Program addition remains intact. Other delegated writers may extend this dirty set; this note does not claim ownership of their edits.

## Seven commits and observed corrections

| Commit | Observed delta | Source evidence / scope limit |
|---|---|---|
| `c0c99ca` | Assignment/transfer responsibility discovery, persisted dataset Survey ID, retention evidence, FIELD measurement codes | `planning/development/ASSIGN-RETENTION-FIELD.md`; entity/create and responsibility/detail projections. No broad receiving-project source access inferred. |
| `368e556` | Crew/Operator paging/filter/status readback and source-backed mobile notifications | `planning/development/MOBILE-CREW-OPERATOR-LISTS-AND-STATUS.md`; processing summary is independent from dataset assessment/PM confirmation. |
| `2ab6539` | Explicit PM task completion from exact current dataset/assessment plus Operator assessment notifications | `planning/development/SURVEY-COMPLETION-AND-ASSESSMENT-NOTIFICATIONS.md`; preserves separate task/plan state and immutable completion basis. |
| `5acb90c` | Persisted Survey type/parent readback, scoped conflicts, explicit plan completion | `planning/development/SURVEY-TYPE-CONFLICT-PLAN-COMPLETION.md`; current PM/source/lineage guards and active-scope index remain. |
| `9a022bd` | Aligns Survey scope-conflict SQL assertion with existing distinct contract | Test assertion change; not evidence of a runtime policy change. |
| `973e318` | FIELD SQL trigger now accepts measurement 1–6; initial transferred TemporarySafety source responsibility; safe MOCK diagnostics/correlation | `planning/development/FIELD-SAFETY-AI-INTEGRATION.md`; two additive Nov23 migrations. Actual external trigger patch/FE DB not verified. |
| `86e9f4e` | Exact effective Crew SUPPLEMENT reason; API acceptance expectations for prior fields/migrations | `planning/development/CREW-SUPPLEMENT-REASON.md`; diagnostic nullable fields and migration-last assertions updated. Historical hosted failures described there are not fresh results from this review. |

CURRENT_VERIFIED: Huy fixes the entity/SQL measurement mismatch, explicit survey completion/readback gaps, missing source navigation/notification readback and current supplement reason within those specs. Huy's MOCK diagnostics and candidate readback do not implement the local REAL AI pipeline.

## Auto-merged seams

- `FieldInspectionWorkflowRepository.Business.cs`: Huy adds `RequiredMeasurementType` 1–6 validation. Preserved local REAL branch additionally requires same project/model, completed processing job, ingested work for the dataset and latest processing attempt. Source/pin checks remain before task creation. This is source review; another integration slice must execute REAL → Defect → FIELD.
- `SurveyV2PersistenceService.Dataset.cs`: Huy's `EmitMobile(...SUBMITTED...)`, processing summaries and persisted Survey ID coexist with the local durable `ai.real.dataset_submitted.v1` outbox insertion in the same dataset transaction. No media/model/network call was inserted into that transaction. The notification does not approve a dataset or complete the plan.
- `RoadGuardDbContextModelSnapshot.cs`: Huy's lifecycle-kind constraint 1–8 coexists with local AI work/artifact/telemetry/outcome entities. Fresh EF `HasPendingModelChanges()` check passes in the new startup test. No snapshot rewrite was needed.

## Confirmed remaining startup issue and correction

CURRENT_VERIFIED: before this review's fix, Program registered `UploadVerificationWorker` only when `IsDevelopment()` and a MinIO endpoint were present. Production with a configured endpoint registered zero verifiers. This breaks the hosted upload-verification leg required before downstream confirmed-file/dataset/REAL admission.

Reproduced via actual Program service descriptors before fixture removal of hosted workers: Production+endpoint expected 1, actual 0; Development+endpoint and no-endpoint controls passed. Root authorized the smallest correction: keep the existing nonblank endpoint opt-in and remove the environment restriction. No new configuration or direct verification bypass. Test fixture captures registrations and removes all workers before host startup; it performs no SQL/storage/AI call.

## Migration sequencing

CURRENT_VERIFIED: discovered order is Nov17 Web metadata → `20261119161424_AddAiWorkerTransportLedger` → `20261119164737_AddAiVideoTelemetryRevisions` → `20261119170649_AddAiVideoAttemptOutcomes` → `20261123093000_FieldTaskMeasurementDimensions` → `20261123093100_InitialTemporarySafetyResponsibility`. Generated SQL over this range creates all three AI tables, includes the FIELD trigger replacement and contains no `DROP TABLE`. Model/snapshot equality passes. No migration or snapshot was edited here.

UNKNOWN / dependent deployment gate: a database already at Huy's Nov23 head but without the Nov19 local AI history must be audited before applying pending migrations; no shared DB was queried here. Generated SQL/model checks do not prove an actual mixed-history upgrade or recovery. Existing owned-container `WorkerLedgerPersistenceTests.Migration_DownUpPreservesExistingProcessingFacts` is the focused rehearsal candidate; it must preserve processing facts and reapply both later Huy trigger migrations. Never downgrade a shared database to make the history chronological.

CURRENT_VERIFIED remaining rollback/data-policy risk in local AI migrations: all three Nov19 `Down` implementations drop their AI evidence tables without a populated-data refusal. The artifact/telemetry/outcome runtime triggers protect UPDATE/DELETE, not `DROP TABLE`; their append-only evidence can therefore be lost by rollback. Huy's Nov23 migrations explicitly refuse downgrade while incompatible measurement types or kind-8 history remain. No migration was edited here. Fixer: shared migration writer after policy approval; until then rollback with populated AI transport evidence is not an approved recovery path. A disposable empty/reseed rehearsal does not establish evidence-preserving production rollback.

## Flow verification plan and evidence

| Flow | Focused check | Review evidence |
|---|---|---|
| Configured Production storage → hosted upload verifier | New actual-Program descriptor regression with Development/no-endpoint controls | Red 4 PASS/1 FAIL; green below. No worker/storage network execution. |
| Current model → integrated migration sequence | New `HasPendingModelChanges` and generated SQL test | PASS; SQL application not implied. |
| Fresh/patched FIELD trigger → measurement codes/source/pins | Existing `FieldMeasurementTriggerHttpTests` (two cases) | NOT RUN in initial startup batch. |
| Formal NORMAL item → TemporarySafety before/after transfer | Existing `TemporarySafetyObligationHttpTests` | NOT RUN by this reviewer; spec/historical evidence only. |
| Manual dataset assessment → supplement → explicit task/plan completion | Existing `Anh01SurveyFlowTests` | NOT RUN by this reviewer; spec/historical evidence only. |
| REAL upload → telemetry → hosted ingest → candidate → PM KEEP_NEW → Crew FIELD | Root's local integration journey and consumer tests | Separate delegated evidence; this startup review does not claim full E2E. |

Executed with `DOTNET_PROCESSOR_COUNT=2`, `--no-restore`, `-m:1`; no configured SQL environment used:

- `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --filter FullyQualifiedName~UploadWorkerCompositionTests --logger trx...`: `artifacts/huy-startup-review/huy-startup-red.trx`, 4 PASS / 1 FAIL / 0 SKIP. Only Production verifier composition fails as intended.
- After Program correction, same project with filter `FullyQualifiedName~UploadWorkerCompositionTests|FullyQualifiedName~ApiStartupTests`: `artifacts/huy-startup-review/huy-startup-green.trx`, 7 PASS / 0 FAIL / 0 SKIP. Fresh API and dependency build succeeded with existing analyzer warnings.
- `git diff --check`: exit 0 at initial review checkpoint.

Final CURRENT_VERIFIED follow-up: the test-only disabled REAL worker configuration key was corrected from `AiWorker:Enabled` to the actual `AiRealWorker:Enabled` after the initial green batch. The consumers writer then freshly rebuilt and ran the exact current source with filter `SurveyAiCaseDefectReportingSqlTests|Huy01CaseDefectReadSqlTests|UploadWorkerCompositionTests|ApiStartupTests`. `tests/RoadGuardSystem.ApiTests/TestResults/consumer-api-green.trx` reports 10 PASS / 0 FAIL / 0 SKIP, including these same 7 startup cases plus 3 consumer cases. This reviewer parsed the TRX and confirmed all five new startup identities passed; the consumers writer separately confirmed the complete 7 startup cases and the corrected configuration key were included. No redundant rebuild was run by this reviewer.

## Two review passes

Pass 1 (self-review): traced configured storage registration → upload verification → downstream confirmed-file dependency; reproduced actual Program registration failure before correction. Inspected Huy authority/source/measurement and dataset transaction changes against the assigned specs. No business authority was inferred from status names or passing tests.

Pass 2 (self-review): checked shared-file ownership, preserved dirty REAL additions, model/snapshot equality and chronological migration generation; kept workers off in disconnected tests and corrected the REAL disable key. No migration/schema/public contract edits. External review and deployed SQL/storage/provider/FE acceptance remain NOT VERIFIED.
