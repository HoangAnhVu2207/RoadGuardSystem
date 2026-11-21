using System.Text.Json;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.BusinessObjects.Messaging;
namespace RoadGuardSystem.Repositories.Implementations.Surveys;

public sealed partial class SurveyV2PersistenceService
{
    private void EmitMobileAssignment(SurveyRequest task, SurveyAssignment assignment)
    {
        var supplement = task.ParentTaskId.HasValue && task.SupplementRequestId.HasValue;
        EmitMobile(task, supplement ? "SUPPLEMENT" : "ASSIGNED", supplement ? "survey.task.supplement_requested.v1" : "survey.task.assigned.v1", assignment.Id, assignment.AssignedAt, assignment.OperatorUserId);
    }
    private void EmitMobile(SurveyRequest task, string kind, string messageType, Guid origin, DateTimeOffset at, Guid? responsible = null)
    {
        var id = Guid.NewGuid();
        _context.OutboxMessages.Add(OutboxMessage.Create(id, messageType, at, null, JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            eventId = id,
            kind,
            projectId = task.ProjectId,
            sourceKind = "SurveyTask",
            sourceId = task.Id,
            originEventId = origin,
            occurredAtUtc = at.ToUniversalTime(),
            sourceRevisionId = origin,
            responsibleUserId = responsible
        }, DatasetScopeJsonOptions)));
    }
}
