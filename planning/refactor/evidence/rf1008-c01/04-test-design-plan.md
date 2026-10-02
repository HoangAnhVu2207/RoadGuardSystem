# RF-10-08-C01 Test Design Plan

**Generated:** 2026-10-01  
**Purpose:** Focused test design for identified evidence gaps

## Design Principles

1. **No exhaustive permutations** - select representative operations per gap
2. **Positive controls** - fresh actors/projects, comprehensive snapshots
3. **Distinguish replay projection vs current state** - mutate entity after first request
4. **Reuse existing patterns** - leverage P120/P2 test structure
5. **One test class** - `IdempotencyPerCommandCharacterizationTests.cs` in `RoadGuardSystem.ApiTests`

## Selected Representative Operations

| Gap | Operation | Reason |
|---|---|---|
| Gap 1: Same key + same fingerprint replay | Upload Create, Survey Plan Create | Missing in upload, strengthen survey coverage |
| Gap 2: Same key + different fingerprint conflict | Upload Create, Survey Plan Create | Missing in both |
| Gap 3: Actor isolation | Upload Create | Clear composite key verification |
| Gap 4: Project isolation | Upload Create, Survey Task Create | Both have non-null projectId |
| Gap 5: Precondition stored in outcome replay | Survey Plan Postpone | ONLY pure Pattern A example |
| Gap 6: Authorization change before replay | Project Create | Strong existing coverage, extend to actor role change |
| Gap 9: Stored outcome vs current state | Upload Create | Session status mutation after creation |

**NOT tested exhaustively:**
- Gap 7: Race condition - P120 line 104-111 already demonstrates
- Gap 8: Commit failure recovery - P120 line 119-126, 341-386 already demonstrates
- Gap 10: Operation name isolation - lower priority, implied by composite key

## Test Class Structure

```
tests/RoadGuardSystem.ApiTests/Idempotency/IdempotencyPerCommandCharacterizationTests.cs

[Trait("TaskId", "RF-10-08-C01")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class IdempotencyPerCommandCharacterizationTests
{
    // Fixtures and helpers
    
    // Gap 1: Replay
    [Fact] UploadCreate_SameKeySamePayload_ReplaysWithIdenticalOutcome()
    [Fact] SurveyPlanCreate_SameKeySamePayload_ReplaysWithIdenticalOutcome()
    
    // Gap 2: Conflict
    [Fact] UploadCreate_SameKeyDifferentPayload_ReturnsConflict()
    [Fact] SurveyPlanCreate_SameKeyDifferentPayload_ReturnsConflict()
    
    // Gap 3: Actor isolation
    [Fact] UploadCreate_DifferentActorsSameKey_BothExecute()
    
    // Gap 4: Project isolation
    [Fact] UploadCreate_SameActorSameKeyDifferentProjects_BothExecute()
    [Fact] SurveyTaskCreate_SameActorSameKeyDifferentProjects_BothExecute()
    
    // Gap 5: Stored precondition outcome
    [Fact] SurveyPlanPostpone_StaleVersionThenReplay_ReturnsStoredPreconditionFailure()
    
    // Gap 6: Authorization change before replay
    [Fact] ProjectCreate_ActorRoleRevokedAfterCreate_ReplayReturnsStoredOutcome()
    
    // Gap 9: Stored outcome vs current state
    [Fact] UploadCreate_StatusMutatedAfterCreate_ReplayReturnsOriginalStatus()
}
```

## Detailed Test Specifications

### Gap 1 Tests: Replay

#### UploadCreate_SameKeySamePayload_ReplaysWithIdenticalOutcome

**Setup:**
- Create Supervisor, ProjectManager
- Authenticate as PM
- Create project

**First request:**
- POST /api/v1/uploads with payload A, idempotency key K1
- Assert 201 Created
- Capture response body text, uploadId, fileId, version

**Snapshot after first:**
- Count: UploadSessions, Files, FileScopes, AuditLogs, IdempotencyRecords
- Read: UploadSession by uploadId (Id, FileId, Status, OwnerUserId, ExpectedSizeBytes, CreatedAt, RowVersion)
- Read: FileScope by fileId (FileId, ProjectId, OwnerUserId)
- Read: IdempotencyRecord (ActorUserId, ProjectId, Operation="UploadSessionCreated", IdempotencyKey=K1, RequestFingerprint, OutcomeJson)

**Replay request:**
- POST /api/v1/uploads with SAME payload A, SAME key K1
- Assert 201 Created
- Assert response body text EXACTLY equals captured first response

