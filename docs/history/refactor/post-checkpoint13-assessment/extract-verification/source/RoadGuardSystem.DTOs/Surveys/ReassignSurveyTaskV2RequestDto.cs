using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Surveys;

public sealed record ReassignSurveyTaskV2RequestDto(
    Guid OperatorId,
    [Required, MinLength(1)] string Reason,
    DateTimeOffset? DueAt = null);
