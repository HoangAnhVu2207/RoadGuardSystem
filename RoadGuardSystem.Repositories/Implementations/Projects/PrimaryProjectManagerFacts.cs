namespace RoadGuardSystem.Repositories.Projects;

public sealed record PrimaryProjectManagerFacts(
    Guid ProjectId,
    bool ProjectExists,
    bool ProjectIsClosed,
    bool ReplacementUserIsActiveProjectManager,
    Guid? CurrentMembershipId,
    Guid? CurrentProjectManagerUserId,
    DateOnly? CurrentValidFrom,
    byte[]? CurrentRowVersion);
