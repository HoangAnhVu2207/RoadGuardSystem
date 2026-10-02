# Corrected Findings Report

**Package:** RF-10-checkpoint-13-correction-02-source-supplement  
**Date:** 2026-10-01  
**Purpose:** Evidence-based finding corrections  
**Writer:** BOX 3

---

## Finding F-C13-01c: Role Revocation Replay Authorization

**Original Claim (correction-02-finding-role-revocation.md):**
> "Test expected idempotency replay to bypass current authorization, but production correctly enforces JWT claims before replay lookup."

**SOURCE EVIDENCE (IdempotencyPerCommandCharacterizationTests.cs:854-920):**

Test sequence:
1. Supervisor authenticates and creates project (201 Created, stores idempotency record)
2. Test CHANGES supervisor's role to ProjectManager in database
3. Test RE-AUTHENTICATES supervisor (obtains NEW JWT with ProjectManager role)
4. Test replays project creation with NEW JWT
5. Assertion: expects 403 Forbidden

**CORRECTED ANALYSIS:**

The test uses a **new JWT with updated claims** for replay, not the original Supervisor JWT. Line 896: `await AuthenticateAsync(client, supervisor.UserName!)` obtains fresh token AFTER role change.

**What the test characterizes:**
- Authorization check at line 23-26 in ProjectCreationService.cs: `if (actorRole != UserRoleCode.Supervisor) return Forbidden`
- This check happens with NEW JWT claims (ProjectManager), not original claims (Supervisor)
- 403 response characterizes: "current JWT role gates replay access"

**What the test DOES NOT prove:**
- Original Supervisor JWT becoming invalid
- Token revocation or invalidation mechanism
- Historical claim enforcement vs current claim enforcement

**CORRECTED CLAIM:**

Test characterizes that replay authorization uses **current JWT claims at replay time**, not historical claims from original request. When actor obtains new JWT with different role, replay sees new role. Test does not demonstrate that old JWT is revoked or becomes invalid—only that new JWT carries new role.

**Service Authorization Timing:** VERIFIED—authorization check (lines 23-26) occurs BEFORE replay lookup (line 58 TryGetReplayAsync).

---

## Finding F-C13-02: SurveyPlan Postpone Date Validation

**Original Claim (correction-02-finding-postpone-dates.md):**
> "Test postpone dates violated business rule newPlannedStartAt <= PlannedEndAt. API response omits plannedEndAt field."

**SOURCE EVIDENCE (IdempotencyPerCommandCharacterizationTests.cs:700-847):**

Test sequence:
1. Create plan with plannedEndAt = "2026-12-31T23:59:59Z" (line 725)
2. Direct database mutation: postpone to plannedEndAt.AddDays(-10) (line 755)
3. Stale request with version1 and safePostpone2 = actualPlannedEndAt.AddDays(-5) (line 769)
4. Returns 412, stores idempotency record (line 784)
5. Another database mutation: postpone to actualPlannedEndAt.AddDays(-3) (line 805)
6. Replay request with SAME payload and version1 (line 818-823)
7. Returns 412 with SAME instance ID as stale response (line 833-836)

**ASSERTION VERIFICATION:**

Lines 833-836: Test compares `replayBody.GetProperty("instance")` with `staleBody.GetProperty("instance")`—NOT comparing entire response bodies. Assertion characterizes: "stored 412 has same instance identifier."

**CORRECTED ANALYSIS:**

**What test characterizes:**
- Both stale request and replay return 412 PreconditionFailed
- Both responses have matching "instance" field (correlation identifier)
- Postponement count remains 0 after both requests (line 841)
- Plan version unchanged after replay (line 845)

**What test DOES NOT characterize:**
- Whether idempotency service re-evaluates version check during replay
- Whether "matching instance" proves replay skipped version check vs replay re-checked and got same result
- API immutability—test only checks: same 412 status, same instance ID, zero postponements, unchanged version

**Response DTO structure:** VERIFIED—SurveyPlanV2ResponseDto (dtos/SurveyPlanV2ResponseDto.cs) contains only: id, projectId, scope, plannedAt, status, version. Field plannedEndAt NOT in DTO.

**CORRECTED CLAIM:**

Test characterizes: stale-version request gets 412, stores outcome, replay with same key returns 412 with matching instance ID. Zero postponements and unchanged version show neither request mutated state. Test does NOT prove whether replay re-evaluated version check or directly returned stored 412 without re-checking.

---

## Finding F-C13-03: SurveyAssignmentData Entity Mapping

**Original Claim (correction-02-finding-assignment-entity.md):**
> "Test seeded SurveyAssignmentData (helper class) directly via DbContext.Add, but entity not registered in EF Core model. Fixed by using SurveyAssignment.Create() factory."

**SOURCE EVIDENCE (Rf1009NotificationInboxCharacterizationTests.cs:322-410):**

