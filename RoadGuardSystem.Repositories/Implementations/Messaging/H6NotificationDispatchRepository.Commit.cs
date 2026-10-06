using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Messaging;

namespace RoadGuardSystem.Repositories.Messaging;

public sealed partial class H6NotificationDispatchRepository
{
    public async Task<H6DispatchOutcome> DispatchAsync(H6Claim claim, H6DispatchPlan plan, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return await CommitAsync(claim, async (message, now, token) =>
        {
            if (plan.Source.SchemaVersion != 1 || plan.Source.EventId != message.Id || plan.MessageType != message.MessageType || plan.Source.OccurredAtUtc != message.OccurredAtUtc)
                return await RejectLockedAsync(message, claim, "notification_envelope_invalid", now, token);
            if (plan.AuditOnly != (message.MessageType is "field.task.lifecycle.v1" or "repair.decision.corrected.v1"))
                return await RejectLockedAsync(message, claim, "notification_envelope_invalid", now, token);
            var adapter = sources.SingleOrDefault(source => source.Supports(plan.Source.SourceKind));
            var proof = adapter is null ? new H6SourceResolution("PENDING_PRODUCER", "notification_source_pending_producer")
                : await adapter.ResolveAsync(plan, token);
            if (proof.Status == "PENDING_PRODUCER")
            {
                await AuditAsync(message.Id, null, "PENDING_PRODUCER", "notification_source_pending_producer", now, null, token);
                message.ScheduleRetry(Owner(claim), now.AddMinutes(5), "notification_source_pending_producer", "Source adapter adoption is pending.", int.MaxValue);
                return new("PENDING_PRODUCER", message.Id, ReasonCode: "notification_source_pending_producer");
            }
            if (proof.Status != "VERIFIED")
                return await RejectLockedAsync(message, claim, "notification_source_relation_invalid", now, token);
            if (plan.AuditOnly)
            {
                await AuditAsync(message.Id, plan.Source.ProjectId, "VERIFIED_AUDIT_ONLY", "notification_source_audit_only", now, plan.Source, token);
                Receipt(message, claim, null, "COMMITTED", now); message.CompleteLease(Owner(claim));
                return new("COMMITTED", message.Id);
            }
            if (!MatchesEnvelope(plan))
                return await RejectLockedAsync(message, claim, "notification_envelope_invalid", now, token);
            var envelope = plan.Envelope!;
            var occurrence = envelope.Kind == NotificationEventKind.WeeklyReviewPending
                ? NotificationOccurrence.CreateWeekly(Guid.NewGuid(), envelope, plan.Source.ScheduledAtUtc!.Value)
                : NotificationOccurrence.Create(Guid.NewGuid(), envelope);
            var row = await db.Set<H6NotificationOccurrenceRow>().SingleOrDefaultAsync(existing => existing.OccurrenceKey == occurrence.OccurrenceKey, token);
            if (row is not null)
            {
                if (row.ContentFingerprint != occurrence.ContentFingerprint)
                    return await RejectLockedAsync(message, claim, "notification_occurrence_content_conflict", now, token);
                Receipt(message, claim, row.Id, "COMMITTED", now); message.CompleteLease(Owner(claim));
                return new("COMMITTED", message.Id, row.Id);
            }
            row = new()
            {
                Id = occurrence.Id,
                ProjectId = envelope.ProjectId,
                SourceEventId = message.Id,
                OccurrenceKey = occurrence.OccurrenceKey,
                ContentFingerprint = occurrence.ContentFingerprint,
                EventType = message.MessageType,
                SourceKind = plan.Source.SourceKind,
                SourceId = envelope.SourceId,
                OriginEventId = envelope.OriginEventId,
                PayloadJson = message.PayloadJson,
                OccurredAtUtc = envelope.OccurredAtUtc,
                ScheduledAtUtc = plan.Source.ScheduledAtUtc
            };
            db.Set<H6NotificationOccurrenceRow>().Add(row);
            var effects = await DeliverAsync(row, plan, proof, now, token);
            Receipt(message, claim, row.Id, "COMMITTED", now); message.CompleteLease(Owner(claim));
            return new("COMMITTED", message.Id, row.Id, effects.Delivered, effects.Unresolved);
        }, cancellationToken);
    }
    public Task<H6DispatchOutcome> RejectAsync(H6Claim claim, string reasonCode, CancellationToken cancellationToken)
        => CommitAsync(claim, (message, now, token) => RejectLockedAsync(message, claim, Sanitize(reasonCode), now, token), cancellationToken);
    private async Task<H6DispatchOutcome> CommitAsync(H6Claim claim,
        Func<OutboxMessage, DateTimeOffset, CancellationToken, Task<H6DispatchOutcome>> action, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(claim);
        if (db.Database.CurrentTransaction is not null) throw new InvalidOperationException("Dispatch owns its fenced effect transaction.");
        H6DispatchOutcome result = new("STALE_LEASE", claim.Id);
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear(); var now = clock.GetUtcNow().ToUniversalTime();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
            var message = await db.OutboxMessages.FromSqlInterpolated($"SELECT * FROM [OutboxMessages] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={claim.Id}").SingleOrDefaultAsync(token);
            var receipt = await db.Set<H6NotificationEventReceipt>().AsNoTracking().SingleOrDefaultAsync(row => row.OutboxMessageId == claim.Id, token);
            var payloadHash = Hash(claim.PayloadJson);
            if (receipt is not null && message?.DeliveryStatus == OutboxDeliveryStatus.Completed && receipt.CompletionFence == claim.Fence &&
                receipt.MessageType == claim.MessageType && receipt.PayloadHash == payloadHash)
                result = new(receipt.Status, claim.Id, receipt.OccurrenceId);
            else if (message is not null && message.DeliveryStatus == OutboxDeliveryStatus.Leased && message.LeaseOwner == Owner(claim) &&
                message.LeaseExpiresAtUtc > now && message.LeaseExpiresAtUtc == claim.LeaseExpiresAtUtc && message.DeliveryAttemptCount == claim.Attempt &&
                message.MessageType == claim.MessageType && message.OccurredAtUtc == claim.OccurredAtUtc && message.PayloadJson == claim.PayloadJson)
                result = await action(message, now, token);
            else result = new("STALE_LEASE", claim.Id);
            await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        });
        return result;
    }
    private async Task<H6DispatchOutcome> RejectLockedAsync(OutboxMessage message, H6Claim claim, string reason, DateTimeOffset now, CancellationToken token)
    {
        await AuditAsync(message.Id, null, "REJECTED", Sanitize(reason), now, null, token);
        Receipt(message, claim, null, "REJECTED", now); message.CompleteLease(Owner(claim));
        return new("REJECTED", message.Id, ReasonCode: Sanitize(reason));
    }
    private async Task AuditAsync(Guid messageId, Guid? projectId, string classification, string reason, DateTimeOffset now,
        H6StoredEvent? provenSource, CancellationToken token)
    {
        var key = Hash(messageId.ToString("N") + "|" + NotificationRegisteredTypes.RegistryVersion + "|" + reason);
        if (!await db.Set<H6NotificationAuditRow>().AnyAsync(row => row.DedupKey == key, token))
            db.Set<H6NotificationAuditRow>().Add(new()
            {
                Id = Guid.NewGuid(),
                OutboxMessageId = messageId,
                ProjectId = projectId,
                Classification = classification,
                SourceKind = provenSource?.SourceKind ?? "REGISTERED",
                SourceId = provenSource?.SourceId ?? Guid.Empty,
                ReasonCode = reason,
                ResolverVersion = NotificationRegisteredTypes.RegistryVersion,
                DedupKey = key,
                RecordedAtUtc = now
            });
    }
    private void Receipt(OutboxMessage message, H6Claim claim, Guid? occurrenceId, string status, DateTimeOffset now)
        => db.Set<H6NotificationEventReceipt>().Add(new()
        {
            OutboxMessageId = message.Id,
            CompletionFence = claim.Fence,
            OccurrenceId = occurrenceId,
            MessageType = message.MessageType,
            PayloadHash = Hash(message.PayloadJson),
            Status = status,
            RecordedAtUtc = now
        });
    private static string Owner(H6Claim claim) => "h6:" + claim.Fence.ToString("N");
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static string Sanitize(string reason) => reason is "notification_envelope_invalid" or "notification_event_unregistered" or
        "notification_calendar_period_required" or "notification_calendar_period_invalid" or "notification_source_kind_invalid" or
        "notification_source_action_mismatch" or "notification_source_relation_invalid" or "notification_occurrence_content_conflict"
        ? reason : "notification_envelope_invalid";
    private static bool MatchesEnvelope(H6DispatchPlan plan)
        => plan.Envelope is { } envelope && envelope.EventId == plan.Source.EventId && envelope.ProjectId == plan.Source.ProjectId &&
            envelope.SourceId == plan.Source.SourceId && envelope.OriginEventId == plan.Source.OriginEventId &&
            envelope.SourceRevisionId == plan.Source.SourceRevisionId && envelope.ResponsibleUserId == plan.Source.ResponsibleUserId &&
            envelope.OccurredAtUtc == plan.Source.OccurredAtUtc && envelope.SourceKind.ToString() == plan.Source.SourceKind &&
            envelope.MessageType == plan.MessageType;
}
