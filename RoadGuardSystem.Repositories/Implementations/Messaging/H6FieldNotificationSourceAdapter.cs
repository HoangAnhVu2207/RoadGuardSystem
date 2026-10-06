using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Messaging;

namespace RoadGuardSystem.Repositories.Messaging;

public sealed class H6FieldNotificationSourceAdapter(RoadGuardDbContext db) : IH6NotificationSourceAdapter
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public bool Supports(string sourceKind) => sourceKind == "FieldTask";
    public IQueryable<H6SourceScope> ScopeQuery()
        => from task in db.FieldInspectionTasks
           join assignment in db.FieldInspectionAssignments.Where(row => row.Status == FieldInspectionAssignmentStatus.Active && row.EndedAt == null)
               on task.Id equals assignment.FieldInspectionTaskId into assignments
           from assignment in assignments.DefaultIfEmpty()
           select new H6SourceScope
           {
               SourceKind = "FieldTask",
               SourceId = task.Id,
               ProjectId = task.ProjectId,
               AssignedUserId = assignment == null ? null : assignment.AssignedToUserId
           };
    public async Task<H6SourceResolution> ResolveAsync(H6DispatchPlan plan, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Source proof requires the caller's transaction.");
        if (!Supports(plan.Source.SourceKind)) return new("REJECTED", "notification_source_kind_invalid");
        var source = plan.Source;
        // Transport identity may differ from the business origin only when an actual durable
        // outbox envelope binds every claimed source fact. Caller DTOs are not source evidence.
        var transport = await db.OutboxMessages.FromSqlInterpolated($"SELECT * FROM [OutboxMessages] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={source.EventId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (transport is null || !MatchesTransport(transport, plan))
            return new("REJECTED", "notification_transport_relation_invalid");
        var task = await db.FieldInspectionTasks.FromSqlInterpolated($"SELECT * FROM [FieldInspectionTasks] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={source.SourceId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        var sourceEvent = await db.FieldInspectionTaskEvents.FromSqlInterpolated($"SELECT * FROM [FieldInspectionTaskEvents] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={source.OriginEventId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (task is null || sourceEvent is null) return new("REJECTED", "notification_source_not_found");
        var assignment = sourceEvent.AssignmentId is Guid assignmentId
            ? await db.FieldInspectionAssignments.FromSqlInterpolated($"SELECT * FROM [FieldInspectionAssignments] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={assignmentId}")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken) : null;
        FieldInspectionSubmission? submission = null; FieldInspectionReview? review = null;
        if (source.SourceRevisionId is Guid revisionId)
        {
            submission = await db.FieldInspectionSubmissions.FromSqlInterpolated($"SELECT * FROM [FieldInspectionSubmissions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={revisionId}")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            if (source.Kind is "SUPPLEMENT" or "REVIEWED")
                review = await db.FieldInspectionReviews.AsNoTracking().Where(row => row.SubmissionId == revisionId &&
                    row.TaskId == task.Id && row.ProjectId == task.ProjectId && row.ActorId == sourceEvent.ActorId && row.OccurredAt == sourceEvent.OccurredAt &&
                    (source.Kind == "SUPPLEMENT" ? row.Decision == "SUPPLEMENT" : row.Decision == "CONFIRM" || row.Decision == "NO_DEFECT"))
                    .OrderBy(row => row.Id).FirstOrDefaultAsync(cancellationToken);
        }
        var claim = new NotificationFieldSourceClaim(source.EventId, source.OriginEventId, source.ProjectId,
            source.SourceId, source.Kind, source.OccurredAtUtc, source.SourceRevisionId, source.ResponsibleUserId);
        return NotificationFieldSourceProof.Verify(claim, task, sourceEvent, assignment, submission, review)
            ? new("VERIFIED", TaskId: task.Id, AssignmentId: assignment?.Id, ResponsibleUserId: source.ResponsibleUserId,
                ResponsibleRole: source.Kind == "SUPPLEMENT" ? UserRoleCode.RepairCrew : null)
            : new("REJECTED", "notification_source_relation_invalid");
    }
    internal static bool MatchesTransport(OutboxMessage transport, H6DispatchPlan plan)
    {
        if (plan.Source.SchemaVersion != 1 || transport.Id != plan.Source.EventId ||
            !string.Equals(transport.MessageType, plan.MessageType, StringComparison.Ordinal) ||
            transport.OccurredAtUtc != plan.Source.OccurredAtUtc ||
            transport.PayloadJson.Length > 65536) return false;
        try
        {
            using var document = JsonDocument.Parse(transport.PayloadJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return false;
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in document.RootElement.EnumerateObject())
                if (!names.Add(property.Name)) return false;
            return JsonSerializer.Deserialize<H6StoredEvent>(transport.PayloadJson, Json) == plan.Source;
        }
        catch (JsonException) { return false; }
    }
}
