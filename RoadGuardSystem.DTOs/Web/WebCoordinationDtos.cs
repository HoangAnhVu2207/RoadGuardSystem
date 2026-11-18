using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Web;

public sealed record WebMetadataVersion(string Version);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ProjectLocalityInput(string? Locality, string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record OrganizationInput(string? Organization, string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PackagePlanningInput(string? Name, string? Description, DateTimeOffset? PlannedDueAt, string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ItemPlanningInput(decimal? PlannedQuantity, string? PlannedUnit, string? PlannedMaterials, string Reason);
public sealed record WebPage<T>(IReadOnlyList<T> Items, string? NextCursor);
