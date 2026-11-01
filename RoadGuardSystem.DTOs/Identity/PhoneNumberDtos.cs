using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Identity;

public sealed class PhoneNumberUpdateRequestDto
{
    [Required]
    public string? PhoneNumber { get; init; }
}
