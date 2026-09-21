namespace RoadGuardSystem.Repositories.Projects;

public sealed record PrimaryProjectManagerWriteResult(
    PrimaryProjectManagerWriteStatus Status,
    Guid? PreviousMembershipId = null,
    Guid? CurrentMembershipId = null,
    byte[]? CurrentRowVersion = null);
