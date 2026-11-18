namespace RoadGuardSystem.BusinessObjects.PersistenceFacts.Web;

public sealed record ProjectRouteOverview(Guid RoadSectionId, Guid RouteVersionId, int VersionNo, int SourceSrid,
    Guid? CrsProfileRevisionId, double? LengthMeters);
public sealed record WarrantyOverview(Guid Id, Guid? RoadSectionId, DateOnly HandoverDate, DateOnly StartDate,
    DateOnly EndDate, string Status);
public sealed record OpenDefectMetric(string DefinitionVersion, string[] IncludedStatuses, long? Value,
    string Availability, long UnknownCount);
public sealed record ProjectOverview(Guid ProjectId, string Name, string? Locality, int? EngineeringUtmSrid,
    ProjectRouteOverview[] Routes, double? TotalLengthMeters, string LengthAvailability,
    WarrantyOverview[] Warranties, OpenDefectMetric OpenDefects, string Version);
public sealed record ProjectContact(Guid MembershipId, Guid UserId, string DisplayName, string Role,
    string? Email, string? PhoneNumber, bool PhoneNumberConfirmed, string? Organization);
public sealed record OrganizationProfile(Guid UserId, string? Organization, string Version);
public sealed record TaskAssignee(Guid TaskId, string TaskKind, Guid AssignmentId, Guid UserId, string? DisplayName,
    string NameSource, DateTimeOffset AssignedAt, DateTimeOffset? EndedAt, bool Current);
public sealed record PolicyDiscovery(Guid Id, string Kind, string State, string DefectTypeCode, string ChecklistVersion,
    int? Revision, DateTimeOffset At, bool Applicable, string Version);
public sealed record PackagePlanning(Guid Id, string? Name, string? Description, DateTimeOffset? PlannedDueAt,
    Guid? CreatedBy, DateTimeOffset? CreatedAt, string Version);
public sealed record ItemPlanning(Guid Id, decimal? PlannedQuantity, string? PlannedUnit, string? PlannedMaterials, string Version);
public sealed record WebMetadataCommand(string Action, Guid TargetId, Guid? ProjectId, Guid? PackageId,
    string? Text, string? Description, DateTimeOffset? DueAt, decimal? Quantity, string? Unit, string? Materials,
    string Reason, string Key, string ExpectedVersion);
public sealed record WebMetadataResult(int Status, string? Code = null, string? Version = null);
public sealed record DefectSourceReview(Guid DefectId, string DefectTypeCode, string Severity, string Status,
    string? GeometryWkt, int? Srid, string DataKind, string Version, DefectOriginReference[] Sources, string[] MissingSections, DefectSourceEvidence[]? Evidence = null);
public sealed record DefectOriginReference(string Kind, Guid Id, Guid? CaseId = null);
public sealed record DefectSourceEvidence(Guid EvidenceId, string ContentPath, string SourceKind, DateTimeOffset? CapturedAt);
