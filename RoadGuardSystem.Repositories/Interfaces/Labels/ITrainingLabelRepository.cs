using RoadGuardSystem.BusinessObjects.Labels;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Labels;

namespace RoadGuardSystem.Repositories.Labels;

public sealed record TrainingLabelIdentity(Guid Id, Guid ProjectId, Guid SourceId, string SourceKind);
public sealed record TrainingLabelApprovalFact(
    RoadGuardSystem.Repositories.Models.Huy01.HuyTrainingLabelHead Head,
    RoadGuardSystem.Repositories.Models.Huy01.HuyTrainingLabelRevision Revision,
    RoadGuardSystem.Repositories.Models.Huy01.HuyTrainingLabelReview Review,
    RoadGuardSystem.BusinessObjects.Files.StoredFile File);
public sealed record TrainingLabelFileSourceFact(string SourceKind, Guid SourceId, string SourceVersion,
    Guid FileId, string FileVersion, string? Checksum, long SizeBytes, string MimeType);

public interface ITrainingLabelRepository
{
    Task<TrainingLabelApprovalFact[]> ReadApprovedFactsAsync(Guid projectId, DateTimeOffset? fromInclusive,
        DateTimeOffset? toExclusive, Guid[] defectIds, CancellationToken token);
    Task<TrainingLabelFileSourceFact[]> ReadFileSourceFactsAsync(Guid projectId, Guid[] fileIds, CancellationToken token);
    Task<TrainingLabelIdentity?> IdentifyAsync(Guid labelId, CancellationToken token);
    Task<TrainingLabelViewFact?> CurrentAsync(Guid projectId, Guid labelId, CancellationToken token);
    Task<TrainingLabelViewFact?> ReadAsync(Guid projectId, Guid labelId,
        Func<CancellationToken, Task> guard, CancellationToken token);
    Task<TrainingLabelPageFact> ListAsync(Guid projectId, int pageSize, Guid? afterId,
        Func<CancellationToken, Task> guard, CancellationToken token);
    Task<TrainingLabelViewFact> CreateAsync(Guid actor, TrainingLabel label,
        string reason, Guid? correlation, CancellationToken token);
    Task<TrainingLabelViewFact> ReviseAsync(Guid actor, Guid projectId, Guid labelId,
        string expectedVersion, string sourceVersion, Guid fileId, string fileVersion,
        LabelAnnotationFact annotation, string defectTypeCode, string reason,
        Guid? correlation, CancellationToken token);
    Task<TrainingLabelViewFact> ReviewAsync(Guid actor, Guid projectId, Guid labelId,
        string expectedVersion, TrainingLabelReviewStatus decision, string reason,
        Guid? correlation, CancellationToken token);
}
