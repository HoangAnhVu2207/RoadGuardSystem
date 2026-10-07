using RoadGuardSystem.Repositories.Labels;
using System.Security.Cryptography;
using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Defects;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Integration;
using RoadGuardSystem.Services.Labels;

namespace RoadGuardSystem.Services.Implementations.Labels;

public sealed class TrainingLabelExportReader(ITrainingLabelRepository labelsRepository, ICaseWorkflowRepository cases,
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
            var rows = await labelsRepository.ReadApprovedFactsAsync(projectId, filters.From, filters.To, filters.DefectIds, ct);
            var labels = new List<ApprovedTrainingLabelV1>(rows.Length);
            foreach (var row in rows)
            {
                if (row.Head.SourceKind == "AI_DETECTION")
                {
                    var ai = aiReaders.SingleOrDefault();
                    if (ai is null) return null;
                    var aiResult = await ai.ResolveAsync(actorId, role, projectId, row.Head.SourceId,
                        cancellationToken: ct);
                    if (aiResult.Status != AnhHuyProducerStatus.Ready) return null;
                    await cases.GuardAsync(actorId, role, [], projectId,
                        async (project, token) => await scope.AuthorizeAsync(actorId, role, project, token) is not null, ct);
                    await candidates.LockAiSourceAsync(row.Head.SourceId, ct);
                    aiResult = await ai.ResolveAsync(actorId, role, projectId, row.Head.SourceId,
                        cancellationToken: ct);
                    if (aiResult.Status != AnhHuyProducerStatus.Ready) return null;
                    var proof = aiResult.Facts!;
                    if (proof.Mode != "MOCK/SYNTHETIC" || row.Revision.SourceVersion != proof.SourceVersion ||
                        row.Revision.FileId != proof.FrameFileId || row.Revision.FileVersion != proof.FrameFileVersion ||
                        row.File.Checksum != proof.FrameSha256 || row.File.SizeBytes != proof.FrameSizeBytes ||
                        row.File.MimeType != proof.FrameMediaType) return null;
                    if (filters.SegmentIds.Length > 0 && (proof.SegmentId is not Guid segment || !filters.SegmentIds.Contains(segment)))
                        continue;
                    labels.Add(new(row.Head.Id, row.Revision.Revision, row.Revision.Id, projectId,
                        row.Revision.DefectTypeCode,
                        new(row.Revision.X, row.Revision.Y, row.Revision.Width, row.Revision.Height),
                        proof.FrameFileId, proof.FrameFileVersion, proof.FrameSha256, proof.FrameSizeBytes,
                        proof.FrameMediaType, "AI_DETECTION", row.Head.SourceId, proof.SourceVersion,
                        row.Review.Id, row.Review.ActorUserId, row.Review.ReviewedAt, proof.JobId,
                        proof.ModelVersionId, proof.DatasetVersionId, "MOCK", proof.SegmentId));
                    continue;
                }
                if (row.Head.SourceKind != "REPORT") return null;
                // REPORT labels have no authoritative per-segment assignment yet.
                if (filters.SegmentIds.Length > 0) continue;
                var result = await producer.ResolveCandidateSourceAsync(actorId, role, projectId,
                    CandidateSourceKind.Report, row.Head.SourceId, cancellationToken: ct);
                if (result.Status != AnhHuyProducerStatus.Ready) return null;
                var facts = result.Facts!;
                await cases.GuardAsync(actorId, role, [facts.CaseId], projectId,
                    async (project, token) => await scope.AuthorizeAsync(actorId, role, project, token) is not null, ct);
                await candidates.LockSourceAsync(row.Head.SourceId, facts.CaseId, ct);
                result = await producer.ResolveCandidateSourceAsync(actorId, role, projectId,
                    CandidateSourceKind.Report, row.Head.SourceId, cancellationToken: ct);
                if (result.Status != AnhHuyProducerStatus.Ready) return null;
                var evidence = result.Facts!.Evidence.FirstOrDefault(item => item.Reference.FileId == row.Revision.FileId &&
                    item.Reference.FileVersion == row.Revision.FileVersion && item.MediaType is "image/jpeg" or "image/png");
                if (evidence is null || evidence.ChecksumSha256 != row.File.Checksum ||
                    evidence.SizeBytes != row.File.SizeBytes || evidence.MediaType != row.File.MimeType)
                    return null;
                labels.Add(new(row.Head.Id, row.Revision.Revision, row.Revision.Id, projectId,
                    row.Revision.DefectTypeCode,
                    new(row.Revision.X, row.Revision.Y, row.Revision.Width, row.Revision.Height),
                    row.Revision.FileId, row.Revision.FileVersion, evidence.ChecksumSha256,
                    evidence.SizeBytes, evidence.MediaType, "REPORT", row.Head.SourceId,
                    row.Revision.SourceVersion, row.Review.Id, row.Review.ActorUserId,
                    row.Review.ReviewedAt, null, null, null, "REAL", null));
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
                var rows = await labelsRepository.ReadFileSourceFactsAsync(projectId, fileIds, ct);
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
        => candidates.ReadConsistentlyAsync(read, token);

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
