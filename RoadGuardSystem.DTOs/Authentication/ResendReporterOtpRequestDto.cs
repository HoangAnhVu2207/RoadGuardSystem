using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Authentication;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ResendReporterOtpRequestDto
{
    public Guid IntentId { get; init; }
}
