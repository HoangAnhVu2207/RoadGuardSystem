using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.DTOs.Retention;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Repositories.Retention;

namespace RoadGuardSystem.Repositories.Implementations.Retention;

public sealed class Huy01RetentionInventoryContributor(RoadGuardDbContext db) : IRetentionInventoryContributor
{
    public string Name => "HUY";

    public async Task<RetentionInventoryContribution> ReadAsync(Guid fileId, CancellationToken token)
    {
        var originals = await db.Reports.AsNoTracking().SelectMany(r => r.OriginalEvidence)
            .Where(e => e.FileId == fileId).Select(e => new { e.Id, e.ReportId, e.FileVersion }).ToArrayAsync(token);
        var supplements = await db.Set<ReportSupplement>().AsNoTracking().SelectMany(s => s.Evidence)
            .Where(e => e.FileId == fileId).Select(e => new { e.Id, e.ReportId, e.FileVersion }).ToArrayAsync(token);
        var evidenceIds = originals.Concat(supplements).Select(e => e.Id).Distinct().ToArray();
        var reportIds = originals.Concat(supplements).Select(e => e.ReportId).Distinct().ToArray();
        var references = new List<RetentionReferenceView>();

        var reports = await db.Reports.AsNoTracking().Where(report => reportIds.Contains(report.Id))
            .Select(report => new { report.Id, report.ReceivedAt, Revision = EF.Property<long>(report, "Revision"),
                RowVersion = EF.Property<byte[]>(report, "RowVersion") }).ToArrayAsync(token);
        references.AddRange(reports.Select(row => new RetentionReferenceView("REPORT", row.Id, null,
            Version(new { row.Id, row.ReceivedAt, row.Revision, RowVersion = Convert.ToBase64String(row.RowVersion) }))));
        references.AddRange(originals.Select(row => new RetentionReferenceView("REPORT_EVIDENCE", row.Id, null,
            Version(new { row.Id, row.ReportId, row.FileVersion }))));
        references.AddRange(supplements.Select(row => new RetentionReferenceView("REPORT_SUPPLEMENT_EVIDENCE", row.Id, null,
            Version(new { row.Id, row.ReportId, row.FileVersion }))));

        var supplementRows = await db.Set<ReportSupplement>().AsNoTracking()
            .Where(s => s.Evidence.Any(e => e.FileId == fileId))
            .Select(s => new { s.Id, s.ReportId, s.ReceivedAt }).ToArrayAsync(token);
        references.AddRange(supplementRows.Select(row => new RetentionReferenceView("REPORT_SUPPLEMENT", row.Id, null,
            Version(row))));

        var links = await (from link in db.Set<HuyCaseReportLink>().AsNoTracking()
            join incident in db.IncidentCases.AsNoTracking() on link.CaseId equals incident.Id
            where reportIds.Contains(link.ReportId)
            select new { link.Id, link.ReportId, link.CaseId, link.StartedAt, link.EndedAt, incident.ProjectId,
                CaseRevision = EF.Property<long>(incident, "Revision"), CaseVersion = EF.Property<byte[]>(incident, "RowVersion") }).ToArrayAsync(token);
        references.AddRange(links.Select(row => new RetentionReferenceView("CASE_REPORT_LINK", row.Id, row.ProjectId,
            Version(new { row.ReportId, row.CaseId, row.StartedAt, row.EndedAt, row.ProjectId, row.CaseRevision,
                CaseVersion = Convert.ToBase64String(row.CaseVersion) }))));

        var conclusions = await (from relation in db.Set<HuyConclusionEvidence>().AsNoTracking()
            join conclusion in db.Set<CaseConclusion>().AsNoTracking() on relation.ConclusionId equals conclusion.Id
            join incident in db.IncidentCases.AsNoTracking() on EF.Property<Guid>(conclusion, "CaseId") equals incident.Id
            where evidenceIds.Contains(relation.EvidenceId)
            select new { relation.ConclusionId, relation.EvidenceId, relation.SourceReportId,
                incident.ProjectId, conclusion.Outcome, conclusion.ConcludedAt }).ToArrayAsync(token);
        references.AddRange(conclusions.Select(row => new RetentionReferenceView("CASE_CONCLUSION_EVIDENCE", row.ConclusionId,
            row.ProjectId, Version(new { row.ConclusionId, row.EvidenceId, row.SourceReportId, row.ProjectId, row.Outcome, row.ConcludedAt }))));

        var publications = await (from relation in db.Set<HuyPublicationEvidence>().AsNoTracking()
            join publication in db.Set<CasePublication>().AsNoTracking() on relation.PublicationId equals publication.Id
            join incident in db.IncidentCases.AsNoTracking() on publication.CaseId equals incident.Id
            where evidenceIds.Contains(relation.EvidenceId)
            select new { relation.PublicationId, relation.RecipientReportId, relation.EvidenceId, relation.SourceReportId,
                incident.ProjectId, publication.PublishedAt }).ToArrayAsync(token);
        references.AddRange(publications.Select(row => new RetentionReferenceView("CASE_PUBLICATION_EVIDENCE", row.PublicationId,
            row.ProjectId, Version(new { row.PublicationId, row.RecipientReportId, row.EvidenceId, row.SourceReportId, row.ProjectId, row.PublishedAt }))));

        var decisions = await db.SourceDecisions.AsNoTracking()
            .Where(d => d.Source.Kind == CandidateSourceKind.Report && reportIds.Contains(d.Source.Id))
            .Select(d => new { d.Id, d.ProjectId, SourceId = d.Source.Id, SourceVersion = d.Source.SourceVersion,
                d.Decision, d.SupersedesDecisionId, RowVersion = EF.Property<byte[]>(d, "RowVersion") }).ToArrayAsync(token);
        references.AddRange(decisions.Select(row => new RetentionReferenceView("CANDIDATE_DECISION", row.Id, row.ProjectId,
            Version(new { row.SourceId, row.SourceVersion, row.Decision, row.SupersedesDecisionId,
                RowVersion = Convert.ToBase64String(row.RowVersion) }))));

        var sourceLinks = await (from link in db.Set<HuyDefectSourceLink>().AsNoTracking()
            join evidence in db.Reports.AsNoTracking().SelectMany(report => report.OriginalEvidence)
                on link.ReportSourceId equals evidence.ReportId
            where evidence.FileId == fileId
            select new { link.Id, link.ProjectId, link.SourceKind, link.SourceId, link.DefectId,
                link.DecisionId, link.CreatedAt, link.EndedAt }).Distinct().ToArrayAsync(token);
        references.AddRange(sourceLinks.Select(row => new RetentionReferenceView("DEFECT_SOURCE_LINK", row.Id,
            row.ProjectId, Version(row))));
        var supplementalSourceLinks = await (from link in db.Set<HuyDefectSourceLink>().AsNoTracking()
            join evidence in db.Set<ReportSupplement>().AsNoTracking().SelectMany(s => s.Evidence)
                on link.ReportSourceId equals evidence.ReportId
            where evidence.FileId == fileId
            select new { link.Id, link.ProjectId, link.SourceKind, link.SourceId, link.DefectId,
                link.DecisionId, link.CreatedAt, link.EndedAt }).Distinct().ToArrayAsync(token);
        references.AddRange(supplementalSourceLinks.Where(row => sourceLinks.All(existing => existing.Id != row.Id))
            .Select(row => new RetentionReferenceView("DEFECT_SOURCE_LINK", row.Id, row.ProjectId, Version(row))));

        var revisions = await (from revision in db.Set<HuyTrainingLabelRevision>().AsNoTracking()
            join head in db.Set<HuyTrainingLabelHead>().AsNoTracking() on revision.LabelId equals head.Id
            where revision.FileId == fileId
            select new { revision.Id, revision.LabelId, revision.Revision, revision.SourceVersion,
                revision.FileVersion, revision.DefectTypeCode, revision.CreatedAt,
                head.ProjectId, head.SourceKind, head.SourceId }).ToArrayAsync(token);
        references.AddRange(revisions.Select(row => new RetentionReferenceView("TRAINING_LABEL_REVISION",
            row.Id, row.ProjectId, Version(new { row.LabelId, row.Revision, row.SourceVersion,
                row.FileVersion, row.DefectTypeCode, row.CreatedAt, row.SourceKind, row.SourceId }))));
        var revisionIds = revisions.Select(row => row.Id).ToArray();
        var reviews = await db.Set<HuyTrainingLabelReview>().AsNoTracking()
            .Where(row => revisionIds.Contains(row.RevisionId))
            .Select(row => new { row.Id, row.RevisionId, row.Decision, row.ReviewedAt, row.ActorUserId })
            .ToArrayAsync(token);
        references.AddRange(reviews.Select(row => new RetentionReferenceView("TRAINING_LABEL_REVIEW", row.Id,
            revisions.Single(revision => revision.Id == row.RevisionId).ProjectId, Version(row))));

        var histories = await (from relation in db.Set<HuyLinkHistoryReport>().AsNoTracking()
            join history in db.Set<CaseReportLinkHistory>().AsNoTracking() on relation.HistoryId equals history.Id
            where reportIds.Contains(relation.ReportId)
            select new { relation.HistoryId, relation.ReportId, history.FromCaseId, history.ToCaseId, history.OccurredAt }).ToArrayAsync(token);
        references.AddRange(histories.Select(row => new RetentionReferenceView("CASE_LINK_HISTORY", row.HistoryId, null,
            Version(row))));

        var ordered = references.OrderBy(r => r.Kind, StringComparer.Ordinal).ThenBy(r => r.Id)
            .ThenBy(r => r.ProjectId).ThenBy(r => r.SourceVersion, StringComparer.Ordinal).ToArray();
        return new(Name, false, ordered, ["HUY_REPAIR_REFERENCE_UNAVAILABLE"]);
    }

    public async Task<IReadOnlyList<Guid>> KnownProjectFilesAsync(Guid projectId, CancellationToken token)
    {
        var reports = db.Reports.AsNoTracking().Where(report => db.Set<HuyCaseReportLink>()
            .Any(link => link.ReportId == report.Id && db.IncidentCases.Any(incident => incident.Id == link.CaseId && incident.ProjectId == projectId)));
        var original = await reports.SelectMany(report => report.OriginalEvidence).Select(e => e.FileId).ToArrayAsync(token);
        var supplemental = await reports.SelectMany(report => report.Supplements).SelectMany(s => s.Evidence)
            .Select(e => e.FileId).ToArrayAsync(token);
        var labels = await db.Set<HuyTrainingLabelRevision>().AsNoTracking()
            .Where(revision => db.Set<HuyTrainingLabelHead>().Any(head => head.Id == revision.LabelId && head.ProjectId == projectId))
            .Select(revision => revision.FileId).ToArrayAsync(token);
        return original.Concat(supplemental).Concat(labels).Distinct().Order().ToArray();
    }

    private static string Version(object value)
        => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value))).ToLowerInvariant();
}
