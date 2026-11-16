namespace RoadGuardSystem.DTOs.Surveys;

public sealed record DatasetListItemDto(
    Guid Id,
    Guid SurveyTaskId,
    int VersionNo,
    DateTimeOffset? SubmittedAt,
    string IntegrityStatus,
    string Version,
    Guid? AssessmentId);
