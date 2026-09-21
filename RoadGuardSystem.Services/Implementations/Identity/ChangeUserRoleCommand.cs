using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Identity;

public sealed record ChangeUserRoleCommand(
    UserRoleCode NewRoleCode,
    string? ExpectedRowVersion,
    Guid OperationId,
    Guid? CorrelationId,
    string? Reason);
