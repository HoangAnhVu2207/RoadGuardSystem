namespace RoadGuardSystem.DTOs.Projects;

public sealed record ProjectLifecycleHistoryDto(Guid Id,string Kind,Guid ActorId,DateTimeOffset RecordedAtUtc,
    Guid? ObligationId,Guid? GrantId,Guid? ReceiverId,Guid? OperationalClosureId,string Reason,string BasisReference,
    string AuthoritySourceReference,string SourceDisposition,Guid? DefectId,Guid? LinkedDefectId,string? HandlingScope=null);
public sealed record ProjectLifecycleViewDto(Guid ProjectId,string ConstructionCompletion,string OperationalClosure,
    bool WarrantyRecorded,bool AcceptsNewReports,string ObligationInventory,string OperationalClosureEligibility,
    Guid[] OutstandingMandatoryObligationIds,string[] MissingReasons,ProjectLifecycleHistoryDto[] History,string Version);
[System.Text.Json.Serialization.JsonUnmappedMemberHandling(System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow)]
public sealed record RenewedHandlingScopeInput(Guid OperationalClosureId,string Reason,string HandlingScope,string Basis);
public sealed record ProjectLifecycleMutationResult(int Status,string? Code=null,ProjectLifecycleViewDto? Value=null);
