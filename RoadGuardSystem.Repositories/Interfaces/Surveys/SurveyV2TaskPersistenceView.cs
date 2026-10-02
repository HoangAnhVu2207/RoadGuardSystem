namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyV2TaskPersistenceView(Guid Id, Guid ProjectId, string ScopeJson, Guid OperatorId, string Status, string Version, DateTimeOffset? DueAt, string? AccessPointJson, Guid? SupplementTaskId = null)
{
    // Internal authorization evidence; management reads retain ended assignment history.
    public bool AssignmentIsActive { get; init; } = true;
    public bool AssignmentWasDeclined { get; init; }
}
