using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Defects;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Integration;
using RoadGuardSystem.Services.Labels;

namespace RoadGuardSystem.Services.Implementations.Labels;

public sealed class TrainingLabelExportReader(RoadGuardDbContext db, ICaseWorkflowRepository cases,
    ICandidateDecisionRepository candidates, IProjectScopeGuard scope,
    IAnhHuyProducerService producer, IEnumerable<IAiCandidateFactsReader> aiReaders)
    : IApprovedTrainingLabelReader, ITrainingSourceAccessReader
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task<ApprovedLabelSnapshotV1?> CaptureApprovedAsync(Guid actorId, UserRoleCode role, Guid projectId,
        TrainingLabelFilterV1 filters, CancellationToken cancellationToken = default)
        => Consistent(async ct =>
        {
            await AuthorizeAsync(actorId, role, projectId, ct);
            if (filters.SegmentIds is null || filters.DefectIds is null ||
                filters.SegmentIds.Any(id => id == Guid.Empty) || filters.DefectIds.Any(id => id == Guid.Empty) ||
                filters.From is { } from && filters.To is { } to && from >= to)
                return null;
            var query = from head in db.Set<HuyTrainingLabelHead>().AsNoTracking()
                join revision in db.Set<HuyTrainingLabelRevision>().AsNoTracking() on head.Id equals revision.LabelId
                join review in db.Set<HuyTrainingLabelReview>().AsNoTracking() on revision.Id equals review.RevisionId
                join file in db.Files.AsNoTracking() on revision.FileId equals file.Id
                where head.ProjectId == projectId &&
                    head.CurrentRevision == revision.Revision && review.Decision == "APPROVED"
                select new { head, revision, review, file };
            if (filters.From is { } start) query = query.Where(row => row.review.ReviewedAt >= start);
            if (filters.To is { } end) query = query.Where(row => row.review.ReviewedAt < end);
            var rows = await query.OrderBy(row => row.head.Id).ToArrayAsync(ct);
            if (filters.DefectIds.Length > 0)
            {
                var activeSources = await db.Set<HuyDefectSourceLink>().AsNoTracking()
                    .Where(link => filters.DefectIds.Contains(link.DefectId) && link.ProjectId == projectId && link.EndedAt == null)
                    .Select(link => new { link.SourceKind, link.SourceId }).Distinct().ToArrayAsync(ct);
                rows = rows.Where(row => activeSources.Any(link => link.SourceId == row.head.SourceId &&
                    (link.SourceKind == CandidateSourceKind.Report ? "REPORT" : "AI_DETECTION") == row.head.SourceKind)).ToArray();
            }
            var labels = new List<ApprovedTrainingLabelV1>(rows.Length);
            foreach (var row in rows)
            {
                if (row.head.SourceKind == "AI_DETECTION")
                {
                    var ai = aiReaders.SingleOrDefault();
                    if (ai is null) return null;
                    var aiResult = await ai.ResolveAsync(actorId, role, projectId, row.head.SourceId,
                        cancellationToken: ct);
                    if (aiResult.Status != AnhHuyProducerStatus.Ready) return null;
                    await cases.GuardAsync(actorId, role, [], projectId,
                        async (project, token) => await scope.AuthorizeAsync(actorId, role, project, token) is not null, ct);
                    await candidates.LockAiSourceAsync(row.head.SourceId, ct);
                    aiResult = await ai.ResolveAsync(actorId, role, projectId, row.head.SourceId,
                        cancellationToken: ct);
                    if (aiResult.Status != AnhHuyProducerStatus.Ready) return null;
                    var proof = aiResult.Facts!;
                    if (proof.Mode != "MOCK/SYNTHETIC" || row.revision.SourceVersion != proof.SourceVersion ||
                        row.revision.FileId != proof.FrameFileId || row.revision.FileVersion != proof.FrameFileVersion ||
                        row.file.Checksum != proof.FrameSha256 || row.file.SizeBytes != proof.FrameSizeBytes ||
                        row.file.MimeType != proof.FrameMediaType) return null;
                    if (filters.SegmentIds.Length > 0 && (proof.SegmentId is not Guid segment || !filters.SegmentIds.Contains(segment)))
                        continue;
                    labels.Add(new(row.head.Id, row.revision.Revision, row.revision.Id, projectId,
                        row.revision.DefectTypeCode,
                        new(row.revision.X, row.revision.Y, row.revision.Width, row.revision.Height),
                        proof.FrameFileId, proof.FrameFileVersion, proof.FrameSha256, proof.FrameSizeBytes,
                        proof.FrameMediaType, "AI_DETECTION", row.head.SourceId, proof.SourceVersion,
                        row.review.Id, row.review.ActorUserId, row.review.ReviewedAt, proof.JobId,
                        proof.ModelVersionId, proof.DatasetVersionId, "MOCK", proof.SegmentId));
                    continue;
                }
                if (row.head.SourceKind != "REPORT") return null;
                // REPORT labels have no authoritative per-segment assignment yet.
                if (filters.SegmentIds.Length > 0) continue;
                var result = await producer.ResolveCandidateSourceAsync(actorId, role, projectId,
                    CandidateSourceKind.Report, row.head.SourceId, cancellationToken: ct);
                if (result.Status != AnhHuyProducerStatus.Ready) return null;
                var facts = result.Facts!;
                await cases.GuardAsync(actorId, role, [facts.CaseId], projectId,
                    async (project, token) => await scope.AuthorizeAsync(actorId, role, project, token) is not null, ct);
                await candidates.LockSourceAsync(row.head.SourceId, facts.CaseId, ct);
                result = await producer.ResolveCandidateSourceAsync(actorId, role, projectId,
                    CandidateSourceKind.Report, row.head.SourceId, cancellationToken: ct);
                if (result.Status != AnhHuyProducerStatus.Ready) return null;
                var evidence = result.Facts!.Evidence.FirstOrDefault(item => item.Reference.FileId == row.revision.FileId &&
                    item.Reference.FileVersion == row.revision.FileVersion && item.MediaType is "image/jpeg" or "image/png");
                if (evidence is null || evidence.ChecksumSha256 != row.file.Checksum ||
                    evidence.SizeBytes != row.file.SizeBytes || evidence.MediaType != row.file.MimeType)
                    return null;
                labels.Add(new(row.head.Id, row.revision.Revision, row.revision.Id, projectId,
                    row.revision.DefectTypeCode,
                    new(row.revision.X, row.revision.Y, row.revision.Width, row.revision.Height),
                    row.revision.FileId, row.revision.FileVersion, evidence.ChecksumSha256,
                    evidence.SizeBytes, evidence.MediaType, "REPORT", row.head.SourceId,
                    row.revision.SourceVersion, row.review.Id, row.review.ActorUserId,
                    row.review.ReviewedAt, null, null, null, "REAL", null));
            }
            var snapshot = new ApprovedLabelSnapshotV1("anh-huy.approved-label.v1", Guid.NewGuid(), "",
                DateTimeOffset.UtcNow, labels);
            var hash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
                snapshot with { Hash = "" }, Json))).ToLowerInvariant();
            return snapshot with { Hash = hash };
        }, cancellationToken);

    public async Task<bool> CanReadAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid[] fileIds,
        CancellationToken cancellationToken = default)
    {
        if (fileIds is null || fileIds.Any(id => id == Guid.Empty)) return false;
        try
        {
            return await Consistent(async ct =>
            {
                await AuthorizeAsync(actorId, role, projectId, ct);
                // A historical relation locates the resource; neither its approval nor
                // the current label head grants resource access. Check only requested files.
                var rows = await (from head in db.Set<HuyTrainingLabelHead>().AsNoTracking()
                    join revision in db.Set<HuyTrainingLabelRevision>().AsNoTracking() on head.Id equals revision.LabelId
                    join file in db.Files.AsNoTracking() on revision.FileId equals file.Id
                    where head.ProjectId == projectId && fileIds.Contains(revision.FileId)
                    select new { head.SourceKind, head.SourceId, revision.SourceVersion,
                        revision.FileId, revision.FileVersion, file.Checksum, file.SizeBytes, file.MimeType })
                    .ToArrayAsync(ct);
                foreach (var fileId in fileIds.Distinct().Order())
                {
                    var permitted = false;
                    foreach (var row in rows.Where(row => row.FileId == fileId)
                        .OrderBy(row => row.SourceKind).ThenBy(row => row.SourceId))
                    {
                        if (row.SourceKind == "REPORT")
                        {
                            var source = await producer.ResolveCandidateSourceAsync(actorId, role, projectId,
                                CandidateSourceKind.Report, row.SourceId, cancellationToken: ct);
                            if (source.Status != AnhHuyProducerStatus.Ready) continue;
                            await cases.GuardAsync(actorId, role, [source.Facts!.CaseId], projectId,
                                async (project, token) => await scope.AuthorizeAsync(actorId, role, project, token) is not null, ct);
                            await candidates.LockSourceAsync(row.SourceId, source.Facts.CaseId, ct);
                            source = await producer.ResolveCandidateSourceAsync(actorId, role, projectId,
                                CandidateSourceKind.Report, row.SourceId, cancellationToken: ct);
                            if (source.Status != AnhHuyProducerStatus.Ready) continue;
                            permitted = source.Facts!.Evidence.Any(evidence =>
                                evidence.Reference.FileId == fileId && evidence.Reference.FileVersion == row.FileVersion &&
                                evidence.ChecksumSha256 == row.Checksum && evidence.SizeBytes == row.SizeBytes &&
                                evidence.MediaType == row.MimeType && evidence.MediaType is "image/jpeg" or "image/png");
                        }
                        else if (row.SourceKind == "AI_DETECTION")
                        {
                            var ai = aiReaders.SingleOrDefault();
                            if (ai is null) continue;
                            await candidates.LockAiSourceAsync(row.SourceId, ct);
                            var source = await ai.ResolveAsync(actorId, role, projectId, row.SourceId,
                                cancellationToken: ct);
                            if (source.Status != AnhHuyProducerStatus.Ready) continue;
                            var proof = source.Facts!;
                            permitted = proof.Mode == "MOCK/SYNTHETIC" && proof.SourceVersion == row.SourceVersion &&
                                proof.FrameFileId == fileId && proof.FrameFileVersion == row.FileVersion &&
                                proof.FrameSha256 == row.Checksum && proof.FrameSizeBytes == row.SizeBytes &&
                                proof.FrameMediaType == row.MimeType;
                        }
                        if (permitted) break;
                    }
                    if (!permitted) return false;
                }
                return true;
            }, cancellationToken);
        }
        catch (UnauthorizedAccessException) { return false; }
        catch (CaseWorkflowException) { return false; }
    }

    private Task<T> Consistent<T>(Func<CancellationToken, Task<T>> read, CancellationToken token)
    {
        if (db.Database.CurrentTransaction is { } current)
        {
            if (current.GetDbTransaction().IsolationLevel is not (IsolationLevel.Serializable or IsolationLevel.Snapshot))
                throw new InvalidOperationException("Approved labels require SERIALIZABLE or SNAPSHOT admission isolation.");
            return read(token);
        }
        return db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
            var result = await read(token);
            await transaction.CommitAsync(token);
            return result;
        });
    }

    private async Task AuthorizeAsync(Guid actor, UserRoleCode role, Guid project, CancellationToken token)
    {
        if (role != UserRoleCode.ProjectManager) throw new UnauthorizedAccessException("A current project PM is required.");
        try
        {
            await cases.GuardAsync(actor, role, [], project,
                async (id, ct) => await scope.AuthorizeAsync(actor, role, id, ct) is not null, token);
        }
        catch (CaseWorkflowException error) when (error.Status is 403 or 404)
        {
            throw new UnauthorizedAccessException("A current project PM is required.", error);
        }
    }
}
