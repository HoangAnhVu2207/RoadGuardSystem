using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Processing;

public sealed class DerivedMeasurement
{
    private static readonly HashSet<string> SupportedUnits = ["mm", "cm", "m"];

    private DerivedMeasurement()
    {
    }

    public Guid Id { get; private set; }
    public Guid SurveyDataVersionId { get; private set; }
    public Guid RoadSectionVersionId { get; private set; }
    public string SampleId { get; private set; } = string.Empty;
    public MeasurementType MeasurementType { get; private set; }
    public decimal Value { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public decimal? UncertaintyEstimate { get; private set; }
    public DerivedMeasurementSourceType SourceType { get; private set; }
    public string? AlgorithmVersion { get; private set; }
    public DateTimeOffset ComputedAt { get; private set; }
    public DerivedMeasurementStatus Status { get; private set; }

    public static DerivedMeasurement Create(
        Guid id,
        Guid surveyDataVersionId,
        Guid roadSectionVersionId,
        string sampleId,
        MeasurementType measurementType,
        decimal value,
        string unit,
        decimal? uncertaintyEstimate,
        DerivedMeasurementSourceType sourceType,
        string? algorithmVersion,
        DateTimeOffset computedAt,
        DerivedMeasurementStatus status)
    {
        if (id == Guid.Empty || surveyDataVersionId == Guid.Empty || roadSectionVersionId == Guid.Empty)
        {
            throw new ArgumentException("Derived measurement identity is invalid.");
        }

        if (!Enum.IsDefined(measurementType) || measurementType == MeasurementType.Unknown ||
            !Enum.IsDefined(sourceType) || sourceType == DerivedMeasurementSourceType.Unknown ||
            !Enum.IsDefined(status) || status == DerivedMeasurementStatus.Unknown ||
            value < 0 || uncertaintyEstimate < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        var normalizedUnit = Required(unit, nameof(unit), 20).ToLowerInvariant();
        if (!SupportedUnits.Contains(normalizedUnit))
        {
            throw new ArgumentException("Measurement unit must be mm, cm, or m.", nameof(unit));
        }

        return new DerivedMeasurement
        {
            Id = id,
            SurveyDataVersionId = surveyDataVersionId,
            RoadSectionVersionId = roadSectionVersionId,
            SampleId = Required(sampleId, nameof(sampleId), 100),
            MeasurementType = measurementType,
            Value = value,
            Unit = normalizedUnit,
            UncertaintyEstimate = uncertaintyEstimate,
            SourceType = sourceType,
            AlgorithmVersion = Optional(algorithmVersion, nameof(algorithmVersion), 100),
            ComputedAt = computedAt.ToUniversalTime(),
            Status = status
        };
    }

    private static string Required(string value, string parameterName, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length > maxLength) throw new ArgumentOutOfRangeException(parameterName);
        return normalized;
    }

    private static string? Optional(string? value, string parameterName, int maxLength)
        => string.IsNullOrWhiteSpace(value) ? null : Required(value, parameterName, maxLength);
}
