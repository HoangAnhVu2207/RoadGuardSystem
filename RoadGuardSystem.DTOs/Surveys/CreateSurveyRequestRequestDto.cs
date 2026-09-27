using System.ComponentModel.DataAnnotations;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.DTOs.Surveys;

public sealed record CreateSurveyRequestRequestDto(
    Guid? SurveyPlanId,
    Guid RoadSectionVersionId,
    SurveyType SurveyType,
    DateTimeOffset? DueAt,
    [Required] string OutputRequirements,
    Guid OperationId);
