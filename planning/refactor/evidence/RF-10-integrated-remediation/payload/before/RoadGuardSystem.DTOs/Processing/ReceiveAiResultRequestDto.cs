using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Processing;

public sealed record ReceiveAiResultRequestDto(
    Guid JobAttemptId,
    [param: Required, RegularExpression("^[a-f0-9]{64}$")] string ManifestHash,
    Guid ModelVersionId,
    [param: Required, RegularExpression("^(MOCK|REAL)$")] string Mode,
    Guid RawResultFileId,
    [param: Required, RegularExpression("^[a-f0-9]{64}$")] string ChecksumSha256,
    [param: Required] IReadOnlyList<AiDetectionDto> Detections);
