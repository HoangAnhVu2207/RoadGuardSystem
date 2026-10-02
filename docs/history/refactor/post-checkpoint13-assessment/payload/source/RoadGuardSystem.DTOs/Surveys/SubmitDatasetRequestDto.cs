using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Surveys;

public sealed record SubmitDatasetRequestDto(
    [param: Required, MinLength(1)] IReadOnlyList<Guid> VideoFileIds,
    [param: Required] IReadOnlyList<Guid> TelemetryFileIds,
    [param: Required] DateTimeOffset RecordedAt,
    Guid? DeviceId,
    [param: Required, MinLength(1)] IReadOnlyList<BandScopeDto> Scope);
