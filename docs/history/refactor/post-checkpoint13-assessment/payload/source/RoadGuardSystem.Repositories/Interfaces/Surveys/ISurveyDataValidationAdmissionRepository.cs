using RoadGuardSystem.Repositories.Surveys;

namespace RoadGuardSystem.Repositories.Interfaces.Surveys;

public interface ISurveyDataValidationAdmissionRepository
{
    Task<SurveyDataValidationAdmissionResult> AdmitAsync(
        SurveyDataValidationAdmissionRequest request,
        CancellationToken cancellationToken = default);
}
