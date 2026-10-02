using RoadGuardSystem.DTOs.Surveys;
using RoadGuardSystem.Repositories.Surveys;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Surveys;

public interface ISurveyAssessmentService
{
    Task<AssessmentOutcome> CreateAsync(Guid actor, UserRoleCode role, Guid dataset, CreateAssessmentDto request, string key, string version, Guid? correlation, CancellationToken token);
    Task<AssessmentOutcome> ReadAsync(Guid actor, UserRoleCode role, Guid dataset, Guid assessment, CancellationToken token);
    Task<AssessmentOutcome> SelectAsync(Guid actor, UserRoleCode role, Guid project, CreateBaselineDto request, string key, Guid? correlation, CancellationToken token);
    Task<(string Code, object? Value)> BaselineReadAsync(Guid actor, UserRoleCode role, Guid project, Guid? id, Guid? segmentSetId, CancellationToken token);
}
