namespace RoadGuardSystem.DTOs.Projects;

public sealed record ProjectLifecycleHistoryDto(Guid Id, string Kind, Guid ActorId, DateTimeOffset RecordedAtUtc,
    Guid? ObligationId, Guid? GrantId, Guid? ReceiverId, Guid? OperationalClosureId, string Reason, string BasisReference,
    string AuthoritySourceReference, string SourceDisposition, Guid? DefectId, Guid? LinkedDefectId, string? HandlingScope = null);
public sealed record ProjectLifecycleViewDto(Guid ProjectId, string ConstructionCompletion, string OperationalClosure,
    bool WarrantyRecorded, bool AcceptsNewReports, string ObligationInventory, string OperationalClosureEligibility,
    Guid[] OutstandingMandatoryObligationIds, string[] MissingReasons, ProjectLifecycleHistoryDto[] History, string Version,
    LD06ActionDto[]? Actions = null, ObligationResponsibilityDto[]? Responsibilities = null,
    LD06TransferableObligationDto[]? TransferableObligations = null);
public sealed record LD06TransferableObligationDto(Guid Id, Guid DefectId, string Kind, bool Mandatory, Guid ScopeId,
    Guid PhysicalRoadId, string LocationVersion, string RouteLabel, decimal From, decimal To, decimal OffsetFrom,
    decimal OffsetTo, string ScopeHash, string Version);
public sealed record LD06ActionDto(Guid Id, Guid ProjectId, string Kind, Guid ActorId, DateTimeOffset At, Guid? SourceActionId,
    Guid? DefectId, Guid? LinkedDefectId, Guid? ObligationId, Guid? ReceivingProjectId, Guid? PriorRepairDecisionId, string ScopeHash, string FactsJson);
public sealed record ObligationResponsibilityDto(Guid ObligationId, Guid OriginProjectId, Guid CurrentProjectId, Guid AcceptanceActionId);
[System.Text.Json.Serialization.JsonUnmappedMemberHandling(System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow)]
public sealed record RenewedHandlingScopeInput(Guid OperationalClosureId, string Reason, string HandlingScope, string Basis);
public sealed record ProjectLifecycleMutationResult(int Status, string? Code = null, ProjectLifecycleViewDto? Value = null);
[System.Text.Json.Serialization.JsonUnmappedMemberHandling(System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow)]
public sealed record LD06LifecycleInputDto(string Reason, Guid[] EvidenceFileIds, Guid? SourceActionId = null,
    Guid? DefectId = null, Guid? LinkedDefectId = null, Guid? ObligationId = null, Guid? ReceivingProjectId = null,
    Guid? PriorRepairDecisionId = null, string? ScopeHash = null,
    RoadGuardSystem.DTOs.Defects.CandidateDecisionRequestDto? NewDefect = null);
