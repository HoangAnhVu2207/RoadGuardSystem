using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Processing;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AiDetectionDto(
    [param: Required] string DetectionId,
    Guid FrameFileId,
    int TimestampMs,
    [param: Required, MinLength(1)] string TypeCode,
    decimal Confidence,
    [param: Required, MinLength(4), MaxLength(4)] IReadOnlyList<decimal> Bbox);
