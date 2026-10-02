namespace RoadGuardSystem.Services.Projects;

public sealed record ReassignPrimaryProjectManagerCommand(
    Guid PrimaryProjectManagerUserId,
    DateOnly EffectiveFrom,
    string Reason,
    string ExpectedCurrentMembershipRowVersion,
    Guid OperationId,
    Guid? CorrelationId);
