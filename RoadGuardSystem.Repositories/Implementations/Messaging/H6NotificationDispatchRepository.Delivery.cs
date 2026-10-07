using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.BusinessObjects.Repairs;

namespace RoadGuardSystem.Repositories.Messaging;

public sealed partial class H6NotificationDispatchRepository
{
    private sealed record Recipient(string Key, Guid? Actor, UserRoleCode? Role);
    private async Task<(int Delivered, int Unresolved)> DeliverAsync(H6NotificationOccurrenceRow occurrence,
        H6DispatchPlan plan, H6SourceResolution proof, DateTimeOffset now, CancellationToken token)
    {
        var recipients = await RecipientsAsync(plan.Envelope!, proof, now, token); var delivered = 0; var unresolved = 0;
        foreach (var recipient in recipients.OrderBy(row => row.Key, StringComparer.Ordinal))
        {
            var delivery = new H6NotificationDeliveryRow
            {
                Id = Guid.NewGuid(),
                OccurrenceId = occurrence.Id,
                RecipientUserId = recipient.Actor,
                RecipientKey = recipient.Key,
                NextAttemptAtUtc = now.AddMinutes(5)
            };
            var reason = recipient.Actor is Guid actor
                ? await RecipientAuthorityAsync(actor, recipient.Role, occurrence.ProjectId, plan.Envelope!.RecipientStrategy, proof, now, token)
                : "notification_responsible_actor_missing";
            RecordDeliveryAttempt(delivery, occurrence, plan.Envelope!.Kind, reason, now, proof.BodyOverride);
            if (reason is null) delivered++; else unresolved++;
            db.Set<H6NotificationDeliveryRow>().Add(delivery);
        }
        return (delivered, unresolved);
    }
    private async Task<Recipient[]> RecipientsAsync(NotificationEventEnvelope envelope, H6SourceResolution proof,
        DateTimeOffset now, CancellationToken token)
    {
        var day = DateOnly.FromDateTime(now.UtcDateTime);
        var members = db.ProjectMembers.Where(row => row.ProjectId == envelope.ProjectId && row.Status == ProjectMemberStatus.Active &&
            row.ValidFrom <= day && (!row.ValidTo.HasValue || row.ValidTo >= day));
        var results = new List<Recipient>();
        if (envelope.RecipientStrategy == NotificationRecipientStrategy.AssignedCrew)
            results.Add(RecipientFor(proof.ResponsibleUserId, UserRoleCode.RepairCrew, "AssignedCrew"));
        else if (envelope.RecipientStrategy == NotificationRecipientStrategy.ProjectManager)
        {
            var managers = await members.Where(row => row.RoleCode == UserRoleCode.ProjectManager && row.IsPrimary).Select(row => row.UserId).Distinct().ToArrayAsync(token);
            results.Add(RecipientFor(proof.ResponsibleUserId ?? (managers.Length == 1 ? managers[0] : null), UserRoleCode.ProjectManager, "ProjectManager"));
        }
        else if (!proof.ResponsibleIsSupervisor && envelope.RecipientStrategy is (NotificationRecipientStrategy.ResponsibleActorAndSupervisor or NotificationRecipientStrategy.ResponsibleReviewer or NotificationRecipientStrategy.ExplicitResponsibleActors))
            results.Add(RecipientFor(proof.ResponsibleUserId, proof.ResponsibleRole, envelope.RecipientStrategy.ToString()));
        if (envelope.RecipientStrategy is NotificationRecipientStrategy.Supervisor or NotificationRecipientStrategy.ResponsibleActorAndSupervisor ||
            envelope.RecipientStrategy == NotificationRecipientStrategy.ResponsibleReviewer && proof.ResponsibleIsSupervisor)
        {
            var supervisors = envelope.RecipientStrategy == NotificationRecipientStrategy.Supervisor && proof.ResponsibleUserId is Guid appointed
                ? new[] { appointed }
                : await members.Where(row => row.RoleCode == UserRoleCode.Supervisor).Select(row => row.UserId).Distinct().ToArrayAsync(token);
            if (supervisors.Length == 0) results.Add(RecipientFor(null, UserRoleCode.Supervisor, "Supervisor"));
            else foreach (var supervisor in supervisors) results.Add(RecipientFor(supervisor, UserRoleCode.Supervisor, "Supervisor"));
        }
        return results.DistinctBy(row => row.Key).ToArray();
    }
    private static Recipient RecipientFor(Guid? actor, UserRoleCode? role, string missing)
        => new(actor is Guid id ? "actor:" + id.ToString("N") : "pending:" + missing, actor, role);
    private void RecordDeliveryAttempt(H6NotificationDeliveryRow delivery, H6NotificationOccurrenceRow occurrence,
        NotificationEventKind kind, string? reason, DateTimeOffset now, string? bodyOverride = null)
    {
        if (reason is null)
        {
            var content = NotificationMessageContent.For(kind);
            var notification = Notification.Create(Guid.NewGuid(), delivery.RecipientUserId!.Value, occurrence.SourceKind,
                occurrence.SourceId, occurrence.OccurrenceKey, content.Title, bodyOverride ?? content.Body, occurrence.OccurredAtUtc);
            db.Notifications.Add(notification); var auditId = Guid.NewGuid();
            db.Set<H6NotificationAuditRow>().Add(new()
            {
                Id = auditId,
                NotificationId = notification.Id,
                OutboxMessageId = occurrence.SourceEventId,
                ProjectId = occurrence.ProjectId,
                Classification = "PROJECT",
                SourceKind = occurrence.SourceKind,
                SourceId = occurrence.SourceId,
                ReasonCode = "notification_source_verified",
                ResolverVersion = NotificationRegisteredTypes.RegistryVersion,
                DedupKey = Hash(notification.Id.ToString("N") + "|" + NotificationRegisteredTypes.RegistryVersion),
                RecordedAtUtc = now
            });
            db.Set<H6NotificationScopeRow>().Add(new()
            {
                NotificationId = notification.Id,
                ProjectId = occurrence.ProjectId,
                OccurrenceId = occurrence.Id,
                Classification = "PROJECT",
                SourceKind = occurrence.SourceKind,
                SourceId = occurrence.SourceId,
                EventType = occurrence.EventType,
                ResolverVersion = NotificationRegisteredTypes.RegistryVersion,
                CurrentAuditId = auditId
            });
            delivery.Status = "DELIVERED"; delivery.NotificationId = notification.Id; delivery.DeliveredAtUtc = now;
            delivery.ReasonCode = null;
        }
        else { delivery.Status = "UNRESOLVED"; delivery.ReasonCode = reason; }
        delivery.NextAttemptAtUtc = now.AddMinutes(5);
        db.Set<H6NotificationDeliveryAttempt>().Add(new()
        {
            Id = Guid.NewGuid(),
            DeliveryId = delivery.Id,
            ObservedAtUtc = now,
            Status = delivery.Status,
            ReasonCode = delivery.ReasonCode
        });
    }
    private async Task<string?> RecipientAuthorityAsync(Guid actor, UserRoleCode? expectedRole, Guid projectId,
        NotificationRecipientStrategy strategy, H6SourceResolution proof, DateTimeOffset now, CancellationToken token)
    {
        await Anh02ReceiptAuthority.LockAsync(db, actor, projectId, token);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(row => row.Id == actor, token);
        if (user is null || user.Status != UserStatus.Active || user.MustChangePassword) return "notification_user_inactive";
        if (expectedRole.HasValue && user.RoleCode != expectedRole || user.RoleCode == UserRoleCode.Unknown ||
            !await db.Roles.AsNoTracking().AnyAsync(row => row.Code == user.RoleCode && row.IsActive, token)) return "notification_role_inactive";
        var day = DateOnly.FromDateTime(now.UtcDateTime);
        if (!await db.ProjectMembers.AsNoTracking().AnyAsync(row => row.ProjectId == projectId && row.UserId == actor && row.RoleCode == user.RoleCode &&
            row.Status == ProjectMemberStatus.Active && row.ValidFrom <= day && (!row.ValidTo.HasValue || row.ValidTo >= day), token))
            return "notification_membership_lost";
        if (strategy == NotificationRecipientStrategy.AssignedCrew && (proof.AssignmentId is not Guid assignmentId || proof.TaskId is not Guid taskId ||
            !await db.FieldInspectionAssignments.AsNoTracking().AnyAsync(row => row.Id == assignmentId && row.FieldInspectionTaskId == taskId &&
                row.AssignedToUserId == actor && row.Status == FieldInspectionAssignmentStatus.Active && row.EndedAt == null, token)))
            return "notification_assignment_lost";
        if (strategy == NotificationRecipientStrategy.AssignedCrew && proof.BindingId is Guid bindingId &&
            !await db.RepairItems.AsNoTracking().AnyAsync(item => item.ProjectId == projectId && item.CurrentBindingId == bindingId &&
                item.SupersededByItemId == null && db.Set<RepairFieldTaskBinding>().Any(binding => binding.Id == bindingId &&
                    binding.ItemId == item.Id && binding.ProjectId == projectId && binding.TaskId == proof.TaskId &&
                    binding.AssignmentId == proof.AssignmentId && binding.CrewId == actor) &&
                db.RepairObligations.Any(obligation => obligation.Id == item.ObligationId &&
                    obligation.CurrentRepairItemId == item.Id), token))
            return "notification_binding_lost";
        return null;
    }
}
