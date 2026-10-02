using System.ComponentModel.DataAnnotations;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.DTOs.Surveys;

public sealed record CreateSurveyPlanRequestDto(
    DateTimeOffset PlannedStartAt,
    DateTimeOffset PlannedEndAt,
    Guid RoadSectionVersionId,
    SurveyType SurveyType,
    [Required] string OutputRequirements,
    Guid OperationId);
