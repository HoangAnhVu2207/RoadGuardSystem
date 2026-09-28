using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Authentication;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class VerifyReporterOtpRequestDto
{
    public Guid IntentId { get; init; }

    [Required, MinLength(1)]
    public string? Otp { get; init; }
}
