namespace RoadGuardSystem.Services.Projects;

public sealed record PrimaryProjectManagerReassignmentView(
    Guid PreviousMembershipId,
    Guid CurrentMembershipId,
    string CurrentMembershipRowVersion);
