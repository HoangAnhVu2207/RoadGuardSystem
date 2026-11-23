using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Repairs;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record TemporarySafetyObligationInput(RepairActualScopeInput Scope, [property: JsonRequired] bool Mandatory, string Reason);
public sealed record SafetyObligationScopeView(Guid PhysicalRoadId, string LocationVersion, string RouteLabel,
    decimal FromMeters, decimal ToMeters, decimal OffsetFromMeters, decimal OffsetToMeters);
public sealed record TemporarySafetyObligationView(Guid SafetyObligationId, Guid FormalObligationId,
    Guid PackageId, Guid ItemId, Guid DefectId, string Kind, bool Mandatory, SafetyObligationScopeView Scope,
    bool Resolved, Guid OriginProjectId, Guid ResponsibilityProjectId, string Version);
public sealed record TemporarySafetyObligationSourcesView(Guid PackageId, Guid ItemId, Guid DefectId,
    Guid FormalObligationId, Guid ResponsibilityProjectId, string PackageVersion, string ItemVersion,
    SafetyObligationScopeView FormalScope, RepairTaskBindingView? Binding,
    TemporarySafetyObligationView[] Items);
