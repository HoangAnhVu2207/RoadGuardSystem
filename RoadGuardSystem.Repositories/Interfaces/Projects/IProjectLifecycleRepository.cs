using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Projects;

public interface IProjectLifecycleRepository
{
    Task<ProjectLifecycleFacts?> ReadAsync(Guid actor, Guid project, CancellationToken cancellationToken);
    Task<ProjectLifecycleWriteResult> RenewAsync(ProjectRenewedHandlingCommand command, CancellationToken cancellationToken);
}

public sealed record ProjectRenewedHandlingCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId,
    ProjectRenewedHandlingInput Input, string Key, string ExpectedProjectionVersion);

// Property order is retained for persisted receipt fingerprints and projection versions.
public sealed record ProjectRenewedHandlingInput(Guid OperationalClosureId, string Reason, string HandlingScope, string Basis);
public sealed record ProjectLifecycleHistoryFact(Guid Id, string Kind, Guid ActorId, DateTimeOffset RecordedAtUtc,
    Guid? ObligationId, Guid? GrantId, Guid? ReceiverId, Guid? OperationalClosureId, string Reason, string BasisReference,
    string AuthoritySourceReference, string SourceDisposition, Guid? DefectId, Guid? LinkedDefectId, string? HandlingScope = null);
public sealed record ProjectLifecycleFacts(Guid ProjectId, string ConstructionCompletion, string OperationalClosure,
    bool WarrantyRecorded, bool AcceptsNewReports, string ObligationInventory, string OperationalClosureEligibility,
    Guid[] OutstandingMandatoryObligationIds, string[] MissingReasons, ProjectLifecycleHistoryFact[] History, string Version);
public sealed record ProjectLifecycleWriteResult(int Status, string? Code = null, ProjectLifecycleFacts? Value = null);
