using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.DTOs.Retention;
using RoadGuardSystem.Repositories.Retention;

namespace RoadGuardSystem.Repositories.Implementations.Retention;
/// <summary>Read-only inventory of persisted inspection, repair and offline evidence references.
/// Completeness is limited to the enumerated source tables and verified provenance.
/// Uses the caller's scoped context/transaction and never admits or deletes evidence.</summary>
public sealed class Huy02InspectionRetentionContributor(RoadGuardDbContext db) : IRetentionInventoryContributor
{
    public string Name => "HUY02_INSPECTION";
    public async Task<RetentionInventoryContribution> ReadAsync(Guid fileId, CancellationToken token)
    {
        var measurements = await db.GroundTruthMeasurements.AsNoTracking()
            .Where(x => x.EvidenceFileId == fileId).ToArrayAsync(token);
        var sessionIds = measurements.Select(x => x.FieldInspectionSessionId).Distinct().ToArray();
        var sessions = await db.FieldInspectionSessions.AsNoTracking()
            .Where(x => x.EvidenceFileId == fileId || sessionIds.Contains(x.Id)).ToArrayAsync(token);
        var taskIds = sessions.Where(x => x.FieldInspectionTaskId.HasValue)
            .Select(x => x.FieldInspectionTaskId!.Value).Distinct().ToArray();
        var tasks = await db.FieldInspectionTasks.AsNoTracking().Where(x => taskIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, token);
        var versionIds = sessions.Select(x => x.RoadSectionVersionId).Distinct().ToArray();
        var roads = await (from version in db.RoadSectionVersions.AsNoTracking()
            join road in db.RoadSections.AsNoTracking() on version.RoadSectionId equals road.Id
            where versionIds.Contains(version.Id)
            select new { version.Id, road.ProjectId }).ToDictionaryAsync(x => x.Id, x => x.ProjectId, token);
        var projectIds = sessions.Select(x => x.ProjectId).Distinct().ToArray();
        var projects = await db.Projects.AsNoTracking().Where(x => projectIds.Contains(x.Id))
            .Select(x => x.Id).ToArrayAsync(token);
        var surveyIds = sessions.Where(x => x.SurveyId.HasValue).Select(x => x.SurveyId!.Value).Distinct().ToArray();
        var surveys = await db.Surveys.AsNoTracking().Where(x => surveyIds.Contains(x.Id))
            .Select(x => new { x.Id, x.ProjectId, x.RoadSectionVersionId }).ToDictionaryAsync(x => x.Id, token);
        var defectIds = tasks.Values.Select(x => x.DefectId).Distinct().ToArray();
        var defects = await db.Defects.AsNoTracking().Where(x => defectIds.Contains(x.Id))
            .Select(x => new { x.Id, x.ProjectId, x.RoadSectionVersionId }).ToDictionaryAsync(x => x.Id, token);
        var reasons = new HashSet<string>(StringComparer.Ordinal);
        var references = new List<RetentionReferenceView>();
        var sessionFacts = new Dictionary<Guid, object>();
        foreach (var session in sessions.OrderBy(x => x.Id))
        {
            var task = session.FieldInspectionTaskId is Guid taskId ? tasks.GetValueOrDefault(taskId) : null;
            var valid = projects.Contains(session.ProjectId) &&
                roads.TryGetValue(session.RoadSectionVersionId, out var roadProject) && roadProject == session.ProjectId &&
                (session.SurveyId is not Guid surveyId || (surveys.TryGetValue(surveyId, out var survey) &&
                    survey.ProjectId == session.ProjectId && survey.RoadSectionVersionId == session.RoadSectionVersionId));
            if (session.Purpose is FieldInspectionPurpose.DefectVerification or FieldInspectionPurpose.PreMeasurement or FieldInspectionPurpose.PostRepair or FieldInspectionPurpose.Verification)
                valid &= task is not null && session.InspectorUserId.HasValue &&
                    task.ProjectId == session.ProjectId && task.SurveyId == session.SurveyId &&
                    task.RoadSectionVersionId == session.RoadSectionVersionId &&
                    defects.TryGetValue(task.DefectId, out var defect) && defect.ProjectId == session.ProjectId &&
                    defect.RoadSectionVersionId == session.RoadSectionVersionId;
            else
                valid &= session.Purpose == FieldInspectionPurpose.ResearchValidation && task is null &&
                    session.FieldInspectionTaskId is null;
            if (!valid) reasons.Add("HUY02_INSPECTION_PROVENANCE_UNRESOLVED");
            // Do not require a currently active assignment, Open defect, or mutable session:
            // these are historical obligations even after workflow authority changes.
            var facts = new { session.Id, session.ProjectId, session.Purpose, session.FieldInspectionTaskId,
                session.SurveyId, session.RoadSectionVersionId, session.InspectorUserId, session.ConductedAt,
                session.Method, session.Status, session.EvidenceFileId,
                Task = task is null ? null : new { task.Id, task.ProjectId, task.DefectId, task.SurveyId,
                    task.RoadSectionVersionId } };
            sessionFacts.Add(session.Id, facts);
            if (session.EvidenceFileId == fileId)
                references.Add(new("FIELD_INSPECTION_SESSION", session.Id, session.ProjectId, Version(facts)));
        }
        var sessionById = sessions.ToDictionary(x => x.Id);
        foreach (var measurement in measurements.OrderBy(x => x.Id))
        {
            sessionById.TryGetValue(measurement.FieldInspectionSessionId, out var session);
            var task = session?.FieldInspectionTaskId is Guid taskId ? tasks.GetValueOrDefault(taskId) : null;
            var valid = session is not null && measurement.RoadSectionVersionId == session.RoadSectionVersionId;
            if (session?.Purpose is FieldInspectionPurpose.DefectVerification or FieldInspectionPurpose.PreMeasurement or FieldInspectionPurpose.PostRepair or FieldInspectionPurpose.Verification)
                valid &= task is not null && measurement.DefectId == task.DefectId &&
                    measurement.SurveyId == session.SurveyId;
            else
                valid &= session?.Purpose == FieldInspectionPurpose.ResearchValidation &&
                    measurement.DefectId is null && measurement.SurveyId is null;
            if (!valid) reasons.Add("HUY02_INSPECTION_PROVENANCE_UNRESOLVED");
            references.Add(new("GROUND_TRUTH_MEASUREMENT", measurement.Id, session?.ProjectId,
                Version(new { measurement.Id, measurement.FieldInspectionSessionId, measurement.SampleId,
                    measurement.RoadSectionVersionId, measurement.SurveyId, measurement.DefectId,
                    measurement.MeasurementType, measurement.Value, measurement.Unit,
                    Longitude = measurement.Location == null ? (double?)null : measurement.Location.X,
                    Latitude = measurement.Location == null ? (double?)null : measurement.Location.Y,
                    measurement.ValueState, measurement.UnknownReason, measurement.Dimension, measurement.LocationState, measurement.LocationReason,
                    measurement.InstrumentName, measurement.InstrumentReference, measurement.MeasurementMethod,
                    measurement.MeasuredBy, measurement.MeasuredAt, measurement.EvidenceFileId, measurement.Notes,
                    Session = sessionFacts.GetValueOrDefault(measurement.FieldInspectionSessionId) })));
        }
        var links = await db.Set<FieldInspectionEvidenceLink>().AsNoTracking().Where(x => x.FileId == fileId).OrderBy(x => x.Id).ToArrayAsync(token);
        var reuses = await db.Set<FieldInspectionEvidenceReuseDecision>().AsNoTracking().Where(x => x.FileId == fileId).OrderBy(x => x.Id).ToArrayAsync(token);
        var fileChecksum = await db.Files.AsNoTracking().Where(x => x.Id == fileId).Select(x => x.Checksum).SingleOrDefaultAsync(token);
        foreach (var link in links)
        {
            var task = await db.FieldInspectionTasks.AsNoTracking().SingleOrDefaultAsync(x => x.Id == link.TaskId, token);
            var submission = await db.Set<FieldInspectionSubmission>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == link.SubmissionId, token);
            var assignment = await db.FieldInspectionAssignments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == link.AssignmentId, token);
            if (task is null || submission is null || assignment is null || task.ProjectId != link.ProjectId || submission.TaskId != link.TaskId ||
                submission.ProjectId != link.ProjectId || assignment.FieldInspectionTaskId != link.TaskId || link.DeclaredChecksum != fileChecksum)
                reasons.Add("H3_FIELD_EVIDENCE_PROVENANCE_UNRESOLVED");
            references.Add(new("FIELD_INSPECTION_EVIDENCE",link.Id,link.ProjectId,Version(new {link.Id,link.ProjectId,link.TaskId,link.AssignmentId,
                link.SubmissionId,link.FileId,link.CaptureOriginId,link.Purpose,link.DeclaredChecksum,link.MediaType,link.CaptureFactsJson,
                SubmissionHash=submission?.ContentHash,RootId=submission?.RootId,RouteVersionId=task?.RoadSectionVersionId,task?.SegmentSetId,task?.LayoutRevisionId})));
        }
        foreach (var reuse in reuses)
        {
            if (reuse.FileChecksum != fileChecksum || !await db.FieldInspectionTasks.AsNoTracking().AnyAsync(x => x.Id == reuse.TaskId && x.ProjectId == reuse.ProjectId,token))
                reasons.Add("H3_FIELD_REUSE_PROVENANCE_UNRESOLVED");
            references.Add(new("FIELD_BEFORE_REUSE",reuse.Id,reuse.ProjectId,Version(new {reuse.Id,reuse.ProjectId,reuse.TaskId,reuse.FileId,
                reuse.SourceEvidenceId,reuse.SourceKind,reuse.FileChecksum,reuse.ProvenanceJson,reuse.OccurredAt,reuse.ActorId,reuse.Reason})));
        }
        var repairOffline = await H4H5RetentionSources.ReadAsync(db, fileId, null, token);
        foreach (var source in repairOffline)
        {
            if (source.ActualChecksum is null || source.DeclaredChecksum is not null &&
                !string.Equals(source.DeclaredChecksum, source.ActualChecksum, StringComparison.OrdinalIgnoreCase))
                reasons.Add("H4_H5_EVIDENCE_PROVENANCE_UNRESOLVED");
            references.Add(new(source.Kind, source.SourceId, source.ProjectId,
                Version(new { source.FileId, source.FactsJson, source.ActualChecksum })));
        }
        return new(Name, reasons.Count == 0,
            references.OrderBy(x => x.Kind, StringComparer.Ordinal).ThenBy(x => x.Id).ThenBy(x => x.ProjectId).ToArray(),
            reasons.Order(StringComparer.Ordinal).ToArray());
    }
    public async Task<IReadOnlyList<Guid>> KnownProjectFilesAsync(Guid projectId, CancellationToken token)
    {
        var sessions = db.FieldInspectionSessions.AsNoTracking().Where(x => x.ProjectId == projectId);
        var sessionFiles = await sessions.Where(x => x.EvidenceFileId != null)
            .Select(x => x.EvidenceFileId!.Value).ToArrayAsync(token);
        var measurementFiles = await (from measurement in db.GroundTruthMeasurements.AsNoTracking()
            join session in sessions on measurement.FieldInspectionSessionId equals session.Id
            where measurement.EvidenceFileId != null
            select measurement.EvidenceFileId!.Value).ToArrayAsync(token);
        var fieldFiles = await db.Set<FieldInspectionEvidenceLink>().AsNoTracking().Where(x => x.ProjectId == projectId && x.FileId != null).Select(x => x.FileId!.Value).ToArrayAsync(token);
        var reuseFiles = await db.Set<FieldInspectionEvidenceReuseDecision>().AsNoTracking().Where(x => x.ProjectId == projectId).Select(x => x.FileId).ToArrayAsync(token);
        var repairOffline = await H4H5RetentionSources.ReadAsync(db, null, projectId, token);
        return sessionFiles.Concat(measurementFiles).Concat(fieldFiles).Concat(reuseFiles).Concat(repairOffline.Select(row => row.FileId)).Distinct().Order().ToArray();
    }
    private static string Version(object facts)
        => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(facts))).ToLowerInvariant();
}
