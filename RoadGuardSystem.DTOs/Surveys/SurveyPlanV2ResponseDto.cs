using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Surveys;

public sealed record SurveyPlanV2ResponseDto(
    Guid Id,
    Guid ProjectId,
    IReadOnlyList<BandScopeDto> Scope,
    DateTimeOffset PlannedAt,
    string Status,
    string Version, string SurveyType = "UNKNOWN", SurveyPlanCompletionView? Completion = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? WarrantyId = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? OccurrenceCount = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTimeOffset? FirstPlannedAt = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTimeOffset? LastPlannedAt = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<SurveyPlanOccurrenceDto>? Occurrences = null);

public sealed record SurveyPlanOccurrenceDto(
    Guid Id, DateTimeOffset PlannedStartAt, DateTimeOffset PlannedEndAt, string Status, Guid? SurveyPlanId);
