using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Authentication;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class LoginRequestDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    [EmailAddress]
    public string? Email { get; init; }

    [Required]
    [StringLength(1024, MinimumLength = 1)]
    public string? Password { get; init; }
}
