using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Identity;

public sealed class PhoneNumberUpdateRequestDto
{
    [Required]
    public string? PhoneNumber { get; init; }
}

public sealed record SelfPhoneNumberResponseDto(string? PhoneNumber, bool PhoneNumberConfirmed, string Version);
