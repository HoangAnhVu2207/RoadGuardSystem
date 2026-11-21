using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
namespace RoadGuardSystem.Repositories.Messaging;

public sealed class H6SurveyNotificationSourceAdapter(RoadGuardDbContext db) : IH6NotificationSourceAdapter
{
    public bool Supports(string sourceKind) => sourceKind == "SurveyTask";
    public IQueryable<H6SourceScope> ScopeQuery() => from task in db.SurveyRequests
                                                     join assignment in db.SurveyAssignments on task.Id equals assignment.SurveyRequestId
                                                     where assignment.EndedAt == null
                                                     select new H6SourceScope { SourceKind = "SurveyTask", SourceId = task.Id, ProjectId = task.ProjectId, AssignedProjectId = task.ProjectId, AssignedUserId = assignment.OperatorUserId, ProjectAuthorityVerified = false };
    public async Task<H6SourceResolution> ResolveAsync(H6DispatchPlan plan, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Source proof requires transaction.");
        if (!Supports(plan.Source.SourceKind)) return new("REJECTED", "notification_source_kind_invalid");
        var source = plan.Source;
        var transport = await db.OutboxMessages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == source.EventId, cancellationToken);
        if (transport is null || !H6FieldNotificationSourceAdapter.MatchesTransport(transport, plan)) return new("REJECTED", "notification_transport_relation_invalid");
        var task = await db.SurveyRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == source.SourceId && x.ProjectId == source.ProjectId, cancellationToken);
        if (task is null || task.ScopeFormatVersion != "BAND_V1") return new("REJECTED", "notification_source_not_found");
        if (plan.MessageType == "survey.dataset.assessed.v1")
        {
            var assessment = await (from review in db.Set<RoadGuardSystem.BusinessObjects.Surveys.DatasetAssessment>()
                                    join dataset in db.SurveyDataVersions on review.DatasetId equals dataset.Id
                                    join survey in db.Surveys on dataset.SurveyId equals survey.Id
                                    where review.Id == source.OriginEventId && review.Id == source.SourceRevisionId && review.ReviewedAt == source.OccurredAtUtc &&
                                        review.MethodVersion == "pm-evidence-review.v1" && survey.SurveyRequestId == task.Id && survey.ProjectId == task.ProjectId
                                    select review.Id).AnyAsync(cancellationToken);
            var assigned = await db.SurveyAssignments.AsNoTracking().SingleOrDefaultAsync(x => x.SurveyRequestId == task.Id && x.OperatorUserId == source.ResponsibleUserId && x.EndedAt == null && x.AssignedAt <= source.OccurredAtUtc, cancellationToken);
            return assessment && assigned is not null
                ? new("VERIFIED", TaskId: task.Id, AssignmentId: assigned.Id, ResponsibleUserId: assigned.OperatorUserId, ResponsibleRole: UserRoleCode.DroneOperator)
                : new("REJECTED", "notification_source_relation_invalid");
        }
        if (plan.MessageType == "survey.task.submitted.v1")
        {
            var valid = await (from dataset in db.SurveyDataVersions
                               join survey in db.Surveys on dataset.SurveyId equals survey.Id
                               where dataset.Id == source.OriginEventId && dataset.Id == source.SourceRevisionId && survey.SurveyRequestId == task.Id && survey.ProjectId == task.ProjectId && dataset.ConfirmedAt == source.OccurredAtUtc
                               select dataset.Id).AnyAsync(cancellationToken);
            return valid ? new("VERIFIED", TaskId: task.Id) : new("REJECTED", "notification_source_relation_invalid");
        }
        var assignment = await db.SurveyAssignments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == source.OriginEventId && x.Id == source.SourceRevisionId && x.SurveyRequestId == task.Id && x.AssignedAt == source.OccurredAtUtc && x.OperatorUserId == source.ResponsibleUserId, cancellationToken);
        if (assignment is null || (plan.MessageType == "survey.task.supplement_requested.v1") != (task.ParentTaskId != null && task.SupplementRequestId != null)) return new("REJECTED", "notification_source_relation_invalid");
        if (task.SupplementRequestId is Guid supplement && !await db.SupplementarySurveyRequests.AnyAsync(x => x.Id == supplement && x.SurveyRequestId == task.ParentTaskId, cancellationToken)) return new("REJECTED", "notification_source_relation_invalid");
        // The event remains historical; delivery and inbox still require this exact assignment to be current.
        return new("VERIFIED", TaskId: task.Id, AssignmentId: assignment.Id, ResponsibleUserId: assignment.OperatorUserId, ResponsibleRole: UserRoleCode.DroneOperator);
    }
}
