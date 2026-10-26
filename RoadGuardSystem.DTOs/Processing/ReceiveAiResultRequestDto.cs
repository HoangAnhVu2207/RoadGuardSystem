using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Processing;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ReceiveAiResultRequestDto(
    Guid JobAttemptId,
    [param: Required, RegularExpression("^[a-f0-9]{64}$")] string ManifestHash,
    Guid ModelVersionId,
    [param: Required, RegularExpression("^(MOCK|REAL)$")] string Mode,
    Guid RawResultFileId,
    [param: Required, RegularExpression("^[a-f0-9]{64}$")] string ChecksumSha256,
    [param: Required] IReadOnlyList<AiDetectionDto> Detections) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Detections is not null && Detections.Any(detection => detection is null))
            yield return new ValidationResult("Detections must not contain null elements.", [nameof(Detections)]);
    }
}
