# Finding F-C13-03 Resolution: SurveyAssignmentData Entity Mapping

**Date:** 2026-10-01  
**Test:** Producer_CreateOutbox_LinksCorrectCorrelationAndType  
**Status:** ✅ FIXED - Entity properly mapped via factory

---

## Root Cause Analysis

**Original Issue:** Test attempted to seed `SurveyAssignmentData` directly via `DbContext.Add()`, causing `InvalidOperationException: The entity type 'SurveyAssignmentData' cannot be added because it was not found in the model.`

**Problem:** `SurveyAssignmentData` is a helper/DTO class, not a registered Entity Framework Core entity.

**Production Design:** Domain uses `SurveyAssignment` entity with proper factory method for initialization.

---

## Entity Architecture

### SurveyAssignmentData (Helper Class)

**Purpose:** Data transfer object for assignment creation parameters.

**Location:** RoadGuardSystem.Domain namespace (not registered in DbContext)

**Structure:**
```csharp
public class SurveyAssignmentData
{
    public Guid SurveyPlanId { get; set; }
    public Guid RouteSegmentId { get; set; }
    public Guid RouteVersionId { get; set; }
    public TargetBand TargetBand { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
    public Guid? OperatorUserId { get; set; }
}
```

**Issue:** Not a DbSet entity, cannot be tracked by Entity Framework.

---

### SurveyAssignment (Domain Entity)

**Purpose:** Persisted domain entity for survey assignments.

**Factory Method (SurveyAssignment.cs):**
```csharp
public static SurveyAssignment Create(
    Guid surveyPlanId,
    Guid routeSegmentId,
    Guid routeVersionId,
    TargetBand targetBand,
    DateTimeOffset assignedAt,
    Guid? operatorUserId)
{
    return new SurveyAssignment
    {
        Id = Guid.NewGuid(),
        SurveyPlanId = surveyPlanId,
        RouteSegmentId = routeSegmentId,
        RouteVersionId = routeVersionId,
        TargetBand = targetBand,
        AssignedAt = assignedAt,
        OperatorUserId = operatorUserId,
        Status = SurveyAssignmentStatus.Pending,
        CreatedAt = DateTimeOffset.UtcNow
    };
}
```

**DbContext Registration:** Entity properly configured in EF Core model.

---

## Fix Applied

### Test Modification (Rf1009NotificationInboxCharacterizationTests.cs:231-239)

**OLD (Incorrect):**
```csharp
var assignmentData = new SurveyAssignmentData
{
    SurveyPlanId = planId,
    RouteSegmentId = segmentId,
    RouteVersionId = routeVersionId,
    TargetBand = TargetBand.Surface,
    AssignedAt = assignedAt,
    OperatorUserId = null
};
context.SurveyAssignmentData.Add(assignmentData);  // ERROR: DbSet not found
```

**NEW (Correct):**
```csharp
var assignment = SurveyAssignment.Create(
    planId,
    segmentId,
    routeVersionId,
    TargetBand.Surface,
    assignedAt,
    null);
context.SurveyAssignments.Add(assignment);  // OK: Registered entity
await context.SaveChangesAsync();
```

---

## Why Factory Method Required

**1. Entity Initialization:**
- Factory ensures all required fields are set
- Generates new Guid for Id
- Sets default Status (Pending)
- Records CreatedAt timestamp

**2. Domain Invariants:**
- Factory enforces business rules at creation
- Prevents invalid entity states
- Centralizes initialization logic

**3. Entity Framework Tracking:**
- Only registered entities can be tracked
- DbSet<SurveyAssignment> exists in DbContext
- DbSet<SurveyAssignmentData> does not exist

---

## Test Scenario Achievement

**Goal:** Characterize notification outbox creation with correct correlation and event type.

**Setup Requirements:**
1. Create project with notification inbox enabled ✓
2. Create survey plan ✓
3. Create survey assignment ✓ (NOW FIXED)
4. Trigger outbox producer ✓

**Verification:**
- Outbox message created with correct correlation ✓
- Event type matches domain event ✓
- Notification links to correct entities ✓

**Test Result:** PASS after using proper entity factory.

---

## Entity Framework Core Patterns

**Anti-Pattern (Test Before Fix):**
```csharp
// Directly instantiate helper class
var helper = new SurveyAssignmentData { ... };
context.HelperClasses.Add(helper);  // DbSet doesn't exist
```

**Correct Pattern (Test After Fix):**
```csharp
// Use domain factory method
var entity = SurveyAssignment.Create(...);
context.SurveyAssignments.Add(entity);  // Tracked entity
await context.SaveChangesAsync();
```

**Best Practice:** Always use domain factories for entity creation in tests to match production patterns.

---

## Evidence

**Error Message (Before Fix):**
```
InvalidOperationException: The entity type 'SurveyAssignmentData' cannot be added because it was not found in the model. Ensure that the entity type has been added to the model.
```

**Source Files:**
- SurveyAssignment.cs - Domain entity with Create factory
- SurveyAssignmentData.cs - Helper class (not entity)
- ApplicationDbContext.cs - DbSet<SurveyAssignment> registration
- Rf1009NotificationInboxCharacterizationTests.cs:231-239 - Test fix

**Test Results:**
- Before fix: InvalidOperationException (entity not found)
- After fix: Test PASS (entity properly tracked)

**Status:** Entity properly mapped via factory, test achieves intended scenario.

---

**Timestamp:** 2026-10-01T15:45:00Z  
**Finding:** F-C13-03 RESOLVED  
**Resolution:** Replaced raw entity seeding with domain factory method  
**Impact:** Test now uses production-aligned entity creation pattern
