using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Processing;

public sealed class MeasurementValidationSample
{
    private MeasurementValidationSample()
    {
    }

    public Guid Id { get; private set; }
    public Guid ValidationRunId { get; private set; }
    public Guid GroundTruthMeasurementId { get; private set; }
    public Guid DerivedMeasurementId { get; private set; }
    public decimal SignedError { get; private set; }
    public decimal AbsoluteError { get; private set; }
    public ValidationSampleInclusionStatus InclusionStatus { get; private set; }
    public string? ExclusionReason { get; private set; }

    public static MeasurementValidationSample Create(
        Guid id,
        Guid validationRunId,
        Guid groundTruthMeasurementId,
        Guid derivedMeasurementId,
        decimal signedError,
        ValidationSampleInclusionStatus inclusionStatus,
        string? exclusionReason)
    {
        if (id == Guid.Empty || validationRunId == Guid.Empty || groundTruthMeasurementId == Guid.Empty || derivedMeasurementId == Guid.Empty)
        {
            throw new ArgumentException("Validation sample identity is invalid.");
        }

        if (!Enum.IsDefined(inclusionStatus) || inclusionStatus == ValidationSampleInclusionStatus.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(inclusionStatus));
        }

        var reason = string.IsNullOrWhiteSpace(exclusionReason) ? null : exclusionReason.Trim();
        if (reason is { Length: > 500 } || inclusionStatus != ValidationSampleInclusionStatus.Included && reason is null)
        {
            throw new ArgumentException("Excluded or outlier samples require an exclusion reason.", nameof(exclusionReason));
        }

        return new MeasurementValidationSample
        {
            Id = id,
            ValidationRunId = validationRunId,
            GroundTruthMeasurementId = groundTruthMeasurementId,
            DerivedMeasurementId = derivedMeasurementId,
            SignedError = signedError,
            AbsoluteError = decimal.Abs(signedError),
            InclusionStatus = inclusionStatus,
            ExclusionReason = reason
        };
    }
}
