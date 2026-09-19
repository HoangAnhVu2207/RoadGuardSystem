using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Authentication;

public sealed class ForcedPasswordChangeRequestDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string? Username { get; init; }

    [Required]
    [StringLength(1024, MinimumLength = 1)]
    public string? CurrentPassword { get; init; }

    [Required]
    [StringLength(1024, MinimumLength = 1)]
    public string? NewPassword { get; init; }

    [Required]
    [Compare(nameof(NewPassword))]
    public string? ConfirmPassword { get; init; }

    public Guid OperationId { get; init; }
}
