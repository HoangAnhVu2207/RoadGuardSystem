using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Repairs;

public sealed record TemporarySafetyObligationCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId,
    Guid PackageId, Guid ItemId, RepairActualScopeData Scope, bool Mandatory, string Reason, string Key, string ExpectedPackageVersion);
public sealed record SafetyObligationScopeFact(Guid PhysicalRoadId, string LocationVersion, string RouteLabel,
    decimal FromMeters, decimal ToMeters, decimal OffsetFromMeters, decimal OffsetToMeters);
public sealed record TemporarySafetyObligationFact(Guid SafetyObligationId, Guid FormalObligationId,
    Guid PackageId, Guid ItemId, Guid DefectId, string Kind, bool Mandatory, SafetyObligationScopeFact Scope,
    bool Resolved, Guid OriginProjectId, Guid ResponsibilityProjectId, string Version);
public sealed record TemporarySafetyObligationSourcesFact(Guid PackageId, Guid ItemId, Guid DefectId,
    Guid FormalObligationId, Guid ResponsibilityProjectId, string PackageVersion, string ItemVersion,
    SafetyObligationScopeFact FormalScope, RepairTaskBindingFact? Binding, TemporarySafetyObligationFact[] Items);
