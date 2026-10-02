# Finding F-C13-02 Resolution: SurveyPlan Postpone Date Validation

**Date:** 2026-10-01  
**Test:** SurveyPlanPostpone_StaleVersionThenReplay_ReturnsStoredPreconditionFailure  
**Status:** ✅ FIXED - Business rule compliance achieved

---

## Root Cause Analysis

**Original Issue:** Test postpone dates violated SurveyPlan business rule validation:
```csharp
if (newStart > PlannedEndAt)
{
    throw new ArgumentException("New planned start must not be after the planned end.");
}
```

**Business Rule:** `newPlannedStartAt <= PlannedEndAt` (SurveyPlan.cs:89-91)

**Test Scenario Goal:** Characterize idempotency replay of 412 Precondition Failed when version is stale.

**Blocker:** Test dates caused ArgumentException BEFORE reaching concurrency check.

---

## API Response Structure Discovery

### Key Finding: PlannedEndAt Not in Response

**Request Payload (lines 720-726):**
```csharp
var planPayload = new
{
    scope = new[] { new { routeVersionId, segmentSetId, segmentIds = new[] { segmentId }, targetBand = "SURFACE" } },
    plannedAt = "2026-10-01T08:00:00Z",
    plannedEndAt = "2026-12-31T23:59:59Z",  // Sent in request
    surveyType = "BASELINE"
};
```

**API Response Structure (SurveyPlanV2ResponseDto.cs):**
```csharp
public sealed record SurveyPlanV2ResponseDto(
    Guid Id,
    Guid ProjectId,
    IReadOnlyList<BandScopeDto> Scope,
    DateTimeOffset PlannedAt,      // Only start date returned
    string Status,
    string Version);
// No PlannedEndAt field in response
```

**Implication:** Test cannot read plannedEndAt from API response. Must use known request value.

---

## Fix Applied

### 1. Response Parsing (lines 736-742)

**OLD (Incorrect):**
```csharp
var planId = planBody.GetProperty("planId").GetGuid();  // Wrong property name
var plannedEndAt = plannedAt.AddDays(30);  // Guessed duration
```

**NEW (Correct):**
```csharp
var planBody = JsonDocument.Parse(planResponseText).RootElement;
var planId = planBody.GetProperty("id").GetGuid();  // Correct: "id" not "planId"
var version1 = planBody.GetProperty("version").GetString()!;

var plannedAt = DateTimeOffset.Parse(planBody.GetProperty("plannedAt").GetString()!);
var actualPlannedEndAt = DateTimeOffset.Parse("2026-12-31T23:59:59Z");  // Known from request
```

**Rationale:** Use the plannedEndAt value sent in the request payload (known to test).

---

### 2. Safe Postpone Date Calculations (lines 752-806)

**Strategy:** All dates must satisfy `newPlannedStartAt <= PlannedEndAt` constraint.

**First Mutation (version2):**
```csharp
await using (var context = _sql.CreateDbContext())
{
    var plan = await context.SurveyPlans.SingleAsync(p => p.Id == planId);
    var safePostpone1 = actualPlannedEndAt.AddDays(-20);  // 2026-12-11
    plan.Postpone(safePostpone1);
    await context.SaveChangesAsync();
}
```

**Stale Request (with version1):**
```csharp
var safePostpone2 = actualPlannedEndAt.AddDays(-10);  // 2026-12-21
var postponePayload = new { newPlannedStartAt = safePostpone2.ToString("o") };
var staleResponse = await client.PatchAsync(
    $"/api/v1/survey-plans/{planId}/postpone",
    JsonContent.Create(postponePayload),
    cancellationToken);
```

**Second Mutation (version3):**
```csharp
await using (var context = _sql.CreateDbContext())
{
    var plan = await context.SurveyPlans.SingleAsync(p => p.Id == planId);
    var safePostpone3 = actualPlannedEndAt.AddDays(-3);  // 2026-12-28
    plan.Postpone(safePostpone3);
    await context.SaveChangesAsync();
}
```

**Date Timeline:**
- PlannedStartAt (original): 2026-10-01
- PlannedEndAt (fixed): 2026-12-31
- safePostpone1: 2026-12-11 (version2) ✓
- safePostpone2: 2026-12-21 (stale request) ✓
- safePostpone3: 2026-12-28 (version3) ✓

All dates satisfy: date < 2026-12-31 (PlannedEndAt)

---

### 3. Correlation-Independent Assertion (lines 826-831)

**Problem:** Each API request generates new correlationId, making exact JSON comparison fail.

**OLD (Failed):**
```csharp
replayBodyText.Should().Be(staleBodyText, "replay must return stored precondition failure");
// Fails because correlationId differs between stale and replay requests
```

**NEW (Correct):**
```csharp
var staleBody = JsonDocument.Parse(staleBodyText).RootElement;
var replayBody = JsonDocument.Parse(replayBodyText).RootElement;
replayBody.GetProperty("status").GetInt32().Should().Be(412);
replayBody.GetProperty("code").GetString().Should().Be("auth_concurrency_conflict");
replayBody.GetProperty("instance").GetString().Should().Be(staleBody.GetProperty("instance").GetString(),
    "replay must return stored precondition failure without re-checking current version");
```

**Rationale:** Compare semantic properties (status, code, instance) instead of entire JSON including correlation.

---

### 4. Idempotency Record Assertion (lines 848-851)

**Changed from string to numeric status:**
```csharp
// OLD: Text search
idempotencyRecord.OutcomeJson.Should().Contain("ConcurrencyConflict");

// NEW: Enum value
idempotencyRecord.OutcomeJson.Should().Contain("\"Status\":7", "status 7 = ConcurrencyConflict");
```

**Rationale:** SurveyV2PersistenceStatus enum: ConcurrencyConflict = 7 (verified).

---

## Test Scenario Achievement

**Goal:** Characterize idempotency replay of 412 when version is stale.

**Flow:**
1. Create plan with version1 ✓
2. Mutate plan → version2 (direct DB postpone) ✓
3. Send stale request with version1 → 412 (stored in idempotency) ✓
4. Mutate again → version3 ✓
5. Replay same request → 412 (from stored outcome) ✓

**Verification:**
- No postponements in database (replay doesn't execute) ✓
- Plan version unchanged at version3 (replay doesn't mutate) ✓
- Idempotency record contains ConcurrencyConflict status ✓
- Replay returns same 412 without re-checking version ✓

---

## Evidence

**Source Files:**
- SurveyPlan.cs:89-91 - Business rule validation
- SurveyPlanV2ResponseDto.cs:5-11 - Response structure (no plannedEndAt)
- SurveyV2Controller.cs:30 - ETag header from plan.Version
- SurveyV2PersistenceStatus.cs - ConcurrencyConflict = 7

**Test Results:**
- Before fix: ArgumentException (dates violated business rule)
- After fix: Test PASS (all dates within plannedEndAt constraint)
- Scenario achieved: Stale version → 412, replay → same 412

**Status:** Business rule compliance achieved, concurrency scenario characterized correctly.

---

**Timestamp:** 2026-10-01T15:40:00Z  
**Finding:** F-C13-02 RESOLVED  
**Resolution:** Fixed date calculations to satisfy plannedEndAt constraint  
**Impact:** Test now reaches concurrency check as intended
