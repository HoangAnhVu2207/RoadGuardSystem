# RF-10-08-C01 Idempotency Operation Matrix

**Generated:** 2026-10-01  
**Status:** Initial matrix from source trace  
**Purpose:** Map per-command idempotency semantics for test planning

## IdempotencyOperationService Primitive

**Location:** `RoadGuardSystem.Repositories/Idempotency/IdempotencyOperationService.cs`

**Key semantics:**
- Composite key: `(ActorUserId, ProjectId, Operation, IdempotencyKey)`
- `IdempotencyKey`: Caller-supplied string (max 200 chars, trimmed)
- `RequestFingerprint`: SHA-256 hex (64 chars lowercase), content-based
- `Operation`: Named operation string (max 100 chars, trimmed)
- Returns: `Executed`, `Replayed` (same fingerprint), or `Conflict` (different fingerprint)

**Conflict detection:** 
- Same key + same fingerprint → `Replayed` with stored `OutcomeJson`
- Same key + different fingerprint → `Conflict` status, no execution

**Transaction handling:**
- Lines 98-144: Handler executes within transaction
- Lines 155-159: Post-commit failure recovery queries durable state
- Lines 55-70: Race condition handling via unique constraint violation catch

**Stored state:**
- `IdempotencyRecord`: Receipt with `OperationId` (from handler), `OutcomeJson`, timestamps
- Separate identity from business resource (line 51-53 comment)

## Operation Categories

### Category 1: Upload Operations

**Service:** `UploadPersistenceService`

#### 1A. Upload Session Creation
- **HTTP:** `POST /api/v1/uploads`
- **Controller:** `UploadsController.Create` (line 27-38)
- **Operation name:** `"UploadSessionCreated"` (line 15)
- **Scope:** `(actorUserId, projectId, operation, idempotencyKey)`
- **Key source:** HTTP header `Idempotency-Key` (required, line 35)
- **Fingerprint:** Request body SHA-256 (line 47)
- **Handler:** Lines 49-99 in UploadPersistenceService
- **Durable effects:**
  - `StoredFile` entity created
  - `FileScope` linkage
  - `UploadSession` created
  - `AuditLog` "upload_session_created" (p2-023)
- **Outcome:** `UploadSessionPersistenceView` JSON (line 98)
- **Authorization:** Before idempotency (controller line 34)
- **Replay projection:** Full session view with `ProjectId` from scope

#### 1B. Upload Session Completion
- **HTTP:** `POST /api/v1/uploads/{uploadId}/complete`
- **Controller:** `UploadsController.Complete` (line 64-79)
- **Operation name:** `"UploadSessionCompleted"` (line 16)
- **Scope:** `(actorUserId, projectId, operation, idempotencyKey)`
- **Key source:** HTTP header `Idempotency-Key` + `If-Match` (line 74)
- **Precondition:** `If-Match` RowVersion check (needs investigation - before or within idempotency?)
- **Durable effects:** (needs full read of CompleteAsync)

### Category 2: Survey Operations

**Service:** `SurveyV2PersistenceService`

#### 2A. Survey Plan Creation
- **HTTP:** `POST /api/v1/projects/{projectId}/survey-plans`
- **Controller:** `SurveyV2Controller.CreatePlan` (line 24-39)
- **Operation name:** `"SurveyPlanV2Created"` (line 31)
- **Scope:** `(actorUserId, projectId, operation, idempotencyKey)`
- **Key source:** HTTP header `Idempotency-Key` (required, line 28)
- **Fingerprint:** Request body SHA-256
- **Handler:** Lines 32-46 in SurveyV2PersistenceService
- **Durable effects:**
  - `SurveyPlan` entity created
  - `SurveyPlanScope` items (multi-segment)
  - `AuditLog` "survey_plan_created" (p2-54)
- **Outcome:** `SurveyV2PlanPersistenceView` JSON
- **Authorization:** Before idempotency (controller line 27)
- **Special:** Scope resolution from `RouteVersionId` before transaction

