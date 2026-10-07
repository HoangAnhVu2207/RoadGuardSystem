using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Projects;

public interface IProjectLifecycleRepository
{
    Task<ProjectLifecycleFacts?> ReadAsync(Guid actor, Guid project, CancellationToken cancellationToken);
    Task<ProjectLifecycleWriteResult> RenewAsync(ProjectRenewedHandlingCommand command, CancellationToken cancellationToken);
    Task<ProjectLifecycleWriteResult> ExecuteAsync(LD06LifecycleCommand command, CancellationToken cancellationToken);
    Task ValidateRecurrenceAsync(Guid project, Guid predecessor, Guid decision, Guid[] evidence, CancellationToken cancellationToken);
    Task LinkRecurrenceAsync(Guid actor, Guid project, Guid newDefect, Guid predecessor, Guid decision, Guid[] evidence, string reason, CancellationToken cancellationToken);
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
    Guid[] OutstandingMandatoryObligationIds, string[] MissingReasons, ProjectLifecycleHistoryFact[] History, string Version,
    LD06ActionFact[]? Actions = null, ObligationResponsibilityFact[]? Responsibilities = null,
    LD06TransferableObligationFact[]? TransferableObligations = null);
public sealed record LD06TransferableObligationFact(Guid Id, Guid DefectId, string Kind, bool Mandatory, Guid ScopeId,
    Guid PhysicalRoadId, string LocationVersion, string RouteLabel, decimal From, decimal To, decimal OffsetFrom,
    decimal OffsetTo, string ScopeHash, string Version);
public sealed record LD06ActionFact(Guid Id, Guid ProjectId, string Kind, Guid ActorId, DateTimeOffset At, Guid? SourceActionId,
    Guid? DefectId, Guid? LinkedDefectId, Guid? ObligationId, Guid? ReceivingProjectId, Guid? PriorRepairDecisionId, string ScopeHash, string FactsJson);
public sealed record ObligationResponsibilityFact(Guid ObligationId, Guid OriginProjectId, Guid CurrentProjectId, Guid AcceptanceActionId);
public sealed record ProjectLifecycleWriteResult(int Status, string? Code = null, ProjectLifecycleFacts? Value = null);
public sealed record LD06LifecycleInput(string Reason, Guid[] EvidenceFileIds, Guid? SourceActionId = null,
    Guid? DefectId = null, Guid? LinkedDefectId = null, Guid? ObligationId = null, Guid? ReceivingProjectId = null,
    Guid? PriorRepairDecisionId = null, string? ScopeHash = null);
public sealed record LD06LifecycleCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId,
    RoadGuardSystem.BusinessObjects.Projects.LD06ActionKind Kind, LD06LifecycleInput Input, string Key, string ExpectedVersion);
