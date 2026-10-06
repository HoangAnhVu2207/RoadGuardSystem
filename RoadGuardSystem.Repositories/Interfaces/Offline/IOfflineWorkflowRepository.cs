using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Offline;

public sealed record OfflineWorkflowCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId, string Action,
    object? Input, Guid? ResourceId, string? Key, string? ExpectedVersion);
public sealed record OfflineWorkflowAlgorithms(Func<OfflineOperationData, string> EnvelopeHash,
    Func<OfflineOperationData, OfflineOperationDescriptorData> Describe,
    Func<Guid, Guid, Guid, IReadOnlyList<OfflineOperationDescriptorData>, byte[]> CanonicalManifest,
    Func<Guid, Guid, OfflineOperationData[], string, byte[]> CanonicalAttachedPayload,
    Func<OfflineOperationData[], IReadOnlyDictionary<Guid, string>, Guid[]> ReadyOrigins,
    Func<DateTimeOffset?, DateTimeOffset, string> SyncLateness);
public interface IOfflineWorkflowRepository
{
    Task<OfflineWorkflowFact> ExecuteAsync(OfflineWorkflowCommand command, OfflineWorkflowAlgorithms algorithms,
        Func<CancellationToken, Task<bool>> currentProjectGuard, CancellationToken cancellationToken);
}
