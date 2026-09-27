using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Authentication;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class PasswordRecoveryRequestDto
{
    [Required]
    [EmailAddress]
    [StringLength(254, MinimumLength = 3)]
    public string? Email { get; init; }
}
