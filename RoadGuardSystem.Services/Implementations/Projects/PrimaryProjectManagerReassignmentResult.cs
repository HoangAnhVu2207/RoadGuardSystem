namespace RoadGuardSystem.Services.Projects;

public sealed record PrimaryProjectManagerReassignmentResult(
    PrimaryProjectManagerReassignmentStatus Status,
    PrimaryProjectManagerReassignmentView? Assignment = null);
