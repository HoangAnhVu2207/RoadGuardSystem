using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Authentication;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class RegisterReporterRequestDto
{
    [Required, EmailAddress]
    public string? Email { get; init; }

    [Required, MinLength(1)]
    public string? Password { get; init; }

    [Required, MinLength(1)]
    public string? DisplayName { get; init; }

    [Required, MinLength(1)]
    public string? ReporterType { get; init; }
}