#### 2B. Survey Plan Postponement
- **HTTP:** `POST /api/v1/survey-plans/{planId}/postpone`
- **Controller:** `SurveyV2Controller.Postpone` (line 42-58)
- **Operation name:** `"SurveyPlanV2Postponed"` (line 60)
- **Scope:** `(actorUserId, null projectId, operation, idempotencyKey)`
- **Key source:** HTTP header `Idempotency-Key` + `If-Match` (line 46)
- **Fingerprint:** Request body SHA-256
- **Handler:** Lines 60-95 in SurveyV2PersistenceService
- **Precondition check:** Lines 63-66 - RowVersion check INSIDE idempotency handler
- **Concurrency handling:** Stored in outcome JSON (line 65), replayed as status
- **Durable effects:**
  - `SurveyPlan.Postpone()` mutation (line 71)
  - `SurveyPlanPostponement` record
  - `AuditLog` "survey_plan_postponed" (p2-55)
- **Outcome:** `StoredPlanOutcome` with nested status (line 94)
- **Special:** Precondition checked AFTER replay lookup, outcome stores concurrency result

#### 2C. Survey Task Creation
- **HTTP:** `POST /api/v1/projects/{projectId}/survey-tasks`
- **Controller:** `SurveyV2Controller.CreateTask` (line 61-77)
- **Operation name:** `"SurveyTaskV2Created"` (line 109)
- **Scope:** `(actorUserId, projectId, operation, idempotencyKey)`
- **Key source:** HTTP header `Idempotency-Key` (required, line 65)
- **Fingerprint:** Request body SHA-256
- **Handler:** Lines 109-136 in SurveyV2PersistenceService
- **Precondition:** Operator eligibility check (lines 112-117) INSIDE handler
- **Durable effects:**
  - `SurveyRequest` entity created
  - `SurveyAssignment` initial assignment
  - `SurveyRequestScope` items (multi-segment)
  - `AuditLog` "survey_task_created" (p2-11)
- **Outcome:** `SurveyV2TaskPersistenceView` JSON
- **Authorization:** Before idempotency (controller line 64)
- **Special:** Operator existence check inside transaction, throws if not found

### Category 3: Project Operations

**Service:** `ProjectCreationPersistenceService`

#### 3A. Project Creation
- **HTTP:** `POST /api/v1/projects`
- **Controller:** `ProjectsController.Create` (line 139-200+)
- **Operation name:** `"ProjectCreated"` (line 61)
- **Scope:** `(actorUserId, null projectId, operation, operationId.ToString("N"))`
- **Key source:** Derived from `Idempotency-Key` header via `DeriveOperationId()` (line 173)
- **Key format:** GUID formatted as 32-char hex (N format)
- **Fingerprint:** `Fingerprint(request)` method (needs inspection)
- **Handler:** `CreateAggregateAsync` (line 139)
- **Precondition:** PM eligibility and file existence checked BEFORE idempotency (lines 103-117)
- **Durable effects:** (needs full read of CreateAggregateAsync)
- **Outcome:** `ProjectCreationPersistenceView` JSON
- **Authorization:** Before idempotency (controller line 151-155)
- **Special:** Has `TryGetReplayAsync` separate method (lines 75-101) for early replay check

#### 3B. Primary PM Reassignment
- **HTTP:** `PUT /api/v1/projects/{projectId}/primary-project-manager`
- **Controller:** `ProjectsController.ReassignPrimaryProjectManager` (line 36-86)
- **Service:** `PrimaryProjectManagerService` (wraps persistence)
- **Operation scope:** `(actorUserId, projectId, operation, operationId)`
- **Key source:** `request.OperationId` from body
- **Precondition:** `ExpectedCurrentMembershipRowVersion` from body (needs investigation)
- **Authorization:** Before idempotency (controller line 48-52)

## Idempotency Ordering Patterns

### Pattern A: Authorization → Idempotency → Precondition (Survey Postponement)
1. Controller extracts actor/role
2. Controller calls service with idempotency key
3. Service invokes `IdempotencyOperationService.ExecuteAsync`
4. **Inside handler:** Check RowVersion precondition (line 63-66)
5. If precondition fails: Store failure outcome in JSON, commit transaction
6. Replay returns stored outcome (including precondition failure)

