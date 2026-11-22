using System.Text.Json.Serialization;
namespace RoadGuardSystem.DTOs.Surveys;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SurveyPlanCompletionSource(Guid TaskId, Guid DatasetId, Guid AssessmentId);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CompleteSurveyPlanRequestDto(IReadOnlyList<SurveyPlanCompletionSource> Sources, string Reason);
public sealed record SurveyPlanCompletionView(Guid Id, DateTimeOffset At, Guid ActorId, string Reason, IReadOnlyList<SurveyPlanCompletionSource> Sources);
