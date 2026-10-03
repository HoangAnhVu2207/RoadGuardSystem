using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Labels;
using RoadGuardSystem.DTOs.Labels;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Labels;
using RoadGuardSystem.Repositories.Models.Huy01;

namespace RoadGuardSystem.Repositories.Implementations.Labels;

public sealed class TrainingLabelRepository(RoadGuardDbContext db) : ITrainingLabelRepository
{
    public Task<TrainingLabelIdentity?> IdentifyAsync(Guid labelId, CancellationToken token)
        => db.Set<HuyTrainingLabelHead>().AsNoTracking().Where(row => row.Id == labelId)
            .Select(row => new TrainingLabelIdentity(row.Id, row.ProjectId, row.SourceId, row.SourceKind))
            .SingleOrDefaultAsync(token);

    public async Task<TrainingLabelViewDto?> CurrentAsync(Guid projectId, Guid labelId, CancellationToken token)
    {
        var head = await db.Set<HuyTrainingLabelHead>().AsNoTracking()
            .SingleOrDefaultAsync(row => row.Id == labelId && row.ProjectId == projectId, token);
        return head is null ? null : await ProjectAsync(head, token);
    }

    public Task<TrainingLabelViewDto?> ReadAsync(Guid projectId, Guid labelId,
        Func<CancellationToken, Task> guard, CancellationToken token)
        => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
            await guard(token);
            var result = await CurrentAsync(projectId, labelId, token);
            await transaction.CommitAsync(token);
            return result;
        });

    public Task<TrainingLabelPageDto> ListAsync(Guid projectId, int pageSize, Guid? afterId,
        Func<CancellationToken, Task> guard, CancellationToken token)
        => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
            await guard(token);
            var query = db.Set<HuyTrainingLabelHead>().AsNoTracking().Where(row => row.ProjectId == projectId);
            if (afterId is Guid last) query = query.Where(row => row.Id.CompareTo(last) > 0);
            var heads = await query.OrderBy(row => row.Id).Take(pageSize + 1).ToArrayAsync(token);
            var items = new List<TrainingLabelViewDto>();
            foreach (var head in heads.Take(pageSize)) items.Add(await ProjectAsync(head, token));
            await transaction.CommitAsync(token);
            return new TrainingLabelPageDto(items, heads.Length > pageSize && items.Count > 0
                ? Convert.ToBase64String(items[^1].Id.ToByteArray()) : null);
        });

    public async Task<TrainingLabelViewDto> CreateAsync(Guid actor, TrainingLabel label,
        string reason, Guid? correlation, CancellationToken token)
    {
        await RequireActiveTypeAsync(label.CurrentRevision.DefectTypeCode, token);
        var revision = label.CurrentRevision;
        var now = DateTimeOffset.UtcNow;
        var head = new HuyTrainingLabelHead
        {
            Id = label.Id, ProjectId = label.ProjectId, SourceKind = label.SourceKind,
            SourceId = label.SourceId,
            ReportSourceId = label.SourceKind == "REPORT" ? label.SourceId : null,
            AIDetectionSourceId = label.SourceKind == "AI_DETECTION" ? label.SourceId : null,
            CurrentRevision = 1, UpdatedAt = now
        };
        db.Set<HuyTrainingLabelHead>().Add(head);
        db.Set<HuyTrainingLabelRevision>().Add(Row(revision, now));
        Audit(actor, label.Id, "training_label_created", reason, correlation, now);
        await db.SaveChangesAsync(token);
        return await ProjectAsync(head, token);
    }

    public async Task<TrainingLabelViewDto> ReviseAsync(Guid actor, Guid projectId, Guid labelId,
        string expectedVersion, string sourceVersion, Guid fileId, string fileVersion,
        LabelAnnotationDto annotation, string defectTypeCode, string reason,
        Guid? correlation, CancellationToken token)
    {
        var head = await LockHeadAsync(projectId, labelId, expectedVersion, token);
        await RequireActiveTypeAsync(defectTypeCode, token);
        var revision = TrainingLabelRevision.Create(Guid.NewGuid(), labelId, checked(head.CurrentRevision + 1),
            head.SourceId, sourceVersion, fileId, annotation.X, annotation.Y,
            annotation.Width, annotation.Height, defectTypeCode, reason, fileVersion);
        var now = DateTimeOffset.UtcNow;
        db.Set<HuyTrainingLabelRevision>().Add(Row(revision, now));
        head.CurrentRevision = revision.Revision;
        head.UpdatedAt = now;
        Audit(actor, labelId, "training_label_revised", reason, correlation, now);
        await db.SaveChangesAsync(token);
        return await ProjectAsync(head, token);
    }

    public async Task<TrainingLabelViewDto> ReviewAsync(Guid actor, Guid projectId, Guid labelId,
        string expectedVersion, TrainingLabelReviewStatus decision, string reason,
        Guid? correlation, CancellationToken token)
    {
        var head = await LockHeadAsync(projectId, labelId, expectedVersion, token);
        var current = await db.Set<HuyTrainingLabelRevision>().AsNoTracking().SingleAsync(
            row => row.LabelId == labelId && row.Revision == head.CurrentRevision, token);
        if (await db.Set<HuyTrainingLabelReview>().AnyAsync(row => row.RevisionId == current.Id, token))
            throw new CaseWorkflowException(409, "invalid_state_transition");
        var revision = TrainingLabelRevision.Create(current.Id, labelId, current.Revision, head.SourceId,
            current.SourceVersion, current.FileId, current.X, current.Y, current.Width, current.Height,
            current.DefectTypeCode, current.Reason, current.FileVersion);
        var now = DateTimeOffset.UtcNow;
        revision.Review(actor, decision, reason, now);
        db.Set<HuyTrainingLabelReview>().Add(new()
        {
            Id = Guid.NewGuid(), LabelId = labelId, RevisionId = current.Id,
            Decision = decision == TrainingLabelReviewStatus.Approved ? "APPROVED" : "REJECTED",
            Reason = reason, ActorUserId = actor, ReviewedAt = now
        });
        head.UpdatedAt = now;
        Audit(actor, labelId, "training_label_reviewed", reason, correlation, now);
        await db.SaveChangesAsync(token);
        return await ProjectAsync(head, token);
    }

    private async Task<HuyTrainingLabelHead> LockHeadAsync(Guid projectId, Guid labelId,
        string expectedVersion, CancellationToken token)
    {
        var head = await db.Set<HuyTrainingLabelHead>().FromSqlInterpolated(
                $"SELECT * FROM [TrainingLabels] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={labelId}")
            .SingleOrDefaultAsync(token);
        if (head is null || head.ProjectId != projectId) throw new CaseWorkflowException(404, "not_found");
        if (Convert.ToBase64String(head.RowVersion) != expectedVersion)
            throw new CaseWorkflowException(412, "concurrency_conflict");
        return head;
    }

    private async Task<TrainingLabelViewDto> ProjectAsync(HuyTrainingLabelHead head, CancellationToken token)
    {
        var revision = await db.Set<HuyTrainingLabelRevision>().AsNoTracking().SingleAsync(
            row => row.LabelId == head.Id && row.Revision == head.CurrentRevision, token);
        var review = await db.Set<HuyTrainingLabelReview>().AsNoTracking().SingleOrDefaultAsync(
            row => row.RevisionId == revision.Id, token);
        return new(head.Id, head.ProjectId, revision.Revision, review?.Decision ?? "PENDING",
            head.SourceKind, head.SourceId, revision.SourceVersion, revision.FileId, revision.FileVersion,
            new("BBOX", "NORMALIZED", revision.X, revision.Y, revision.Width, revision.Height),
            revision.DefectTypeCode, review?.ActorUserId, review?.ReviewedAt, review?.Reason,
            Convert.ToBase64String(head.RowVersion));
    }

    private static HuyTrainingLabelRevision Row(TrainingLabelRevision revision, DateTimeOffset now) => new()
    {
        Id = revision.Id, LabelId = revision.LabelId, Revision = revision.Revision,
        SourceVersion = revision.SourceVersion, FileId = revision.FileId, FileVersion = revision.FileVersion,
        X = revision.X, Y = revision.Y, Width = revision.Width, Height = revision.Height,
        DefectTypeCode = revision.DefectTypeCode, Reason = revision.Reason, CreatedAt = now
    };

    private async Task RequireActiveTypeAsync(string code, CancellationToken token)
    {
        if (!await db.DefectTypes.AnyAsync(item => item.Code == code && item.IsActive, token))
            throw new CaseWorkflowException(400, "validation_error");
    }

    private void Audit(Guid actor, Guid labelId, string eventType, string reason,
        Guid? correlation, DateTimeOffset now) => db.AuditLogs.Add(AuditLog.Create(
        Guid.NewGuid(), actor, now, eventType, "TrainingLabel", labelId, null,
        JsonSerializer.Serialize(new { labelId }), reason, "huy01.label", correlation, ["labelId"]));
}
