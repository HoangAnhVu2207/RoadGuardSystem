using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Processing;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateValidationRunRequestDto(
    [param: Required, MinLength(1)] IReadOnlyList<ValidationPairDto> Pairs,
    Guid ModelVersionId,
    Guid DatasetSplitId,
    [param: Required, MinLength(1)] string MeasurementType,
    [param: Required, MinLength(1)] string Unit) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Pairs is not null && Pairs.Any(pair => pair is null))
            yield return new ValidationResult("Validation pairs must not contain null elements.", [nameof(Pairs)]);
    }
}
