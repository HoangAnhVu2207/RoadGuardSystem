using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Repairs;

namespace RoadGuardSystem.Repositories.Repairs;

public sealed record RoadCoverageCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId,
    RoadCoverageInputFact Input, string Key, string ExpectedVersion);
public sealed record HandoverAttachmentCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId, Guid FileId,
    Guid? HandoverDocumentId, string? DocumentNo, DateOnly? HandoverDate, string Key, string ExpectedVersion);
public sealed record RoadCoverageResult(int Status, string? Code = null, RoadCoverageReadFact? Value = null);
public sealed record HandoverDocumentsResult(int Status, string? Code = null,
    RoadGuardSystem.BusinessObjects.Projects.HandoverDocumentsReadFact? Value = null);
public interface IRoadCoverageRepository
{
    Task<RoadCoverageResult> ReadAsync(Guid actor, UserRoleCode role, Guid project, CancellationToken token);
    Task<HandoverDocumentsResult> ReadHandoverDocumentsAsync(Guid actor, UserRoleCode role, Guid project, CancellationToken token);
    Task<RoadCoverageResult> AttachHandoverAsync(HandoverAttachmentCommand command, CancellationToken token);
    Task<RoadCoverageResult> ConfirmAsync(RoadCoverageCommand command, CancellationToken token);
}