**Snapshot after replay:**
- Count unchanged: same counts as after first
- Read: UploadSession by uploadId - SAME values as first snapshot
- Read: FileScope by fileId - SAME values
- Read: IdempotencyRecord - SAME record, count=1

**Assertions:**
- Response: status 201, identical body text (not just matching fields, but byte-for-byte JSON)
- SQL: single UploadSession, single File, single FileScope, single AuditLog, single IdempotencyRecord
- Identity: uploadId, fileId from replay match first request
- Content: OutcomeJson contains uploadId, fileId, status=ACTIVE, projectId

#### SurveyPlanCreate_SameKeySamePayload_ReplaysWithIdenticalOutcome

**Similar structure:**
- POST /api/v1/projects/{projectId}/survey-plans with scope S, key K2
- Snapshot: SurveyPlans, SurveyPlanScope, IdempotencyRecords
- Replay with same scope/key → identical response

### Gap 2 Tests: Conflict

#### UploadCreate_SameKeyDifferentPayload_ReturnsConflict

**Setup:**
- Same as Gap 1

**First request:**
- POST /api/v1/uploads with payload A (fileName="first.mp4"), key K3
- Assert 201 Created

**Changed payload request:**
- POST /api/v1/uploads with payload B (fileName="second.mp4", DIFFERENT fingerprint), SAME key K3
- Assert 409 Conflict
- Assert problem code "duplicate_request"

**Verification:**
- Count: single UploadSession (only first succeeded)
- IdempotencyRecord exists for K3 with fingerprint of payload A
- NO second session created

#### SurveyPlanCreate_SameKeyDifferentPayload_ReturnsConflict

**Similar structure:**
- First: scope with segment S1, key K4 → 201
- Changed: scope with segment S2 (different fingerprint), same key K4 → 409

### Gap 3 Test: Actor Isolation

#### UploadCreate_DifferentActorsSameKey_BothExecute

**Setup:**
- Create PM1, PM2
- Create project P1 (PM1 as primary)
- Grant PM2 access to project P1 (or create second project P2)

**Actor 1 request:**
- Authenticate as PM1
- POST /api/v1/uploads to project P1, key K5 → 201, uploadId=U1

**Actor 2 request:**
- Authenticate as PM2  
- POST /api/v1/uploads to project P1, SAME key K5 → 201, uploadId=U2

**Verification:**
- Two UploadSessions: U1 (actor PM1), U2 (actor PM2)
- Two IdempotencyRecords: (PM1, P1, "UploadSessionCreated", K5), (PM2, P1, "UploadSessionCreated", K5)
- Composite key scope includes ActorUserId

### Gap 4 Tests: Project Isolation

#### UploadCreate_SameActorSameKeyDifferentProjects_BothExecute

**Setup:**
- Create Supervisor, PM
- Create project P1, project P2

**Project 1 request:**
- Authenticate as PM
- POST /api/v1/uploads to project P1, key K6 → 201, uploadId=U1

**Project 2 request:**
- Authenticate as PM (SAME actor)
- POST /api/v1/uploads to project P2, SAME key K6 → 201, uploadId=U2

**Verification:**
- Two UploadSessions: U1 (projectId=P1), U2 (projectId=P2)
- Two IdempotencyRecords: (PM, P1, operation, K6), (PM, P2, operation, K6)

#### SurveyTaskCreate_SameActorSameKeyDifferentProjects_BothExecute

**Similar structure** with survey task creation

### Gap 5 Test: Stored Precondition Outcome (Pattern A)

#### SurveyPlanPostpone_StaleVersionThenReplay_ReturnsStoredPreconditionFailure

**Setup:**
- Create Supervisor, PM
- Create project, road section, segments
- Create survey plan → planId, version V1

**First postpone with stale version:**
- Update plan externally to version V2
- POST /api/v1/survey-plans/{planId}/postpone with If-Match V1 (stale), key K7
- Assert 412 Precondition Failed
- Capture response body

**Snapshot after stale:**
- IdempotencyRecord exists: (PM, null, "SurveyPlanV2Postponed", K7, fingerprint F7)
- OutcomeJson contains: status=ConcurrencyConflict
- NO SurveyPlanPostponement created

**Mutate plan state:**
- Update plan externally to version V3

**Replay with same key/fingerprint:**
- POST /api/v1/survey-plans/{planId}/postpone with If-Match V1 (SAME stale), SAME key K7
- Assert 412 Precondition Failed
- Assert response body matches captured first response

**Critical assertion:**
- Replay returns stored 412 WITHOUT re-checking current version V3
- Plan remains at version V3 (no mutation attempted)
- IdempotencyRecord unchanged, single record

