# Finding: Actor Isolation in Idempotency

**Date:** 2026-10-01
**Finding ID:** F-C13-01b-ANALYZED
**Component:** BOX 1 - IdempotencyPerCommandCharacterizationTests.cs
**Test affected:** UploadCreate_DifferentActorsSameKey_BothExecute

## Production Source Analysis

### Idempotency Scope
`RoadGuardSystem.Repositories/Idempotency/IdempotencyOperationService.cs` lines 165-179:
```csharp
private Task<IdempotencyRecord?> FindExistingAsync(
    Guid? actorUserId,
    Guid? projectId,
    string operation,
    string idempotencyKey,
    CancellationToken cancellationToken)
    => _context.Set<IdempotencyRecord>()
        .AsNoTracking()
        .SingleOrDefaultAsync(
            record =>
                record.ActorUserId == actorUserId &&
                record.ProjectId == projectId &&
                record.Operation == operation &&
                record.IdempotencyKey == idempotencyKey,
            cancellationToken);
```

### Upload Creation
`RoadGuardSystem.Repositories/Implementations/Files/UploadPersistenceService.cs` lines 42-46:
```csharp
var outcome = await _idempotency.ExecuteAsync(
    request.ActorUserId,
    request.ProjectId,
    CreateOperation,
    request.IdempotencyKey,
    request.RequestFingerprint,
```

## Key Discovery

**Idempotency scope includes BOTH `actorUserId` AND `projectId`.**

This means:
- Different actors with same idempotency key in same project = **SEPARATE operations**
- Same actor with same key in same project = **REPLAY**

## Test Analysis

The test `UploadCreate_DifferentActorsSameKey_BothExecute` expects:
- manager1 creates upload with key "actor-isolation-key-001" → 201 Created ✓
- manager2 creates upload with SAME key → expects 201 Created
- Current result: 403 Forbidden ✗

## Issue

The 403 Forbidden suggests authorization failure, NOT idempotency replay.

**Hypothesis:** manager2 is not authorized to create uploads in the project owned by manager1.

## Authorization Logic

`RoadGuardSystem.Services/Implementations/Files/UploadService.cs` lines 51-54:
```csharp
if (!await InProjectScopeAsync(actorUserId, role, request.ProjectId!.Value, cancellationToken))
{
    return new(UploadServiceStatus.Forbidden);
}
```

`RoadGuardSystem.Services/Implementations/Authorization/ProjectScopeGuard.cs` lines 35-44:
```csharp
var membership = await _membershipRepository.FindByUserAndProjectAsync(
    userId,
    projectId,
    cancellationToken);
var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
return membership is not null && membership.Status == ProjectMemberStatus.Active &&
       membership.RoleCode == authoritativeRole &&
       membership.ValidFrom <= today && (membership.ValidTo is null || membership.ValidTo >= today)
    ? new ProjectAccessScope(projectId, authoritativeRole, membership.MembershipId)
    : null;
```

## Root Cause Confirmed

**Test creates project with manager1 as primaryProjectManagerUserId, but does NOT add manager2 to project membership.**

Line 438: `var projectId = await CreateProjectAsync(client, manager1.Id);`

This creates a project where:
- manager1 is the primary PM (automatically gets membership)
- manager2 has NO membership in the project

When manager2 tries to create upload:
1. UploadService.CreateAsync checks `InProjectScopeAsync(manager2.Id, ProjectManager, projectId)`
2. ProjectScopeGuard.AuthorizeAsync queries ProjectMembership for manager2 + projectId
3. No membership found → returns null
4. Service returns Forbidden (403)

## Verdict

**NOT a production defect. Test setup is incomplete.**

The test expects both actors to create separate uploads, which is correct per idempotency scope (actorUserId + projectId + operation + key). However, the test fails to establish valid authorization for actor2.

## Required Fix

Add manager2 to project membership before second upload attempt.