Test lines 339-354:
```csharp
var initialAssignmentData = CreateSurveyAssignment(initialAssignmentId, requestId, operator1.Id, pm.Id);
var initialAssignment = SurveyAssignment.Create(
    initialAssignmentData.Id,
    initialAssignmentData.SurveyRequestId,
    initialAssignmentData.OperatorUserId,
    initialAssignmentData.AssignedByUserId,
    initialAssignmentData.AssignedAt,
    acceptedAt: null, rejectedAt: null, rejectionReason: null,
    reassignmentReason: null, endedAt: null);
setup.Add(request);
setup.SurveyAssignments.Add(initialAssignment);  // Uses DbSet<SurveyAssignment>
```

Helper method lines 506-514:
```csharp
private static SurveyAssignmentData CreateSurveyAssignment(Guid assignmentId, Guid requestId, Guid operatorId, Guid assignedBy)
{
    return new SurveyAssignmentData(assignmentId, requestId, operatorId, assignedBy, DateTimeOffset.UtcNow);
}
```

**VERIFIED PATTERN:**

Test uses helper `CreateSurveyAssignment()` to construct DTO/value object, then passes values to `SurveyAssignment.Create()` factory, then adds entity via `DbSet<SurveyAssignment>`. 

**SurveyAssignmentData is VALUE OBJECT:**  
Helper class used for data transfer within test, NOT persisted directly.

**CORRECTED CLAIM:**

Test characterizes proper entity creation pattern: use domain factory `SurveyAssignment.Create()` and add via `DbSet<SurveyAssignment>`. Helper class `SurveyAssignmentData` serves as intermediate value object for test data construction, not direct DbContext.Add target.

**Entity Factory Signature (entities/SurveyAssignment.cs required for assertion verification):** Entity creation requires factory method with all initialization parameters.

---

## Finding F-C13-04: Notification Inbox GET Projection

**Status:** SOURCE_INSPECTED (correction-01)

**Test Evidence (Rf1009NotificationInboxCharacterizationTests.cs:36-100):**

Test name: `InboxGet_RecipientScopeAndProjection_FiltersAndReturnsExpectedFields`

Lines 86-100: Asserts response contains fields: id, message, resourceId, read, occurredAt. Test characterizes: inbox GET projects expected notification fields and filters by recipient.

**VERIFIED:** Test characterizes inbox authorization (NotFound for cross-access) and field projection. F-C13-04 finding correctly classified as SOURCE_INSPECTED.

---

## Findings Status After Source Correction

| Finding | Correction | Status | Coverage | Open Issues |
|---------|-----------|---------|----------|-------------|
| F-C13-01a | C01 | FIXED | Upload status PENDING assertion | None |
| F-C13-01b | C01 | SOURCE_INSPECTED | Actor isolation via DbSet filtering | None |
| F-C13-01c | C02 | FIXED | Authorization with current JWT claims | **CLAIM PRECISION**: Test uses NEW JWT, not revoked old JWT |
| F-C13-02 | C02 | FIXED | Stale version returns stored 412 | **COVERAGE GAP**: Test does not prove replay skips re-evaluation |
| F-C13-03 | C02 | FIXED | Entity factory pattern | **TERMINOLOGY**: SurveyAssignmentData is value object, not entity |
| F-C13-04 | C01 | SOURCE_INSPECTED | Inbox GET recipient filtering | None |

---

## Open Coverage Gaps

### Gap F-C13-02-EVAL: Replay Version Check Re-evaluation

**Observation:** Test verifies matching 412 responses and matching instance IDs, but does NOT prove whether replay:
- (A) Directly returns stored 412 without checking current version, OR
- (B) Re-checks version and happens to get same 412 result

**Evidence Needed:**
- Source inspection of SurveyV2PersistenceService replay path
- Assertion showing replay does not read current plan version from database
- Or opposite: assertion proving replay DOES read version (coverage for scenario B)

**Current Assertion Covers:** "Replay returns stored outcome with matching correlation identifier and no state mutation."

**Current Assertion DOES NOT Cover:** "Replay skips version re-evaluation."

### Gap F-C13-01c-HISTORICAL: Original JWT Validity

**Observation:** Test obtains NEW JWT after role change. Does not characterize what happens if original Supervisor JWT is used for replay.

**Evidence Needed:**
- Test scenario: Keep original JWT (do not re-authenticate), attempt replay
- Assertion: Does original JWT still work? Does service reject it? Does JWT carry stale claims?

**Current Assertion Covers:** "Replay authorization sees current JWT claims."

**Current Assertion DOES NOT Cover:** "Original JWT is revoked" or "Original JWT becomes invalid."

---

## Recommendations for Future Corrections

1. **F-C13-01c**: Add test with original JWT for replay (without re-auth) to characterize historical JWT behavior
2. **F-C13-02**: Source-inspect SurveyV2PersistenceService.PostponeAsync replay path to verify (A) vs (B)
3. **F-C13-03**: Update terminology—SurveyAssignmentData is value object/DTO, not "helper class not registered in DbContext"
4. **All findings**: Reports must distinguish "test asserts X" from "X implies Y"—only source evidence proves implications

---

## Summary

✓ Source evidence extracted from test files  
✓ Assertions verified against actual test code  
✓ Claims corrected to match observable evidence  
✓ 2 coverage gaps identified requiring additional work  
✗ Tests do NOT prove all original correction-02 claims  
✓ Findings remain FIXED status (tests pass), but claim precision corrected
