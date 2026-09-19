using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Authentication;

public sealed class RefreshRequestDto
{
    [Required]
    [StringLength(2048, MinimumLength = 16)]
    public string? RefreshToken { get; init; }
}
