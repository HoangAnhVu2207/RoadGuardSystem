using Facts = RoadGuardSystem.BusinessObjects.PersistenceFacts.Web;
namespace RoadGuardSystem.DTOs.Web;

public sealed record ProjectRouteOverview(Guid RoadSectionId, Guid RouteVersionId, int VersionNo, int SourceSrid,
    Guid? CrsProfileRevisionId, double? LengthMeters)
{
    public static implicit operator ProjectRouteOverview(Facts.ProjectRouteOverview value) => new(value.RoadSectionId, value.RouteVersionId, value.VersionNo, value.SourceSrid, value.CrsProfileRevisionId, value.LengthMeters);
}
public sealed record WarrantyOverview(Guid Id, Guid? RoadSectionId, DateOnly HandoverDate, DateOnly StartDate,
    DateOnly EndDate, string Status)
{
    public static implicit operator WarrantyOverview(Facts.WarrantyOverview value) => new(value.Id, value.RoadSectionId, value.HandoverDate, value.StartDate, value.EndDate, value.Status);
}
/// <summary>Project-wide distinct defects: OPEN/VERIFIED count; RESOLVED is excluded, not an acceptance signal.
/// Unclassified statuses or unavailable source data produce an INCOMPLETE metric with a null value.</summary>
public sealed record OpenDefectMetric(string DefinitionVersion, string[] IncludedStatuses, long? Value,
    string Availability, long UnknownCount)
{
    public string[] ReasonCodes => UnknownCount > 0 ? ["LEGACY_DEFECT_STATUS_UNCLASSIFIED"] : Availability == "INCOMPLETE" ? ["DEFECT_SOURCE_UNAVAILABLE"] : [];
    public static implicit operator OpenDefectMetric(Facts.OpenDefectMetric value) => new(value.DefinitionVersion, value.IncludedStatuses, value.Value, value.Availability, value.UnknownCount);
}
public sealed record ProjectOverview(Guid ProjectId, string Name, string? Locality, int? EngineeringUtmSrid,
    ProjectRouteOverview[] Routes, double? TotalLengthMeters, string LengthAvailability,
    WarrantyOverview[] Warranties, OpenDefectMetric OpenDefects, string Version)
{
    public static implicit operator ProjectOverview(Facts.ProjectOverview value) => new(value.ProjectId, value.Name, value.Locality, value.EngineeringUtmSrid, value.Routes.Select(row => (ProjectRouteOverview)row).ToArray(), value.TotalLengthMeters, value.LengthAvailability, value.Warranties.Select(row => (WarrantyOverview)row).ToArray(), (OpenDefectMetric)value.OpenDefects, value.Version);
}
public sealed record ProjectContact(Guid MembershipId, Guid UserId, string DisplayName, string Role,
    string? Email, string? PhoneNumber, bool PhoneNumberConfirmed, string? Organization)
{
    public static implicit operator ProjectContact(Facts.ProjectContact value) => new(value.MembershipId, value.UserId, value.DisplayName, value.Role, value.Email, value.PhoneNumber, value.PhoneNumberConfirmed, value.Organization);
}
public sealed record OrganizationProfile(Guid UserId, string? Organization, string Version)
{
    public static implicit operator OrganizationProfile(Facts.OrganizationProfile value) => new(value.UserId, value.Organization, value.Version);
}
public sealed record TaskAssignee(Guid TaskId, string TaskKind, Guid AssignmentId, Guid UserId, string? DisplayName,
    string NameSource, DateTimeOffset AssignedAt, DateTimeOffset? EndedAt, bool Current)
{
    public static implicit operator TaskAssignee(Facts.TaskAssignee value) => new(value.TaskId, value.TaskKind, value.AssignmentId, value.UserId, value.DisplayName, value.NameSource, value.AssignedAt, value.EndedAt, value.Current);
}
public sealed record PolicyDiscovery(Guid Id, string Kind, string State, string DefectTypeCode, string ChecklistVersion,
    int? Revision, DateTimeOffset At, bool Applicable, string Version)
{
    public static implicit operator PolicyDiscovery(Facts.PolicyDiscovery value) => new(value.Id, value.Kind, value.State, value.DefectTypeCode, value.ChecklistVersion, value.Revision, value.At, value.Applicable, value.Version);
}
public sealed record PackagePlanning(Guid Id, string? Name, string? Description, DateTimeOffset? PlannedDueAt,
    Guid? CreatedBy, DateTimeOffset? CreatedAt, string Version)
{
    public static implicit operator PackagePlanning(Facts.PackagePlanning value) => new(value.Id, value.Name, value.Description, value.PlannedDueAt, value.CreatedBy, value.CreatedAt, value.Version);
}
public sealed record ItemPlanning(Guid Id, decimal? PlannedQuantity, string? PlannedUnit, string? PlannedMaterials, string Version)
{
    public static implicit operator ItemPlanning(Facts.ItemPlanning value) => new(value.Id, value.PlannedQuantity, value.PlannedUnit, value.PlannedMaterials, value.Version);
}
public sealed record WebMetadataRequest(string Action, Guid TargetId, Guid? ProjectId, Guid? PackageId,
    string? Text, string? Description, DateTimeOffset? DueAt, decimal? Quantity, string? Unit, string? Materials,
    string Reason, string Key, string ExpectedVersion);
public sealed record WebMetadataResult(int Status, string? Code = null, string? Version = null)
{
    public static implicit operator WebMetadataResult(Facts.WebMetadataResult value) => new(value.Status, value.Code, value.Version);
}
public sealed record DefectSourceReview(Guid DefectId, string DefectTypeCode, string Severity, string Status,
    string? GeometryWkt, int? Srid, string DataKind, string Version, DefectOriginReference[] Sources, string[] MissingSections, DefectSourceEvidence[]? Evidence = null)
{
    public static implicit operator DefectSourceReview(Facts.DefectSourceReview value) => new(value.DefectId, value.DefectTypeCode, value.Severity, value.Status, value.GeometryWkt, value.Srid, value.DataKind, value.Version, value.Sources.Select(row => (DefectOriginReference)row).ToArray(), value.MissingSections, value.Evidence?.Select(row => (DefectSourceEvidence)row).ToArray());
}
public sealed record DefectOriginReference(string Kind, Guid Id, Guid? CaseId = null)
{
    public static implicit operator DefectOriginReference(Facts.DefectOriginReference value) => new(value.Kind, value.Id, value.CaseId);
}
public sealed record DefectSourceEvidence(Guid EvidenceId, string ContentPath, string SourceKind, DateTimeOffset? CapturedAt)
{
    public static implicit operator DefectSourceEvidence(Facts.DefectSourceEvidence value) => new(value.EvidenceId, value.ContentPath, value.SourceKind, value.CapturedAt);
}
