using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Authentication;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class RefreshRequestDto
{
    [Required]
    [StringLength(2048, MinimumLength = 16)]
    public string? RefreshToken { get; init; }
}
