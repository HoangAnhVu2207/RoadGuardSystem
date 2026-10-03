using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Implementations.Cases;
using RoadGuardSystem.Services.Integration;

namespace RoadGuardSystem.Services.Implementations.Integration;

// Huy-owned producer. It intentionally returns an incomplete snapshot when the
// current schema cannot prove source/geometry facts; it is not registered here.
public sealed class CaseDefectReadReader(RoadGuardDbContext db) : ICaseDefectReadReader
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    public static string WireStatus(IncidentCaseStatus status) => CaseWorkflowRepository.WireStatus(status);

    public static byte[] SerializeCanonical(RoadGuardSystem.DTOs.Reporting.CaseDefectSnapshotV1 snapshot)
        => JsonSerializer.SerializeToUtf8Bytes(snapshot, WebJson);

    public static string ComputeHash(RoadGuardSystem.DTOs.Reporting.CaseDefectSnapshotV1 snapshot)
        => Convert.ToHexString(SHA256.HashData(SerializeCanonical(snapshot with { Hash = "" }))).ToLowerInvariant();

    public async Task<RoadGuardSystem.DTOs.Reporting.CaseDefectSnapshotV1?> CaptureAsync(
        Guid actorId, UserRoleCode role, Guid projectId,
        RoadGuardSystem.DTOs.Reporting.ReportingFiltersDto filters,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty || projectId == Guid.Empty) throw new UnauthorizedAccessException("A current project authority is required.");
        ArgumentNullException.ThrowIfNull(filters);
        if (role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor))
            throw new UnauthorizedAccessException("Only project PM or Supervisor dossier readers are supported.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var authorized = await db.Users.AsNoTracking().AnyAsync(u =>
            u.Id == actorId && u.Status == UserStatus.Active && u.RoleCode == role && !u.MustChangePassword &&
            db.Roles.Any(r => r.Code == u.RoleCode && r.IsActive), cancellationToken)
            && await db.ProjectMembers.AsNoTracking().AnyAsync(m => m.UserId == actorId && m.ProjectId == projectId &&
                m.RoleCode == role && m.Status == ProjectMemberStatus.Active && m.ValidFrom <= today &&
                (m.ValidTo == null || m.ValidTo >= today), cancellationToken);
        if (!authorized) throw new UnauthorizedAccessException("The actor is not authorized for this project.");

        var casesQuery = db.IncidentCases.AsNoTracking().Where(c => c.ProjectId == projectId);
        if (filters.RouteVersionId is Guid route)
            casesQuery = casesQuery.Where(c => EF.Property<Guid?>(c, "GeometryRouteVersionId") == route);
        if (filters.SegmentSetId is Guid segmentSet)
            casesQuery = casesQuery.Where(c => EF.Property<Guid?>(c, "GeometrySegmentSetId") == segmentSet);
        var cases = await casesQuery
            .Include(c => c.Conclusions)
            .OrderBy(c => c.Id)
            .ToListAsync(cancellationToken);
        var caseIds = cases.Select(c => c.Id).ToArray();
        var caseVersions = await db.IncidentCases.AsNoTracking()
            .Where(c => caseIds.Contains(c.Id))
            .Select(c => new { c.Id, Version = EF.Property<byte[]>(c, "RowVersion") })
            .ToDictionaryAsync(x => x.Id, x => ToVersion(x.Version), cancellationToken);
        var links = await db.Set<HuyCaseReportLink>().AsNoTracking()
            .Where(l => caseIds.Contains(l.CaseId) && l.EndedAt == null).ToListAsync(cancellationToken);
        var reportIds = links.Select(l => l.ReportId).Distinct().ToArray();
        var reports = await db.Reports.AsNoTracking().Include(r => r.Supplements).ThenInclude(s => s.Evidence)
            .Where(r => reportIds.Contains(r.Id) &&
            (!filters.From.HasValue || r.ReceivedAt >= filters.From) && (!filters.To.HasValue || r.ReceivedAt < filters.To))
            .ToListAsync(cancellationToken);
        var reportVersions = await db.Reports.AsNoTracking()
            .Where(r => reportIds.Contains(r.Id))
            .Select(r => new { r.Id, Version = EF.Property<byte[]>(r, "RowVersion") })
            .ToDictionaryAsync(x => x.Id, x => ToVersion(x.Version), cancellationToken);
        var publications = await db.Set<CasePublication>().AsNoTracking().Where(p => caseIds.Contains(p.CaseId)).ToListAsync(cancellationToken);
        var recipients = await db.Set<HuyPublicationRecipient>().AsNoTracking().Where(r => publications.Select(p => p.Id).Contains(r.PublicationId)).ToListAsync(cancellationToken);
        var publicationEvidence = await db.Set<HuyPublicationEvidence>().AsNoTracking().Where(e => publications.Select(p => p.Id).Contains(e.PublicationId)).ToListAsync(cancellationToken);

        var missing = new HashSet<string>(StringComparer.Ordinal);
        var caseFacts = cases.Select(c =>
        {
            var currentReports = links.Where(l => l.CaseId == c.Id).Join(reports, l => l.ReportId, r => r.Id,
                (l, r) => new RoadGuardSystem.DTOs.Reporting.CaseReportRefV1(r.Id, reportVersions[r.Id], r.ReceivedAt))
                .OrderBy(x => x.ReportId).ToArray();
            var caseVersion = caseVersions[c.Id];
            var conclusions = c.Conclusions.OrderBy(x => x.Id)
                .Select(x => new RoadGuardSystem.DTOs.Reporting.ReportingSourceRefDto("CASE_CONCLUSION", x.Id, caseVersion)).ToArray();
            var casePublications = publications.Where(p => p.CaseId == c.Id).Select(p => new RoadGuardSystem.DTOs.Reporting.CasePublicationRefV1(
                p.Id, caseVersion, recipients.Where(r => r.PublicationId == p.Id).Select(r => r.ReportId).OrderBy(x => x).ToArray(),
                publicationEvidence.Where(e => e.PublicationId == p.Id).Select(e => e.EvidenceId).OrderBy(x => x).ToArray()))
                .OrderBy(x => x.PublicationId).ToArray();
            return new RoadGuardSystem.DTOs.Reporting.CaseReadFactV1(c.Id, c.ProjectId!.Value, caseVersion, WireStatus(c.Status), currentReports, conclusions, casePublications);
        }).ToArray();

        var evidenceFacts = new List<RoadGuardSystem.DTOs.Reporting.CaseEvidenceRefV1>();
        foreach (var report in reports.OrderBy(row => row.Id))
        {
            var caseId = links.Single(link => link.ReportId == report.Id).CaseId;
            var evidence = report.OriginalEvidence.Concat(report.Supplements.SelectMany(s => s.Evidence));
            foreach (var item in evidence.OrderBy(row => row.Id))
            {
                var file = await (from stored in db.Files.AsNoTracking()
                    join scope in db.FileScopes.AsNoTracking() on stored.Id equals scope.FileId
                    join upload in db.UploadSessions.AsNoTracking() on stored.Id equals upload.FileId
                    where stored.Id == item.FileId
                    select new { stored, scope, upload }).SingleOrDefaultAsync(cancellationToken);
                if (file is null || file.scope.OwnerUserId != report.ReporterUserId ||
                    file.scope.ProjectId is not null || file.scope.Purpose != "REPORT_PHOTO" ||
                    file.upload.Status != UploadSessionStatus.Verified ||
                    Convert.ToBase64String(file.upload.RowVersion) != item.FileVersion ||
                    file.stored.UploadedByUserId != report.ReporterUserId)
                {
                    missing.Add("evidence_file_checksum_or_current_verification_unavailable");
                    continue;
                }
                evidenceFacts.Add(new(caseId, report.Id, item.Id, item.FileId, item.FileVersion,
                    file.stored.Checksum, file.stored.SizeBytes, file.stored.MimeType));
            }
        }
        foreach (var relation in publicationEvidence)
        {
            if (!recipients.Any(recipient => recipient.PublicationId == relation.PublicationId &&
                    recipient.ReportId == relation.RecipientReportId) ||
                !evidenceFacts.Any(evidence => evidence.EvidenceId == relation.EvidenceId &&
                    evidence.SourceReportId == relation.SourceReportId))
                missing.Add("publication_recipient_evidence_relation_unavailable");
        }

        var defectRows = await db.Defects.AsNoTracking().Where(d => d.ProjectId == projectId).ToListAsync(cancellationToken);
        var defectFacts = new List<RoadGuardSystem.DTOs.Reporting.DefectReadFactV1>();
        var sourceLinks = await db.Set<HuyDefectSourceLink>().AsNoTracking()
            .Where(link => link.ProjectId == projectId && link.EndedAt == null).ToListAsync(cancellationToken);
        foreach (var defect in defectRows.OrderBy(row => row.Id))
        {
            var sourceLink = sourceLinks.Where(link => link.DefectId == defect.Id)
                .OrderBy(link => link.CreatedAt).ThenBy(link => link.Id).FirstOrDefault();
            if (sourceLink is null || sourceLink.SourceKind != RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceKind.Report ||
                sourceLink.ReportSourceId is not Guid sourceReport)
            {
                missing.Add("defect_source_version_and_geometry_unavailable");
                continue;
            }
            var sourceCase = links.FirstOrDefault(link => link.ReportId == sourceReport);
            if (sourceCase is null || !caseIds.Contains(sourceCase.CaseId))
            {
                missing.Add("defect_source_scope_unavailable");
                continue;
            }
            var decision = await db.SourceDecisions.AsNoTracking().SingleOrDefaultAsync(
                row => row.Id == sourceLink.DecisionId, cancellationToken);
            if (decision is null || decision.ProjectId != projectId || decision.Source.Id != sourceReport ||
                decision.Source.Kind != sourceLink.SourceKind || defect.RoadSectionVersionId is not Guid defectRoute)
            {
                missing.Add("defect_source_version_and_geometry_unavailable");
                continue;
            }
            var sourceSet = await db.IncidentCases.AsNoTracking().Where(row => row.Id == sourceCase.CaseId)
                .Select(row => EF.Property<Guid?>(row, "GeometrySegmentSetId"))
                .SingleAsync(cancellationToken);
            var segmentId = decision.Classification?.SegmentId;
            if (filters.RouteVersionId is Guid requiredRoute && requiredRoute != defectRoute ||
                filters.SegmentSetId is Guid requiredSet && requiredSet != sourceSet ||
                filters.SegmentIds is { Length: > 0 } && !filters.SegmentIds.Contains(segmentId ?? Guid.Empty))
                continue;
            var version = await db.Defects.AsNoTracking().Where(row => row.Id == defect.Id)
                .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync(cancellationToken);
            defectFacts.Add(new(defect.Id, projectId, ToVersion(version), defect.Status.ToString().ToUpperInvariant(),
                "REPORT", sourceReport, decision.Source.SourceVersion, defectRoute, sourceSet, segmentId,
                decision.GeometryVersion, defect.Geometry is null ? "UNKNOWN" : "GEOMETRY_PRESENT"));
        }

        var snapshot = new RoadGuardSystem.DTOs.Reporting.CaseDefectSnapshotV1("anh-huy.case-defect.v1", Guid.NewGuid(), projectId,
            DateTimeOffset.UtcNow, "", caseFacts, defectFacts.ToArray(), evidenceFacts.ToArray(),
            missing.OrderBy(x => x, StringComparer.Ordinal).ToArray());
        return snapshot with { Hash = ComputeHash(snapshot) };
    }

    private static string ToVersion(byte[]? value)
    {
        return value is null ? "UNAVAILABLE" : Convert.ToBase64String(value);
    }
}
