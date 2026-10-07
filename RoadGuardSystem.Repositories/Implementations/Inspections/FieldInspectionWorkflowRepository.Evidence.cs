using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Models.Huy01;
namespace RoadGuardSystem.Repositories.Implementations.Inspections;

public sealed partial class FieldInspectionWorkflowRepository
{
    private async Task<FieldWorkflowResultFact> ReuseAsync(FieldWorkflowCommand c, FieldInspectionTask task, CancellationToken token, bool validateOnly = false)
    {
        var input = c.Input as FieldEvidenceReuseInputFact ?? throw new ArgumentException("Reuse decision input required.");
        if (!validateOnly && task.Status is RoadGuardSystem.aBusinessObjects.Commons.FieldInspectionTaskStatus.Completed or RoadGuardSystem.aBusinessObjects.Commons.FieldInspectionTaskStatus.Cancelled) Deny(409, "invalid_state_transition");
        await db.Files.FromSqlInterpolated($"SELECT * FROM [Files] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={input.FileId}").AsNoTracking().ToListAsync(token);
        await db.UploadSessions.FromSqlInterpolated($"SELECT * FROM [UploadSessions] WITH (UPDLOCK,HOLDLOCK) WHERE [FileId]={input.FileId}").AsNoTracking().ToListAsync(token);
        await db.Set<HuyDefectSourceLink>().FromSqlInterpolated($"SELECT * FROM [DefectSourceLinks] WITH (UPDLOCK,HOLDLOCK) WHERE [DefectId]={task.DefectId}").AsNoTracking().ToListAsync(token);
        var file = await new AnhHuyFactsRepository(db).GetFileAsync(input.FileId, token);
        if (file is null) Deny(404, "not_found");
        if (file.State != "VERIFIED" || file.Checksum != input.ExpectedChecksum) Deny(409, "evidence_version_mismatch");
        object source;
        if (input.SourceKind == "REPORTER")
        {
            await db.Database.SqlQuery<Guid>($"SELECT [Id] AS [Value] FROM [ReportOriginalEvidence] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={input.SourceEvidenceId}").ToArrayAsync(token);
            await db.Database.SqlQuery<Guid>($"SELECT [Id] AS [Value] FROM [ReportSupplementEvidence] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={input.SourceEvidenceId}").ToArrayAsync(token);
            var reports = await db.Set<HuyDefectSourceLink>().AsNoTracking().Where(x => x.DefectId == task.DefectId && x.ProjectId == c.ProjectId && x.EndedAt == null && x.ReportSourceId != null).Select(x => x.ReportSourceId!.Value).ToArrayAsync(token);
            var original = await db.Reports.AsNoTracking().Where(x => reports.Contains(x.Id)).SelectMany(x => x.OriginalEvidence).Where(x => x.Id == input.SourceEvidenceId && x.FileId == input.FileId).SingleOrDefaultAsync(token);
            var supplement = original is null ? await db.ReportSupplements.AsNoTracking().Where(x => reports.Contains(x.ReportId)).SelectMany(x => x.Evidence).Where(x => x.Id == input.SourceEvidenceId && x.FileId == input.FileId).SingleOrDefaultAsync(token) : null;
            var evidence = original ?? supplement;
            if (evidence is null || file.ProjectId is not null || file.Purpose != "REPORT_PHOTO" || file.OwnerId != evidence.OwnerUserId || file.FileVersion != evidence.FileVersion) Deny(403, "evidence_access_forbidden");
            source = new { reportId = evidence.ReportId, evidence.Id, evidence.FileVersion, evidence.OwnerUserId, evidence.CaptureMetadata };
        }
        else if (input.SourceKind == "DRONE")
        {
            await db.SurveyFiles.FromSqlInterpolated($"SELECT * FROM [SurveyFiles] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={input.SourceEvidenceId}").AsNoTracking().ToListAsync(token);
            var surveyFile = await db.SurveyFiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == input.SourceEvidenceId && x.FileId == input.FileId && x.SurveyId == task.SurveyId, token);
            if (surveyFile is null || task.SurveyId is null || file.ProjectId != task.ProjectId || file.Purpose is not ("SURVEY_VIDEO" or "TELEMETRY") || surveyFile.Checksum != file.Checksum) Deny(403, "evidence_access_forbidden");
            source = new { surveyFile.Id, surveyFile.SurveyId, surveyFile.CaptureStartedAt, surveyFile.CaptureEndedAt, surveyFile.SyncStatus };
        }
        else throw new ArgumentException("Reporter or Drone source required.");
        if (validateOnly) return new(200);
        var now = clock.GetUtcNow(); var row = FieldInspectionEvidenceReuseDecision.Create(Guid.NewGuid(), c.ProjectId, task.Id, input.FileId, input.SourceEvidenceId, c.Admission.CallerId, input.SourceKind,
            file.Checksum, JsonSerializer.Serialize(new { source, file.FileVersion, file.UploadedAt, file.Purpose, file.Checksum }, Json), input.Reason, now);
        db.Set<FieldInspectionEvidenceReuseDecision>().Add(row); Emit(c, task, await CurrentAssignmentAsync(task.Id, token), "REVIEWED", "Authorized BEFORE evidence reuse", now, row.Id);
        return new(201, Value: new { row.Id, row.TaskId, row.FileId, row.SourceKind, row.FileChecksum, row.Reason, row.OccurredAt });
    }
    private async Task<FieldWorkflowResultFact> EvidenceAsync(FieldWorkflowCommand c, FieldInspectionTask task, CancellationToken token)
    {
        var fileId = c.Input is Guid requested ? requested : Guid.Empty;
        if (fileId == Guid.Empty) Deny(400, "validation_error");
        var link = await db.Set<FieldInspectionEvidenceLink>().AsNoTracking().Where(x => x.TaskId == task.Id && x.FileId == fileId).OrderByDescending(x => x.Id).FirstOrDefaultAsync(token);
        var reuse = await db.Set<FieldInspectionEvidenceReuseDecision>().AsNoTracking().Where(x => x.TaskId == task.Id && x.FileId == fileId).OrderByDescending(x => x.OccurredAt).FirstOrDefaultAsync(token);
        if (link is null && reuse is null) Deny(404, "not_found");
        await db.Files.FromSqlInterpolated($"SELECT * FROM [Files] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={fileId}").AsNoTracking().ToListAsync(token);
        await db.UploadSessions.FromSqlInterpolated($"SELECT * FROM [UploadSessions] WITH (UPDLOCK,HOLDLOCK) WHERE [FileId]={fileId}").AsNoTracking().ToListAsync(token);
        var file = await db.Files.AsNoTracking().SingleOrDefaultAsync(x => x.Id == fileId, token);
        var facts = await new AnhHuyFactsRepository(db).GetFileAsync(fileId, token);
        var scope = await db.FileScopes.AsNoTracking().SingleOrDefaultAsync(x => x.FileId == fileId, token);
        if (file is null || facts is null || scope is null) Deny(404, "not_found");
        if (reuse is null && (scope.ProjectId != task.ProjectId || scope.TargetId != task.Id || scope.Purpose is not ("BEFORE" or "AFTER" or "MEASUREMENT"))) Deny(403, "evidence_access_forbidden");
        if (link is not null && link.DeclaredChecksum != file.Checksum || reuse is not null && reuse.FileChecksum != file.Checksum) Deny(409, "evidence_version_mismatch");
        if (reuse is not null) await ValidateHistoricalReuseAsync(task, reuse, facts, token);
        return new(200, Value: new FieldTaskEvidenceFile(file.Id, facts.State, file.Checksum, file.MimeType, file.SizeBytes, file.StorageUri), Version: file.Checksum);
    }
    private async Task ValidateHistoricalReuseAsync(FieldInspectionTask task, FieldInspectionEvidenceReuseDecision reuse, ReporterFileFacts file, CancellationToken token)
    {
        await db.UploadSessions.FromSqlInterpolated($"SELECT * FROM [UploadSessions] WITH (UPDLOCK,HOLDLOCK) WHERE [FileId]={file.FileId}").AsNoTracking().ToListAsync(token);
        var current = await new AnhHuyFactsRepository(db).GetFileAsync(file.FileId, token);
        if (current is null || current.State != "VERIFIED" || current.Checksum != reuse.FileChecksum) Deny(409, "evidence_version_mismatch");
        using var provenance = JsonDocument.Parse(reuse.ProvenanceJson);
        if (provenance.RootElement.GetProperty("fileVersion").GetString() != current.FileVersion) Deny(409, "evidence_version_mismatch");
        if (reuse.SourceKind == "REPORTER")
        {
            await db.Database.SqlQuery<Guid>($"SELECT [Id] AS [Value] FROM [ReportOriginalEvidence] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={reuse.SourceEvidenceId}").ToArrayAsync(token);
            await db.Database.SqlQuery<Guid>($"SELECT [Id] AS [Value] FROM [ReportSupplementEvidence] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={reuse.SourceEvidenceId}").ToArrayAsync(token);
            var original = await db.Reports.AsNoTracking().SelectMany(x => x.OriginalEvidence).SingleOrDefaultAsync(x => x.Id == reuse.SourceEvidenceId && x.FileId == reuse.FileId, token);
            var supplement = original is null ? await db.ReportSupplements.AsNoTracking().SelectMany(x => x.Evidence).SingleOrDefaultAsync(x => x.Id == reuse.SourceEvidenceId && x.FileId == reuse.FileId, token) : null;
            var evidence = original ?? supplement;
            if (evidence is null || current.OwnerId != evidence.OwnerUserId || current.ProjectId is not null || current.Purpose != "REPORT_PHOTO" || current.FileVersion != evidence.FileVersion ||
                !await db.Set<HuyDefectSourceLink>().AnyAsync(x => x.ProjectId == task.ProjectId && x.DefectId == task.DefectId && x.ReportSourceId == evidence.ReportId &&
                    x.CreatedAt <= reuse.OccurredAt && (x.EndedAt == null || x.EndedAt > reuse.OccurredAt), token)) Deny(403, "evidence_access_forbidden");
        }
        else
        {
            var source = await db.SurveyFiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == reuse.SourceEvidenceId && x.FileId == reuse.FileId && x.SurveyId == task.SurveyId, token);
            if (source is null || current.ProjectId != task.ProjectId || source.Checksum != current.Checksum) Deny(403, "evidence_access_forbidden");
        }
    }
    private async Task<FieldWorkflowResultFact> GeometryAsync(FieldInspectionTask task, CancellationToken token)
    {
        var setId = task.SegmentSetId.GetValueOrDefault();
        if (setId == Guid.Empty) Deny(409, "source_not_ready");
        var set = await db.RoadSegmentSets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == setId && x.RoadSectionVersionId == task.RoadSectionVersionId, token);
        var route = await db.RoadSectionVersions.AsNoTracking().SingleAsync(x => x.Id == task.RoadSectionVersionId, token);
        if (set is null || set.Status is not ("PUBLISHED" or "SUPERSEDED")) Deny(409, "source_not_ready");
        var segments = await db.RoadSegments.AsNoTracking().Where(x => x.SegmentSetId == setId).OrderBy(x => x.Sequence).Select(x => new { x.Id, x.Sequence, x.FromOffsetMeters, x.ToOffsetMeters, x.StartStationMeters, x.EndStationMeters, x.Geometry }).ToArrayAsync(token);
        object? layout = null;
        if (task.LayoutRevisionId is Guid layoutId)
        {
            var row = await db.Set<PavementLayoutRevision>().AsNoTracking().SingleAsync(x => x.Id == layoutId && x.ProjectId == task.ProjectId, token);
            var preview = Decode<PavementGeometryPreviewFact>(row.SnapshotJson);
            layout = new { row.Id, row.ContentHash, row.Kind, slabs = task.SlabId is null ? preview.Slabs : preview.Slabs.Where(x => x.Key == task.SlabId).ToArray() };
        }
        return new(200, Value: new
        {
            schemaVersion = 1,
            taskId = task.Id,
            projectId = task.ProjectId,
            routeVersionId = task.RoadSectionVersionId,
            segmentSetId = setId,
            task.LayoutRevisionId,
            task.SlabId,
            task.MapPublicationId,
            task.CrsProfileRevisionId,
            route = new { spatialSrid = route.Geometry.SRID, wkt = route.Geometry.AsText(), profileStatus = task.CrsProfileRevisionId is null ? "LEGACY_UNKNOWN" : "PINNED_PROFILE" },
            segments = segments.Select(x => new { x.Id, x.Sequence, x.FromOffsetMeters, x.ToOffsetMeters, x.StartStationMeters, x.EndStationMeters, spatialSrid = x.Geometry?.SRID, wkt = x.Geometry?.AsText() }),
            layout
        },
            Version: Convert.ToBase64String(task.RowVersion));
    }
}
