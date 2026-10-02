using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Candidates;

public sealed class CandidateClassification
{
    private CandidateClassification()
    {
    }

    public Guid RoadSectionVersionId { get; private set; }
    public Guid? SegmentId { get; private set; }
    public string DefectTypeCode { get; private set; } = string.Empty;
    public string? CauseCategoryCode { get; private set; }
    public DefectSeverity Severity { get; private set; }

    public static CandidateClassification Create(
        Guid roadSectionVersionId,
        string defectTypeCode,
        string? causeCategoryCode,
        DefectSeverity severity,
        Guid? segmentId)
    {
        if (roadSectionVersionId == Guid.Empty || segmentId == Guid.Empty)
        {
            throw new ArgumentException("Road section version and supplied segment identifiers must not be empty.");
        }

        if (severity == DefectSeverity.Unknown || !Enum.IsDefined(severity))
        {
            throw new ArgumentOutOfRangeException(nameof(severity));
        }

        return new CandidateClassification
        {
            RoadSectionVersionId = roadSectionVersionId,
            SegmentId = segmentId,
            DefectTypeCode = NormalizeCode(defectTypeCode, nameof(defectTypeCode)),
            CauseCategoryCode = string.IsNullOrWhiteSpace(causeCategoryCode) ? null : NormalizeCode(causeCategoryCode, nameof(causeCategoryCode)),
            Severity = severity
        };
    }

    private static string NormalizeCode(string value, string parameterName)
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
