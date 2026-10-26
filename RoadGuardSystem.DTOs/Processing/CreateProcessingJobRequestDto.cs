using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using RoadGuardSystem.DTOs.Surveys;

namespace RoadGuardSystem.DTOs.Processing;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateProcessingJobRequestDto(
    Guid DatasetId,
    BandScopeDto Scope,
    Guid ModelVersionId,
    [param: Required, MinLength(1)] string PreprocessingVersion,
    [param: Required, MinLength(1)] string ConfigVersion,
    [param: Required, RegularExpression("^(MOCK|REAL)$")] string Mode);
