# Finding: Upload Initial Status

**Date:** 2026-10-01
**Finding ID:** F-C13-01a-RESOLVED
**Component:** BOX 1 - IdempotencyPerCommandCharacterizationTests.cs
**Tests affected:** 2

## Production Source Analysis

### Entity Creation
`RoadGuardSystem.BusinessObjects/Files/UploadSession.cs` line 63:
```csharp
Status = UploadSessionStatus.Pending
```

### Persistence Layer
`RoadGuardSystem.Repositories/Implementations/Files/UploadPersistenceService.cs` lines 390-397:
```csharp
private static UploadSessionPersistenceView ToView(UploadSession session, Guid projectId)
    => new(
        session.Id,
        session.FileId,
        session.OwnerUserId,
        projectId,
        session.ObjectKey,
        session.Status.ToString().ToUpperInvariant(),  // "PENDING"
        session.PartSizeBytes,
        session.ExpiresAt,
        Convert.ToBase64String(session.RowVersion));
```

### Service Layer
`RoadGuardSystem.Services/Implementations/Files/UploadService.cs` lines 223-224:
```csharp
private static UploadSessionResponseDto ToDto(UploadSessionPersistenceView view)
    => new(view.Id, view.FileId, view.Status, view.PartSizeBytes, view.ExpiresAt, view.Version);
```

## Conclusion

**Production behavior confirmed:** Upload initial status is **"PENDING"**, not "ACTIVE".

The test expectation `Status = "ACTIVE"` is **incorrect**.

## Correction Required

Tests must assert:
```csharp
Status = "PENDING"
```

This is the actual production behavior for newly created upload sessions.

## Affected Tests

1. `UploadCreate_SameKeySamePayload_ReplaysWithIdenticalOutcome`
2. `UploadCreate_StatusMutatedAfterCreate_ReplayReturnsOriginalStatus`

Both tests currently expect "ACTIVE" which does not match production implementation.
