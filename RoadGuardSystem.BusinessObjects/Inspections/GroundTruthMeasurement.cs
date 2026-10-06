using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Inspections;

public sealed class GroundTruthMeasurement
{
    private static readonly HashSet<string> SupportedUnits = ["mm", "cm", "m"];

    private GroundTruthMeasurement()
    {
    }

    public Guid Id { get; private set; }

    public Guid FieldInspectionSessionId { get; private set; }

    public string SampleId { get; private set; } = string.Empty;

    public Guid RoadSectionVersionId { get; private set; }

    public Guid? SurveyId { get; private set; }

    public Guid? DefectId { get; private set; }

    public MeasurementType MeasurementType { get; private set; }

    public decimal? Value { get; private set; }
    public string ValueState { get; private set; } = "KNOWN";
    public string Dimension { get; private set; } = "LENGTH";
    public string? UnknownReason { get; private set; }
    public string LocationState { get; private set; } = "CAPTURED";
    public string? LocationReason { get; private set; }

    public string Unit { get; private set; } = string.Empty;

    public Point? Location { get; private set; }

    public string InstrumentName { get; private set; } = string.Empty;

    public string? InstrumentReference { get; private set; }

    public string MeasurementMethod { get; private set; } = string.Empty;

    public string MeasuredBy { get; private set; } = string.Empty;

    public DateTimeOffset MeasuredAt { get; private set; }

    public Guid? EvidenceFileId { get; private set; }

    public string? Notes { get; private set; }

    public static GroundTruthMeasurement Create(
        Guid id,
        Guid fieldInspectionSessionId,
        string sampleId,
        Guid roadSectionVersionId,
        Guid? surveyId,
        Guid? defectId,
        MeasurementType measurementType,
        decimal value,
        string unit,
        Point location,
        string instrumentName,
        string? instrumentReference,
        string measurementMethod,
        string measuredBy,
        DateTimeOffset measuredAt,
        Guid? evidenceFileId,
        string? notes)
    {
        if (id == Guid.Empty || fieldInspectionSessionId == Guid.Empty || roadSectionVersionId == Guid.Empty)
        {
            throw new ArgumentException("Measurement, session, and road version ids must not be empty.");
        }

        if (surveyId == Guid.Empty || defectId == Guid.Empty || evidenceFileId == Guid.Empty)
        {
            throw new ArgumentException("Optional measurement ids must be non-empty when supplied.");
        }

        if (measurementType is not (MeasurementType.DepressionDepth or MeasurementType.SlabFaultingHeight or MeasurementType.ShoulderErosionExtent))
        {
            throw new ArgumentOutOfRangeException(nameof(measurementType), "Measurement type must be specified.");
        }

        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Measurement value must not be negative.");
        }

        ArgumentNullException.ThrowIfNull(location);
        if (location.SRID != 4326 || location.IsEmpty)
        {
            throw new ArgumentException("Measurement location must be a non-empty geography point with SRID 4326.", nameof(location));
        }

        var normalizedUnit = NormalizeRequired(unit, nameof(unit), 20).ToLowerInvariant();
        if (!SupportedUnits.Contains(normalizedUnit))
        {
            throw new ArgumentException("Measurement unit must be mm, cm, or m.", nameof(unit));
        }

        var normalizedNotes = NormalizeOptional(notes, nameof(notes), 4_000);
        if (evidenceFileId is null && normalizedNotes is null)
        {
            throw new ArgumentException("A measurement without evidence requires a note explaining why.", nameof(notes));
        }

        return new GroundTruthMeasurement
        {
            Id = id,
            FieldInspectionSessionId = fieldInspectionSessionId,
            SampleId = NormalizeRequired(sampleId, nameof(sampleId), 100),
            RoadSectionVersionId = roadSectionVersionId,
            SurveyId = surveyId,
            DefectId = defectId,
            MeasurementType = measurementType,
            Value = value,
            Unit = normalizedUnit,
            Location = location,
            InstrumentName = NormalizeRequired(instrumentName, nameof(instrumentName), 150),
            InstrumentReference = NormalizeOptional(instrumentReference, nameof(instrumentReference), 150),
            MeasurementMethod = NormalizeRequired(measurementMethod, nameof(measurementMethod), 500),
            MeasuredBy = NormalizeRequired(measuredBy, nameof(measuredBy), 200),
            MeasuredAt = measuredAt.ToUniversalTime(),
            EvidenceFileId = evidenceFileId,
            Notes = normalizedNotes
        };
    }

    public static GroundTruthMeasurement CreateCaptured(Guid id, Guid sessionId, string sampleId,
        Guid routeId, Guid? surveyId, Guid defectId, MeasurementType type, decimal? value, string valueState,
        string? unknownReason, string dimension, string unit, Point? location, string? locationReason,
        string instrument, string method, string measuredBy, DateTimeOffset measuredAt, Guid? evidenceId, string? notes)
    {
        if (id == Guid.Empty || sessionId == Guid.Empty || routeId == Guid.Empty || defectId == Guid.Empty ||
            surveyId == Guid.Empty || evidenceId == Guid.Empty || measuredAt == default || !Enum.IsDefined(type) || type == MeasurementType.Unknown)
            throw new ArgumentException("Captured measurement scope and type are required.");
        if (valueState is not ("KNOWN" or "UNKNOWN") || (valueState == "KNOWN") != value.HasValue || value < 0 ||
            valueState == "UNKNOWN" && string.IsNullOrWhiteSpace(unknownReason))
            throw new ArgumentException("UNKNOWN must have a null value and a reason; zero is KNOWN.");
        var normalizedUnit = NormalizeRequired(unit, nameof(unit), 20).ToLowerInvariant();
        if (type == MeasurementType.Area ? dimension != "AREA" || normalizedUnit != "m²" :
            dimension != "LENGTH" || !SupportedUnits.Contains(normalizedUnit))
            throw new ArgumentException("Measurement type, dimension and unit disagree.");
        if (location is not null && (location.SRID != 4326 || location.IsEmpty || !double.IsFinite(location.X) || !double.IsFinite(location.Y) || location.X is < -180 or > 180 || location.Y is < -90 or > 90) ||
            location is null && string.IsNullOrWhiteSpace(locationReason))
            throw new ArgumentException("Unknown location requires a reason; captured GPS must be valid WGS84.");
        return new GroundTruthMeasurement { Id = id, FieldInspectionSessionId = sessionId, SampleId = NormalizeRequired(sampleId, nameof(sampleId), 100),
            RoadSectionVersionId = routeId, SurveyId = surveyId, DefectId = defectId, MeasurementType = type,
            Value = value, ValueState = valueState, UnknownReason = NormalizeOptional(unknownReason, nameof(unknownReason), 1000),
            Dimension = dimension, Unit = normalizedUnit, Location = location, LocationState = location is null ? "UNKNOWN" : "CAPTURED",
            LocationReason = NormalizeOptional(locationReason, nameof(locationReason), 1000), InstrumentName = NormalizeRequired(instrument, nameof(instrument), 150),
            MeasurementMethod = NormalizeRequired(method, nameof(method), 500), MeasuredBy = NormalizeRequired(measuredBy, nameof(measuredBy), 200),
            MeasuredAt = measuredAt.ToUniversalTime(), EvidenceFileId = evidenceId, Notes = NormalizeOptional(notes, nameof(notes), 4000) };
    }

    private static string NormalizeRequired(string value, string parameterName, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value exceeds maximum length {maxLength}.", parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, string parameterName, int maxLength)
    {
        return string.IsNullOrWhiteSpace(value) ? null : NormalizeRequired(value, parameterName, maxLength);
    }
}
