namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyAssignmentReassignmentFacts(
    Guid ProjectId,
    Guid ActiveAssignmentId);
