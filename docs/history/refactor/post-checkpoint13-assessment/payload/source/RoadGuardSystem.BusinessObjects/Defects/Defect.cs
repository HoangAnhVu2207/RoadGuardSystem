using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Defects;

public sealed class Defect
{
    private Defect()
    {
    }

    public Guid Id { get; private set; }

    public Guid? ProjectId { get; private set; }

    public Guid? RoadSectionVersionId { get; private set; }

    public Guid? SourceAIDetectionId { get; private set; }

    public string DefectTypeCode { get; private set; } = string.Empty;

    public string? CauseCategoryCode { get; private set; }

    public DefectSeverity Severity { get; private set; }

    public DefectStatus Status { get; private set; }

    public Geometry? Geometry { get; private set; }

    public DateTimeOffset? ReportedAt { get; private set; }

    public static Defect Create(Guid id, string defectTypeCode, string? causeCategoryCode = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Defect id must not be empty.", nameof(id));
        }

        return new Defect
        {
            Id = id,
            DefectTypeCode = ValidateCode(defectTypeCode, nameof(defectTypeCode)),
            CauseCategoryCode = string.IsNullOrWhiteSpace(causeCategoryCode)
                ? null
                : ValidateCode(causeCategoryCode, nameof(causeCategoryCode))
        };
    }

    public static Defect Create(
        Guid id,
        Guid projectId,
        Guid roadSectionVersionId,
        Guid? sourceAIDetectionId,
        string defectTypeCode,
        string? causeCategoryCode,
        DefectSeverity severity,
        DefectStatus status,
        Geometry geometry,
        DateTimeOffset reportedAt)
    {
        if (id == Guid.Empty || projectId == Guid.Empty || roadSectionVersionId == Guid.Empty)
        {
            throw new ArgumentException("Defect, project, and road section version ids must not be empty.");
        }

        if (!Enum.IsDefined(severity) || severity == DefectSeverity.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(severity), "Defect severity must be specified.");
        }

        if (!Enum.IsDefined(status) || status == DefectStatus.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Defect status must be specified.");
        }

        ArgumentNullException.ThrowIfNull(geometry);
        if (sourceAIDetectionId == Guid.Empty)
        {
            throw new ArgumentException("Source AI detection id must be non-empty when supplied.", nameof(sourceAIDetectionId));
        }

        return new Defect
        {
            Id = id,
            ProjectId = projectId,
            RoadSectionVersionId = roadSectionVersionId,
            SourceAIDetectionId = sourceAIDetectionId,
            DefectTypeCode = ValidateCode(defectTypeCode, nameof(defectTypeCode)),
            CauseCategoryCode = string.IsNullOrWhiteSpace(causeCategoryCode)
                ? null
                : ValidateCode(causeCategoryCode, nameof(causeCategoryCode)),
            Severity = severity,
            Status = status,
            Geometry = geometry,
            ReportedAt = reportedAt.ToUniversalTime()
        };
    }

    private static string ValidateCode(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length > 80)
        {
            throw new ArgumentException("Catalog code exceeds maximum length 80.", parameterName);
        }

        return normalized;
    }
}
