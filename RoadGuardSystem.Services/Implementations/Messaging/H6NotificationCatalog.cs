using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.DTOs.Messaging;
using RoadGuardSystem.Repositories.Messaging;
using System.Text.Json;

namespace RoadGuardSystem.Services.Messaging;

public sealed class H6NotificationProtocolException(string code) : Exception(code)
{
    public string Code { get; } = code;
}
public static class H6NotificationCatalog
{
    public const string Version = H6NotificationProtocolTypes.RegistryVersion;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly Dictionary<string, (NotificationEventKind Kind, string Action)> Entries =
        new Dictionary<string, (NotificationEventKind, string)>(StringComparer.Ordinal)
        {
            ["field.task.assigned.v1"] = (NotificationEventKind.FieldAssigned, "ASSIGNED"),
            ["field.task.submitted.v1"] = (NotificationEventKind.FieldSubmitted, "SUBMITTED"),
            ["field.task.supplement_requested.v1"] = (NotificationEventKind.FieldSupplementRequested, "SUPPLEMENT"),
            ["repair.work.assigned.v1"] = (NotificationEventKind.RepairAssigned, "ASSIGNED"),
            ["repair.work.submitted.v1"] = (NotificationEventKind.RepairSubmitted, "SUBMITTED"),
            ["repair.work.rework_requested.v1"] = (NotificationEventKind.RepairReworkRequested, "REWORK"),
            ["repair.work.fast_track_confirmed.v1"] = (NotificationEventKind.FastTrackConfirmedInformation, "FAST_TRACK_CONFIRMED"),
            ["review.supervisor_required.v1"] = (NotificationEventKind.SupervisorApprovalRequired, "SUPERVISOR_REQUIRED"),
            ["deadline.breached.v1"] = (NotificationEventKind.DeadlineBreached, "BREACHED"),
            ["safety.warning.v1"] = (NotificationEventKind.SafetyWarning, "WARNING"),
            ["safety.measure_assigned.v1"] = (NotificationEventKind.SafetyMeasureAssigned, "SAFETY_ASSIGNED"),
            ["safety.inspection_due.v1"] = (NotificationEventKind.SafetyInspectionDue, "INSPECTION_DUE"),
            ["review.weekly_pending.v1"] = (NotificationEventKind.WeeklyReviewPending, "WEEKLY_PENDING"),
            ["project.obligation_transferred.v1"] = (NotificationEventKind.ProjectObligationTransferred, "OBLIGATION_TRANSFERRED"),
            ["project.handling_renewed.v1"] = (NotificationEventKind.ProjectHandlingRenewed, "HANDLING_RENEWED")
        };
    public static IReadOnlyList<string> MessageTypes => H6NotificationProtocolTypes.All;
    public static H6DispatchPlan Parse(Guid outboxId, string messageType, DateTimeOffset occurredAtUtc, string payloadJson)
    {
        if (!MessageTypes.Contains(messageType)) throw new H6NotificationProtocolException("notification_event_unregistered");
        try
        {
            if (payloadJson is null || payloadJson.Length > 65536) throw new H6NotificationProtocolException("notification_envelope_invalid");
            using var document = JsonDocument.Parse(payloadJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new H6NotificationProtocolException("notification_envelope_invalid");
            var propertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in document.RootElement.EnumerateObject())
                if (!propertyNames.Add(property.Name)) throw new H6NotificationProtocolException("notification_envelope_invalid");
            if (messageType == "repair.decision.corrected.v1") return ParseCorrection(outboxId, occurredAtUtc, payloadJson);
            var wire = JsonSerializer.Deserialize<H6NotificationEventDto>(payloadJson, Json)
                ?? throw new H6NotificationProtocolException("notification_envelope_invalid");
            var source = new H6StoredEvent(wire.SchemaVersion, wire.EventId, wire.Kind, wire.ProjectId,
                wire.SourceKind, wire.SourceId, wire.OriginEventId, wire.OccurredAtUtc,
                wire.SourceRevisionId, wire.ResponsibleUserId, wire.ScheduledAtUtc);
            if (source.SchemaVersion != 1 || outboxId == Guid.Empty || source.EventId != outboxId ||
                source.ProjectId == Guid.Empty || source.SourceId == Guid.Empty || source.OriginEventId == Guid.Empty ||
                source.OccurredAtUtc == default || source.OccurredAtUtc.Offset != TimeSpan.Zero || source.OccurredAtUtc != occurredAtUtc ||
                source.SourceRevisionId == Guid.Empty || source.ResponsibleUserId == Guid.Empty)
                throw new H6NotificationProtocolException("notification_envelope_invalid");
            if (messageType == "field.task.lifecycle.v1")
            {
                if (source.SourceKind != "FieldTask" || source.Kind is not ("ACCEPTED" or "REJECTED" or "STARTED" or "REVIEWED" or "CANCELLED" or "IMPACT_CONTINUE" or "IMPACT_VERIFY") || source.ScheduledAtUtc is not null)
                    throw new H6NotificationProtocolException("notification_source_action_mismatch");
                return new(source, null, messageType, "", "", true);
            }
            var entry = Entries[messageType];
            if (source.Kind != entry.Action && !(entry.Kind == NotificationEventKind.FieldAssigned && source.Kind == "REASSIGNED"))
                throw new H6NotificationProtocolException("notification_source_action_mismatch");
            if (!Enum.TryParse<NotificationSourceKind>(source.SourceKind, false, out var sourceKind)
                || !Enum.IsDefined(sourceKind) || Enum.GetName(sourceKind) != source.SourceKind)
                throw new H6NotificationProtocolException("notification_source_kind_invalid");
            var envelope = NotificationEventEnvelope.Create(outboxId, entry.Kind, source.ProjectId, sourceKind,
                source.SourceId, source.OriginEventId, source.OccurredAtUtc, source.SourceRevisionId, source.ResponsibleUserId);
            if (entry.Kind == NotificationEventKind.WeeklyReviewPending)
            {
                if (source.ScheduledAtUtc is not DateTimeOffset scheduled) throw new H6NotificationProtocolException("notification_calendar_period_required");
                NotificationOccurrence.CreateWeekly(Guid.NewGuid(), envelope, scheduled);
            }
            else if (source.ScheduledAtUtc is not null) throw new H6NotificationProtocolException("notification_calendar_period_invalid");
            var content = NotificationMessageContent.For(entry.Kind);
            return new(source, envelope, messageType, content.Item1, content.Item2, false);
        }
        catch (JsonException) { throw new H6NotificationProtocolException("notification_envelope_invalid"); }
        catch (ArgumentException) { throw new H6NotificationProtocolException("notification_envelope_invalid"); }
    }
    private static H6DispatchPlan ParseCorrection(Guid outboxId, DateTimeOffset occurredAtUtc, string payloadJson)
    {
        var correction = JsonSerializer.Deserialize<H6RepairCorrectionAuditDto>(payloadJson, Json)
            ?? throw new H6NotificationProtocolException("notification_envelope_invalid");
        if (correction.SchemaVersion != 1 || outboxId == Guid.Empty || correction.EventId != outboxId ||
            correction.OriginEventId != outboxId || correction.SourceRevisionId != outboxId ||
            correction.ProjectId == Guid.Empty || correction.SourceId == Guid.Empty || correction.ObligationId == Guid.Empty ||
            correction.SupersedesDecisionId == Guid.Empty || correction.SupersedesDecisionId == outboxId ||
            correction.Kind != "CORRECTED" || correction.SourceKind != "RepairWork" ||
            correction.OccurredAtUtc == default || correction.OccurredAtUtc.Offset != TimeSpan.Zero || correction.OccurredAtUtc != occurredAtUtc ||
            correction.Result is not ("UNREPAIRED" or "REPORTED_AWAITING_REVIEW" or "CONFIRMED"))
            throw new H6NotificationProtocolException("notification_envelope_invalid");
        var source = new H6StoredEvent(1, correction.EventId, correction.Kind, correction.ProjectId,
            correction.SourceKind, correction.SourceId, correction.OriginEventId, correction.OccurredAtUtc, correction.SourceRevisionId);
        // Parsing authenticates no source row. The actual adapter must prove immutable correction
        // history before audit receipt completion; this recognized event never creates inbox fanout.
        return new(source, null, "repair.decision.corrected.v1", "", "", true,
            new H6StoredCorrectionFacts(correction.ObligationId, correction.SupersedesDecisionId, correction.Result));
    }
}
