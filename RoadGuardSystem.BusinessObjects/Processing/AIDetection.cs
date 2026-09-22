using System.Text.Json;
using NetTopologySuite.Geometries;

namespace RoadGuardSystem.BusinessObjects.Processing;

public sealed class AIDetection
{
    private AIDetection()
    {
    }

    public Guid Id { get; private set; }

    public Guid ProcessingJobId { get; private set; }

    public Guid ModelVersionId { get; private set; }

    public Guid? RoadSectionVersionId { get; private set; }

    public Geometry? Geometry { get; private set; }

    public string? DefectTypeCode { get; private set; }

    public decimal Confidence { get; private set; }

    public decimal? EstimatedWidth { get; private set; }

    public decimal? EstimatedLength { get; private set; }

    public string RawPayload { get; private set; } = string.Empty;

    public static AIDetection Create(
        Guid id,
        Guid processingJobId,
        Guid modelVersionId,
        Guid? roadSectionVersionId,
        Geometry? geometry,
        string? defectTypeCode,
        decimal confidence,
        decimal? estimatedWidth,
        decimal? estimatedLength,
        string rawPayload)
    {
        if (id == Guid.Empty || processingJobId == Guid.Empty || modelVersionId == Guid.Empty)
        {
            throw new ArgumentException("Detection, processing job, and model version ids must not be empty.");
        }

        if (roadSectionVersionId == Guid.Empty)
        {
            throw new ArgumentException("Road section version id must be non-empty when supplied.", nameof(roadSectionVersionId));
        }

        if (confidence is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(confidence), "Detection confidence must be between zero and one.");
        }

        if (estimatedWidth is < 0m || estimatedLength is < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(estimatedWidth), "Estimated dimensions must not be negative.");
        }

        var normalizedPayload = ValidateJson(rawPayload, nameof(rawPayload));
        var normalizedDefectTypeCode = string.IsNullOrWhiteSpace(defectTypeCode)
            ? null
            : NormalizeCode(defectTypeCode, nameof(defectTypeCode));

        return new AIDetection
        {
            Id = id,
            ProcessingJobId = processingJobId,
            ModelVersionId = modelVersionId,
            RoadSectionVersionId = roadSectionVersionId,
            Geometry = geometry,
            DefectTypeCode = normalizedDefectTypeCode,
            Confidence = confidence,
            EstimatedWidth = estimatedWidth,
            EstimatedLength = estimatedLength,
            RawPayload = normalizedPayload
        };
    }

    private static string NormalizeCode(string value, string parameterName)
    {
        var normalized = value.Trim();
        if (normalized.Length > 80)
        {
            throw new ArgumentException("Defect type code exceeds maximum length 80.", parameterName);
        }

        return normalized;
    }

    private static string ValidateJson(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
            {
                throw new ArgumentException("Raw payload must be a JSON object or array.", parameterName);
            }
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Raw payload must be valid JSON.", parameterName, exception);
        }

        return value.Trim();
    }
}
