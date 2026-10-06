using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace RoadGuardSystem.Repositories.Implementations.Retention;

// Reads actual persisted references, including superseded decisions and old attempts.
// This inventory never supplies retention duration or permission to delete.
internal sealed class H4H5RetentionSource
{
    public Guid FileId { get; set; }
    public Guid SourceId { get; set; }
    public Guid ProjectId { get; set; }
    public string Kind { get; set; } = "";
    public string FactsJson { get; set; } = "";
    public string? DeclaredChecksum { get; set; }
    public string? ActualChecksum { get; set; }
}
internal static class H4H5RetentionSources
{
    private const string Sources = """
        SELECT e.FileId, a.Id SourceId, a.ProjectId, 'REPAIR_ATTEMPT' Kind,
          (SELECT e.*,a.PayloadHash,a.ItemId,a.ObligationId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) FactsJson,e.Hash DeclaredChecksum
        FROM RepairAttemptEvidence e JOIN RepairAttempts a ON a.Id=e.AttemptId
        UNION ALL
        SELECT e.FileId,a.Id,a.ProjectId,'REPAIR_ATTEMPT_FIELD_EVIDENCE',
          (SELECT e.*,l.Id AttemptLinkId,l.FormalRootSubmissionId,l.PreviousLinkId,a.ItemId,a.ObligationId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),e.DeclaredChecksum
        FROM RepairAttemptSubmissionLinks l JOIN RepairAttempts a ON a.Id=l.AttemptId AND a.ProjectId=l.ProjectId
        JOIN FieldInspectionEvidenceLinks e ON e.SubmissionId=l.SubmissionId AND e.ProjectId=l.ProjectId
        UNION ALL
        SELECT e.FileId,d.Id,i.ProjectId,'REPAIR_CORRECTION',
          (SELECT e.*,d.SupersedesDecisionId,d.Result,d.ObligationId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),e.Hash
        FROM RepairCorrectionEvidence e JOIN RepairDecisions d ON d.Id=e.DecisionId JOIN RepairItems i ON i.Id=d.ItemId
        UNION ALL
        SELECT e.FileId,c.Id,m.ProjectId,'REPAIR_SAFETY_CHECK',
          (SELECT e.*,c.MeasureId,c.Result,c.At FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),e.Hash
        FROM RepairSafetyCheckEvidence e JOIN RepairSafetyChecks c ON c.Id=e.CheckId JOIN RepairTemporarySafetyMeasures m ON m.Id=c.MeasureId
        UNION ALL
        SELECT e.FileId,e.Id,a.ProjectId,'REPAIR_ASSESSMENT',
          (SELECT e.*,a.ContentHash,a.ItemId,a.TaskId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),e.ChecksumSha256
        FROM RepairAssessmentEvidence e JOIN RepairMeasurementAssessments a ON a.Id=e.AssessmentId WHERE e.FileId IS NOT NULL
        UNION ALL
        SELECT e.FileId,e.Id,e.ProjectId,'OFFLINE_CAPTURE',(SELECT e.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),e.Checksum
        FROM OfflineEvidenceCaptureReferences e
        UNION ALL
        SELECT e.FileId,e.Id,e.ProjectId,'OFFLINE_ADMITTED_FILE',(SELECT e.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),e.ContentChecksum
        FROM OfflineAdmittedFileReferences e
        UNION ALL
        SELECT e.FileId,e.Id,e.ProjectId,'OFFLINE_PACKAGE_FILE',(SELECT e.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),e.ContentChecksum
        FROM OfflinePackageFileReferences e
        UNION ALL
        SELECT e.FileId,a.Id,a.ProjectId,'REPAIR_HANDOVER_SOURCE',(SELECT e.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),NULL
        FROM RepairEligibilityHandoverSources e JOIN RepairEligibilityAssessments a ON a.Id=e.EligibilityAssessmentId WHERE e.FileId IS NOT NULL
        UNION ALL
        SELECT e.SourceDocumentId,a.Id,a.ProjectId,'REPAIR_WARRANTY_SOURCE',(SELECT e.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),NULL
        FROM RepairEligibilityWarrantySources e JOIN RepairEligibilityAssessments a ON a.Id=e.EligibilityAssessmentId WHERE e.SourceDocumentId IS NOT NULL
        """;
    internal static Task<H4H5RetentionSource[]> ReadAsync(RoadGuardDbContext db, Guid? fileId, Guid? projectId, CancellationToken token)
        => db.Database.SqlQueryRaw<H4H5RetentionSource>("SELECT s.*, f.Checksum ActualChecksum FROM (" + Sources +
            ") s LEFT JOIN Files f ON f.Id=s.FileId WHERE (@file IS NULL OR s.FileId=@file) AND (@project IS NULL OR s.ProjectId=@project)",
            new SqlParameter("@file", System.Data.SqlDbType.UniqueIdentifier) { Value = (object?)fileId ?? DBNull.Value },
            new SqlParameter("@project", System.Data.SqlDbType.UniqueIdentifier) { Value = (object?)projectId ?? DBNull.Value }).ToArrayAsync(token);
}
