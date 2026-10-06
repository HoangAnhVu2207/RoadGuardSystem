using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Messaging;

namespace RoadGuardSystem.Repositories.Messaging;

public sealed partial class H6NotificationDispatchRepository
{
    public async Task<int> RetryUnresolvedAsync(CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is not null) throw new InvalidOperationException("Delivery retry owns its effect transaction.");
        var delivered = 0;
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            delivered = 0; db.ChangeTracker.Clear(); var now = clock.GetUtcNow().ToUniversalTime();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var rows = await db.Set<H6NotificationDeliveryRow>().FromSqlInterpolated($"""
                SELECT TOP (100) * FROM [H6NotificationDeliveries] WITH (UPDLOCK,HOLDLOCK)
                WHERE [Status]='UNRESOLVED' AND [NextAttemptAtUtc]<={now}
                ORDER BY [NextAttemptAtUtc],[Id]
                """).ToArrayAsync(cancellationToken);
            foreach (var delivery in rows)
            {
                var occurrence = await db.Set<H6NotificationOccurrenceRow>().SingleAsync(row => row.Id == delivery.OccurrenceId, cancellationToken);
                var plan = RetryPlan(occurrence); var adapter = sources.SingleOrDefault(source => source.Supports(occurrence.SourceKind));
                var proof = plan is null || adapter is null ? new H6SourceResolution("PENDING_PRODUCER")
                    : await adapter.ResolveAsync(plan, cancellationToken);
                var reason = proof.Status == "VERIFIED" ? null : proof.Status == "PENDING_PRODUCER"
                    ? "notification_source_pending_producer" : "notification_source_relation_invalid";
                if (reason is null)
                {
                    var eligible = await RecipientsAsync(plan!.Envelope!, proof, now, cancellationToken);
                    var recipient = delivery.RecipientUserId is Guid actor
                        ? eligible.SingleOrDefault(row => row.Actor == actor)
                        : ResolveMissingRecipient(delivery.RecipientKey, eligible, proof);
                    if (recipient?.Actor is not Guid currentActor) reason = "notification_responsibility_lost";
                    else
                    {
                        // A missing duty can be resolved once. A previously pinned individual is
                        // never replaced by a new assignee/manager merely to make a retry succeed.
                        delivery.RecipientUserId ??= currentActor;
                        reason = await RecipientAuthorityAsync(currentActor, recipient.Role, occurrence.ProjectId,
                            plan.Envelope!.RecipientStrategy, proof, now, cancellationToken);
                    }
                }
                if (plan is null)
                {
                    delivery.ReasonCode = reason; delivery.NextAttemptAtUtc = now.AddMinutes(5);
                    db.Set<H6NotificationDeliveryAttempt>().Add(new() { Id = Guid.NewGuid(), DeliveryId = delivery.Id,
                        ObservedAtUtc = now, Status = delivery.Status, ReasonCode = reason });
                }
                else { RecordDeliveryAttempt(delivery, occurrence, plan.Envelope!.Kind, reason, now); if (reason is null) delivered++; }
            }
            await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
        });
        return delivered;
    }
    private static Recipient? ResolveMissingRecipient(string key, Recipient[] eligible, H6SourceResolution proof)
    {
        IEnumerable<Recipient> candidates = key switch
        {
            "pending:AssignedCrew" => eligible.Where(row => row.Actor is not null && row.Actor == proof.ResponsibleUserId),
            "pending:ProjectManager" => eligible.Where(row => row.Actor is not null && row.Role == UserRoleCode.ProjectManager),
            "pending:Supervisor" => eligible.Where(row => row.Actor is not null && row.Role == UserRoleCode.Supervisor),
            "pending:ResponsibleActorAndSupervisor" or "pending:ResponsibleReviewer" or "pending:ExplicitResponsibleActors"
                => eligible.Where(row => row.Actor is not null && row.Actor == proof.ResponsibleUserId),
            _ => []
        };
        var resolved = candidates.Take(2).ToArray();
        return resolved.Length == 1 ? resolved[0] : null;
    }
    private static H6DispatchPlan? RetryPlan(H6NotificationOccurrenceRow occurrence)
    {
        try
        {
            var source = JsonSerializer.Deserialize<H6StoredEvent>(occurrence.PayloadJson, ClockJson);
            if (source is null || source.SchemaVersion != 1 || source.EventId != occurrence.SourceEventId ||
                source.ProjectId != occurrence.ProjectId || source.SourceId != occurrence.SourceId || source.OriginEventId != occurrence.OriginEventId ||
                source.SourceKind != occurrence.SourceKind || source.OccurredAtUtc != occurrence.OccurredAtUtc ||
                !Enum.TryParse<NotificationSourceKind>(source.SourceKind, out var sourceKind) || Enum.GetName(sourceKind) != source.SourceKind) return null;
            var kind = occurrence.EventType switch
            {
                "field.task.assigned.v1" => NotificationEventKind.FieldAssigned,
                "field.task.submitted.v1" => NotificationEventKind.FieldSubmitted,
                "field.task.supplement_requested.v1" => NotificationEventKind.FieldSupplementRequested,
                "repair.work.assigned.v1" => NotificationEventKind.RepairAssigned,
                "repair.work.submitted.v1" => NotificationEventKind.RepairSubmitted,
                "repair.work.rework_requested.v1" => NotificationEventKind.RepairReworkRequested,
                "repair.work.fast_track_confirmed.v1" => NotificationEventKind.FastTrackConfirmedInformation,
                "review.supervisor_required.v1" => NotificationEventKind.SupervisorApprovalRequired,
                "deadline.breached.v1" => NotificationEventKind.DeadlineBreached,
                "safety.warning.v1" => NotificationEventKind.SafetyWarning,
                "safety.measure_assigned.v1" => NotificationEventKind.SafetyMeasureAssigned,
                "safety.inspection_due.v1" => NotificationEventKind.SafetyInspectionDue,
                "review.weekly_pending.v1" => NotificationEventKind.WeeklyReviewPending,
                "project.obligation_transferred.v1" => NotificationEventKind.ProjectObligationTransferred,
                "project.handling_renewed.v1" => NotificationEventKind.ProjectHandlingRenewed,
                _ => (NotificationEventKind)0
            };
            if (!Enum.IsDefined(kind)) return null;
            var envelope = NotificationEventEnvelope.Create(source.EventId, kind, source.ProjectId, sourceKind, source.SourceId,
                source.OriginEventId, source.OccurredAtUtc, source.SourceRevisionId, source.ResponsibleUserId);
            if (source.ScheduledAtUtc != occurrence.ScheduledAtUtc) return null;
            var content = NotificationMessageContent.For(kind);
            return new(source, envelope, occurrence.EventType, content.Title, content.Body, false);
        }
        catch (JsonException) { return null; }
        catch (ArgumentException) { return null; }
    }
}