### Gap 6 Test: Authorization Change Before Replay

#### ProjectCreate_ActorRoleRevokedAfterCreate_ReplayReturnsStoredOutcome

**Setup:**
- Create Supervisor S, PM
- Authenticate as Supervisor S

**First request:**
- POST /api/v1/projects with operationId O1 → 201, projectId P1
- Capture response body

**Revoke authorization:**
- Update user S: change role to ProjectManager (or DroneOperator)
- Verify S no longer has Supervisor role

**Replay request:**
- Authenticate as S (now non-Supervisor)
- POST /api/v1/projects with SAME operationId O1, SAME payload
- Assert 201 Created
- Assert response body EXACTLY matches first response

**Verification:**
- Actor S now has ProjectManager role (authorization changed)
- Replay returns stored Success outcome (NOT re-checked authorization)
- Single project P1 created
- Single IdempotencyRecord

**Note:** Controller authorization at line 151-155 extracts role from JWT. After role change, NEW login gets updated JWT. Test needs to:
1. Create project with Supervisor JWT
2. Change role in DB
3. Login again to get PM JWT  
4. Retry create with PM JWT, same operationId

### Gap 9 Test: Stored Outcome vs Current State

#### UploadCreate_StatusMutatedAfterCreate_ReplayReturnsOriginalStatus

**Setup:**
- Create Supervisor, PM
- Authenticate as PM
- Create project

**First request:**
- POST /api/v1/uploads with key K8 → 201, status=ACTIVE
- Capture uploadId, response body

**Mutate entity state:**
- Update UploadSession: Status = Verified, VerifiedAt = now
- Verify current DB state shows status=VERIFIED

**Replay request:**
- POST /api/v1/uploads with SAME key K8, SAME payload
- Assert 201 Created
- Parse response: status field = "ACTIVE" (NOT "VERIFIED")

**Verification:**
- Response shows original status=ACTIVE from stored OutcomeJson
- Database shows current status=VERIFIED
- Replay does NOT re-query current state

## Snapshot Requirements

Each test must capture:
- **Before:** baseline counts (if testing "no write on failure")
- **After first:** entity identity (ID, RowVersion), entity content (status, foreign keys), related entities (scopes, audit logs), idempotency record (fingerprint, outcome JSON)
- **After replay:** same structure as after first, verify unchanged

**Snapshot pattern from P120:**
```csharp
await using var verification = _sql.CreateDbContext();
var project = await verification.Projects
    .AsNoTracking()
    .SingleAsync(p => p.Id == projectId);
project.Id.Should().Be(expectedId);
project.Status.Should().Be(ProjectStatus.Active);
// ... comprehensive field checks

var idempotencyRecord = await verification.IdempotencyRecords
    .AsNoTracking()
    .SingleAsync(r => r.Operation == "ProjectCreated" && r.IdempotencyKey == expectedKey);
idempotencyRecord.ActorUserId.Should().Be(supervisorId);
idempotencyRecord.RequestFingerprint.Should().Be(expectedFingerprint);
// Parse and verify OutcomeJson
```

## Reuse Strategy

**From P120ProjectCreationTests.cs:**
- Helper: `ValidRequest(pmId)` pattern
- Helper: `AuthenticateAsync(client, username, password)`
- Helper: `ProblemCodeAsync(response)`
- Assertion: identical body text (line 80, 195)
- Assertion: single entity counts (line 83-94)

**From P2SurveyV2ApiTests.cs:**
- Helper: `CreateProjectAsync(client, managerId)`
- Helper: `CreateRoadSectionAsync(client, projectId)`
- Segment setup: direct DB insert (line 38-43)
- Helper: `PostponeAsync(client, planId, key, version)` (line 129-135)

**From UploadApiTests.cs:**
- Mock storage: `EndpointUploadStorage` (line 290-300)
- Snapshot: scope/session verification (line 165-167)

## Test File Organization

```
tests/
  RoadGuardSystem.ApiTests/
    Idempotency/
      IdempotencyPerCommandCharacterizationTests.cs  (NEW)
```

**Trait:** `[Trait("TaskId", "RF-10-08-C01")]`  
**Collection:** `[Collection(AuthenticationApiFixture.Name)]`  
**Status:** All tests marked NOT_RUN in comments until BOX 3 verification

## Next Steps

1. Write test class with 11 focused tests
2. Use existing P120/P2/Upload helper patterns
3. Mark all tests NOT_RUN in source comments
4. Create baseline document with test list and expected evidence
5. Do NOT run tests (BOX 3 responsibility)
6. Document in BOX1-READY.md for handoff
