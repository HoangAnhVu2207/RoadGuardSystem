using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Identity;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class AccountUpdateRequestDto
{
    [Required, MinLength(1)]
    public string? Status { get; init; }

    [Required, MinLength(1)]
    public string? Role { get; init; }

    [Required, MinLength(1)]
    public string? Reason { get; init; }
}