### Pattern B: Authorization → Precondition → Idempotency (Project Creation)
1. Controller extracts actor/role
2. Service calls `GetFactsAsync` for PM eligibility and file existence (lines 103-117)
3. If precondition fails: Return error WITHOUT idempotency check
4. Service invokes `IdempotencyOperationService.ExecuteAsync`
5. Handler executes business logic

### Pattern C: Authorization → Idempotency → Eligibility (Survey Task Creation)
1. Controller extracts actor/role
2. Service invokes `IdempotencyOperationService.ExecuteAsync`
3. **Inside handler:** Check operator eligibility (lines 112-117)
4. If ineligible: Throw exception, transaction rolls back
5. Exception propagates, NOT stored in idempotency outcome

## Evidence Gaps and Test Requirements

### Gap 1: Same Key, Same Fingerprint (Replay)
**Scenario:** Client retries identical request with same idempotency key
**Expected:** `Replayed` status, same outcome JSON, no duplicate entities
**Coverage needed:**
- Upload session creation replay
- Survey plan creation replay
- Project creation replay
**Evidence existing:** UNKNOWN (needs test search)

### Gap 2: Same Key, Different Fingerprint (Conflict)
**Scenario:** Client reuses key with different request body
**Expected:** `Conflict` status, no execution, no outcome
**Coverage needed:**
- Upload session creation conflict
- Survey plan creation conflict
**Evidence existing:** UNKNOWN

### Gap 3: Actor Isolation
**Scenario:** Two actors use same key for different operations
**Expected:** Separate idempotency scopes, both execute
**Coverage needed:**
- Different actorUserId, same key/operation/projectId
**Evidence existing:** UNKNOWN

### Gap 4: Project Isolation
**Scenario:** Same actor uses same key for operations in different projects
**Expected:** Separate idempotency scopes, both execute
**Coverage needed:**
- Same actorUserId/key/operation, different projectId
**Evidence existing:** UNKNOWN

### Gap 5: Precondition Changes Before Replay (Pattern A)
**Scenario:** 
1. First request: RowVersion stale → stored outcome = ConcurrencyConflict
2. Replay request: Same key, same fingerprint
**Expected:** Replayed status with stored ConcurrencyConflict outcome, NO re-check of current RowVersion
**Coverage needed:**
- Survey plan postponement with stale version
**Evidence existing:** UNKNOWN

### Gap 6: Authorization Changes Before Replay
**Scenario:**
1. First request: Actor authorized → operation executes
2. Actor role revoked
3. Replay request: Same key, same fingerprint
**Expected:** (Needs clarification - authorization before or after replay lookup?)
**Coverage needed:** Authorization timing relative to replay

### Gap 7: Race Condition (Unique Constraint Violation)
**Scenario:** Two concurrent requests with same key
**Expected:** One executes, other catches unique violation (line 55-70), queries durable state, returns Replayed
**Coverage needed:**
- Concurrent upload session creation
**Evidence existing:** UNKNOWN

### Gap 8: Commit Failure Recovery
**Scenario:** Transaction commits but acknowledgement fails
**Expected:** Retry finds durable record (lines 150-159), returns Replayed
**Coverage needed:**
- Simulated commit acknowledgement failure
**Evidence existing:** UNLIKELY (requires infrastructure simulation)

### Gap 9: Stored Outcome Replay vs Current State
**Scenario:**
1. Upload session created with status=Active
2. Session externally transitioned to Completed
3. Replay creation request
**Expected:** Replayed outcome shows original status=Active, NOT current Completed
**Coverage needed:**
- Verify replay returns stored JSON, not re-queried state
**Evidence existing:** UNKNOWN

### Gap 10: Operation Name Isolation
**Scenario:** Same key used for different operations
**Expected:** Separate idempotency scopes (operation is part of composite key)
**Coverage needed:**
- Same actor/project/key, different operation names
**Evidence existing:** UNKNOWN

## Next Steps

1. Search existing tests for idempotency coverage
2. Read complete handlers for CompleteAsync, CreateAggregateAsync
3. Identify authorization timing for each operation
4. Select representative operations per gap
5. Design focused tests (no exhaustive permutations)
6. Write tests with positive controls and fresh snapshots
7. Distinguish replay projection vs current state assertions