using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Candidates;
using System.Text.Json;

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

    public static Defect CreateFromReport(Guid id, CandidateSourceFacts source, CandidateClassification classification,
        Geometry? geometry, DateTimeOffset reportedAt)
    {
        ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(classification);
        if (id == Guid.Empty || source.Source.Kind != CandidateSourceKind.Report) throw new ArgumentException("A Report source and defect identity are required.");
        if (geometry is not null && geometry.SRID is not (32648 or 32649)) throw new ArgumentException("Resolved metric geometry is required when present.", nameof(geometry));
        return new Defect { Id = id, ProjectId = source.ProjectId, RoadSectionVersionId = classification.RoadSectionVersionId,
            DefectTypeCode = classification.DefectTypeCode, CauseCategoryCode = classification.CauseCategoryCode, Severity = classification.Severity,
            Status = DefectStatus.Open, Geometry = geometry, ReportedAt = reportedAt.ToUniversalTime() };
    }

    public DefectVerificationLog Assess(string type, string? cause, DefectSeverity severity, Guid actor, string reason)
    {
        if (Status is not (DefectStatus.Open or DefectStatus.Verified)) throw new InvalidOperationException("Only an Open or Verified defect can be assessed.");
        if (severity == DefectSeverity.Unknown || !Enum.IsDefined(severity)) throw new ArgumentOutOfRangeException(nameof(severity));
        var nextType = ValidateCode(type, nameof(type));
        var nextCause = string.IsNullOrWhiteSpace(cause) ? null : ValidateCode(cause, nameof(cause));
        var log = DefectVerificationLog.Create(Guid.NewGuid(), Id, null, DefectVerificationAction.Adjust,
            JsonSerializer.Serialize(new { DefectTypeCode, CauseCategoryCode, Severity, Status }),
            JsonSerializer.Serialize(new { DefectTypeCode = nextType, CauseCategoryCode = nextCause, Severity = severity, Status }), null, null, actor, reason);
        DefectTypeCode = nextType; CauseCategoryCode = nextCause; Severity = severity;
        return log;
    }

    // The caller must resolve/lock actual source relations and VERIFIED files.
    // These domain facts do not prove authorization or SQL persistence.
    public DefectVerificationLog DecideFromExistingEvidence(DefectVerificationAction action, IReadOnlyCollection<Guid> evidenceIds,
        IReadOnlyCollection<Guid> verifiedRelatedEvidenceIds, Guid actor, string reason)
    {
        if (Status != DefectStatus.Open) throw new InvalidOperationException("Only an Open defect can be verified or rejected.");
        if (action is not (DefectVerificationAction.Confirm or DefectVerificationAction.Reject)) throw new ArgumentOutOfRangeException(nameof(action));
        ArgumentNullException.ThrowIfNull(evidenceIds); ArgumentNullException.ThrowIfNull(verifiedRelatedEvidenceIds);
        if (evidenceIds.Count == 0 || evidenceIds.Any(id => id == Guid.Empty || !verifiedRelatedEvidenceIds.Contains(id)) || evidenceIds.Distinct().Count() != evidenceIds.Count)
            throw new InvalidOperationException("Every selected evidence item must be verified and related to this defect.");
        var nextStatus = action == DefectVerificationAction.Confirm ? DefectStatus.Verified : DefectStatus.Rejected;
        var log = DefectVerificationLog.Create(Guid.NewGuid(), Id, null, action, JsonSerializer.Serialize(new { Status }),
            JsonSerializer.Serialize(new { Status = nextStatus, method = "EXISTING_EVIDENCE", evidenceIds }), null, null, actor, reason);
        Status = nextStatus;
        return log;
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
