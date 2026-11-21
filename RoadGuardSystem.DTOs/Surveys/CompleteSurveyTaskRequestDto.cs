using System.Text.Json.Serialization;
namespace RoadGuardSystem.DTOs.Surveys;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CompleteSurveyTaskRequestDto(Guid DatasetId, Guid AssessmentId, string Reason);
