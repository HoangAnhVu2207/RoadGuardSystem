namespace RoadGuardSystem.DTOs.Projects;

public sealed record ReassignPrimaryProjectManagerResponseDto(
    Guid PreviousMembershipId,
    Guid CurrentMembershipId,
    string CurrentMembershipRowVersion);
