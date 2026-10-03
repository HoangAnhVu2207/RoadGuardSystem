using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Services.Integration;

namespace RoadGuardSystem.Services.Implementations.Integration;

// Huy-owned producer. It intentionally returns an incomplete snapshot when the
// current schema cannot prove source/geometry facts; it is not registered here.
public sealed class CaseDefectReadReader(RoadGuardDbContext db) : ICaseDefectReadReader
{
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
            u.Id == actorId && u.Status == UserStatus.Active && u.RoleCode == role && !u.MustChangePassword, cancellationToken)
            && await db.ProjectMembers.AsNoTracking().AnyAsync(m => m.UserId == actorId && m.ProjectId == projectId &&
                m.RoleCode == role && m.Status == ProjectMemberStatus.Active && m.ValidFrom <= today &&
                (m.ValidTo == null || m.ValidTo >= today), cancellationToken);
        if (!authorized) throw new UnauthorizedAccessException("The actor is not authorized for this project.");

        var casesQuery = db.IncidentCases.AsNoTracking().Where(c => c.ProjectId == projectId);
        if (filters.RouteVersionId is Guid route)
            casesQuery = casesQuery.Where(c => EF.Property<Guid?>(c, "GeometryRouteVersionId") == route);
        if (filters.SegmentSetId is Guid segmentSet)
            casesQuery = casesQuery.Where(c => EF.Property<Guid?>(c, "GeometrySegmentSetId") == segmentSet);
        var cases = await casesQuery.ToListAsync(cancellationToken);
        var caseIds = cases.Select(c => c.Id).ToArray();
        var links = await db.Set<HuyCaseReportLink>().AsNoTracking()
            .Where(l => caseIds.Contains(l.CaseId) && l.EndedAt == null).ToListAsync(cancellationToken);
        var reportIds = links.Select(l => l.ReportId).Distinct().ToArray();
        var reports = await db.Reports.AsNoTracking().Where(r => reportIds.Contains(r.Id) &&
            (!filters.From.HasValue || r.ReceivedAt >= filters.From) && (!filters.To.HasValue || r.ReceivedAt < filters.To))
            .ToListAsync(cancellationToken);
        var reportVersions = reports.ToDictionary(r => r.Id, r => VersionOf(db.Entry(r), "RowVersion"));
        var publications = await db.Set<CasePublication>().AsNoTracking().Where(p => caseIds.Contains(p.CaseId)).ToListAsync(cancellationToken);
        var recipients = await db.Set<HuyPublicationRecipient>().AsNoTracking().Where(r => publications.Select(p => p.Id).Contains(r.PublicationId)).ToListAsync(cancellationToken);
        var publicationEvidence = await db.Set<HuyPublicationEvidence>().AsNoTracking().Where(e => publications.Select(p => p.Id).Contains(e.PublicationId)).ToListAsync(cancellationToken);

        var missing = new HashSet<string>(StringComparer.Ordinal);
        var caseFacts = cases.Select(c =>
        {
            var currentReports = links.Where(l => l.CaseId == c.Id).Join(reports, l => l.ReportId, r => r.Id,
                (l, r) => new RoadGuardSystem.DTOs.Reporting.CaseReportRefV1(r.Id, reportVersions[r.Id], r.ReceivedAt)).ToArray();
            var conclusions = c.Conclusions.Select(x => new RoadGuardSystem.DTOs.Reporting.ReportingSourceRefDto("CASE_CONCLUSION", x.Id, "UNAVAILABLE")).ToArray();
            var casePublications = publications.Where(p => p.CaseId == c.Id).Select(p => new RoadGuardSystem.DTOs.Reporting.CasePublicationRefV1(
                p.Id, "UNAVAILABLE", recipients.Where(r => r.PublicationId == p.Id).Select(r => r.ReportId).OrderBy(x => x).ToArray(),
                publicationEvidence.Where(e => e.PublicationId == p.Id).Select(e => e.EvidenceId).OrderBy(x => x).ToArray())).ToArray();
            if (c.Conclusions.Count > 0 || casePublications.Length > 0) missing.Add("case_history_version_unavailable");
            return new RoadGuardSystem.DTOs.Reporting.CaseReadFactV1(c.Id, c.ProjectId!.Value, VersionOf(db.Entry(c), "RowVersion"), c.Status.ToString().ToUpperInvariant(), currentReports, conclusions, casePublications);
        }).ToArray();

        var defects = await db.Defects.AsNoTracking().Where(d => d.ProjectId == projectId).ToListAsync(cancellationToken);
        if (defects.Count > 0) missing.Add("defect_source_and_row_version_unavailable");
        var defectFacts = defects.Select(d => new RoadGuardSystem.DTOs.Reporting.DefectReadFactV1(
            d.Id, d.ProjectId!.Value, "UNAVAILABLE", d.Status.ToString().ToUpperInvariant(), "UNKNOWN", Guid.Empty,
            "UNAVAILABLE", d.RoadSectionVersionId, null, null, null, d.Geometry is null ? "UNAVAILABLE" : "PRESENT"))
            .ToArray();
        missing.Add("defect_geometry_version_unavailable");
        missing.Add("evidence_file_checksum_and_recipient_authority_not_captured");

        var snapshot = new RoadGuardSystem.DTOs.Reporting.CaseDefectSnapshotV1("schema.anh-huy.case-defect.v1", Guid.NewGuid(), projectId,
            DateTimeOffset.UtcNow, "", caseFacts, defectFacts, [], missing.OrderBy(x => x, StringComparer.Ordinal).ToArray());
        var canonical = JsonSerializer.Serialize(snapshot with { Hash = "" });
        return snapshot with { Hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant() };
    }

    private static string VersionOf(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string name)
    {
        var property = entry.Metadata.FindProperty(name);
        if (property is null) return "UNAVAILABLE";
        var value = entry.Property(name).CurrentValue as byte[];
        return value is null ? "UNAVAILABLE" : Convert.ToBase64String(value);
    }
}
