using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Labels;

namespace RoadGuardSystem.Services.Labels;

public interface ITrainingLabelService
{
    Task<TrainingLabelResult> CreateAsync(Guid actor, UserRoleCode role, Guid project,
        CreateTrainingLabelDto request, string key, Guid? correlation, CancellationToken token);
    Task<TrainingLabelResult> ReviseAsync(Guid actor, UserRoleCode role, Guid project, Guid label,
        ReviseTrainingLabelDto request, string key, string? ifMatch, Guid? correlation, CancellationToken token);
    Task<TrainingLabelResult> ReviewAsync(Guid actor, UserRoleCode role, Guid label,
        ReviewTrainingLabelDto request, string key, string? ifMatch, Guid? correlation, CancellationToken token);
    Task<TrainingLabelResult> ReadAsync(Guid actor, UserRoleCode role, Guid project,
        Guid label, CancellationToken token);
    Task<TrainingLabelPageResult> ListAsync(Guid actor, UserRoleCode role, Guid project,
        int pageSize, string? cursor, CancellationToken token);
}
