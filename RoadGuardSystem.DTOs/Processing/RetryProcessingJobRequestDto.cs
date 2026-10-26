using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Processing;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RetryProcessingJobRequestDto([param: Required, MinLength(1), MaxLength(1000)] string Reason);
