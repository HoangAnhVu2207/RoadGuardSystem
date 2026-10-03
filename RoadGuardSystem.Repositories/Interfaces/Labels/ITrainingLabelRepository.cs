using RoadGuardSystem.BusinessObjects.Labels;
using RoadGuardSystem.DTOs.Labels;

namespace RoadGuardSystem.Repositories.Labels;

public sealed record TrainingLabelIdentity(Guid Id, Guid ProjectId, Guid SourceId, string SourceKind);

public interface ITrainingLabelRepository
{
    Task<TrainingLabelIdentity?> IdentifyAsync(Guid labelId, CancellationToken token);
    Task<TrainingLabelViewDto?> CurrentAsync(Guid projectId, Guid labelId, CancellationToken token);
    Task<TrainingLabelViewDto?> ReadAsync(Guid projectId, Guid labelId,
        Func<CancellationToken, Task> guard, CancellationToken token);
    Task<TrainingLabelPageDto> ListAsync(Guid projectId, int pageSize, Guid? afterId,
        Func<CancellationToken, Task> guard, CancellationToken token);
    Task<TrainingLabelViewDto> CreateAsync(Guid actor, TrainingLabel label,
        string reason, Guid? correlation, CancellationToken token);
    Task<TrainingLabelViewDto> ReviseAsync(Guid actor, Guid projectId, Guid labelId,
        string expectedVersion, string sourceVersion, Guid fileId, string fileVersion,
        LabelAnnotationDto annotation, string defectTypeCode, string reason,
        Guid? correlation, CancellationToken token);
    Task<TrainingLabelViewDto> ReviewAsync(Guid actor, Guid projectId, Guid labelId,
        string expectedVersion, TrainingLabelReviewStatus decision, string reason,
        Guid? correlation, CancellationToken token);
}
