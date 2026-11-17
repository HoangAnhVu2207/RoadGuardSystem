using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Defects;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateDefectTypeDto(string Code, string Name, string? Description, [property: JsonRequired] bool IsActive, string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateDefectTypeDto(string Name, string? Description, [property: JsonRequired] bool IsActive, string Reason);
public sealed record DefectTypeAdminDto(string Code, string Name, string? Description, bool IsActive, string Version);