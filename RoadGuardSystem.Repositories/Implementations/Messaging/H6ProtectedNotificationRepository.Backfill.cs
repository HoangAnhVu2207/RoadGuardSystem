using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Messaging;

namespace RoadGuardSystem.Repositories.Messaging;

public sealed partial class H6ProtectedNotificationRepository
{
    private async Task BackfillOwnedScopesAsync(Guid actor, CancellationToken token)
    {
        var rows = await db.Notifications.AsNoTracking().Where(notification => notification.RecipientUserId == actor &&
            !db.Set<H6NotificationScopeRow>().Any(scope => scope.NotificationId == notification.Id))
            .OrderBy(notification => notification.Id).Take(100).ToArrayAsync(token);
        foreach (var notification in rows)
        {
            // Lock the actual notification anchor: concurrent readers append one scope/audit,
            // never a sidecar permission derived from a legacy label alone.
            await db.Notifications.FromSqlInterpolated($"SELECT * FROM [Notifications] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={notification.Id}")
                .AsNoTracking().SingleAsync(token);
            if (await db.Set<H6NotificationScopeRow>().AnyAsync(row => row.NotificationId == notification.Id, token)) continue;
            var project = await LegacyProjectAsync(notification, token); var classification = "UNKNOWN_PROTECTED";
            if (project.HasValue) classification = "PROJECT";
            else if (notification.SourceEntityType == "PasswordRecoveryRequest" && notification.EventType == "password_recovery_requested" &&
                await db.PasswordRecoveryRequests.AnyAsync(row => row.Id == notification.SourceEntityId && row.TargetUserId.HasValue, token))
                classification = "NON_PROJECT";
            var auditId = Guid.NewGuid(); var now = clock.GetUtcNow().ToUniversalTime();
            var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(notification.Id.ToString("N") + "|legacy|" + NotificationRegisteredTypes.RegistryVersion))).ToLowerInvariant();
            db.Set<H6NotificationAuditRow>().Add(new()
            {
                Id = auditId,
                NotificationId = notification.Id,
                ProjectId = project,
                Classification = classification,
                SourceKind = notification.SourceEntityType,
                SourceId = notification.SourceEntityId,
                ReasonCode = classification == "UNKNOWN_PROTECTED" ? "notification_legacy_scope_unknown" : "notification_legacy_source_verified",
                ResolverVersion = NotificationRegisteredTypes.RegistryVersion,
                DedupKey = key,
                RecordedAtUtc = now
            });
            db.Set<H6NotificationScopeRow>().Add(new()
            {
                NotificationId = notification.Id,
                ProjectId = project,
                Classification = classification,
                SourceKind = notification.SourceEntityType,
                SourceId = notification.SourceEntityId,
                EventType = notification.EventType,
                ResolverVersion = NotificationRegisteredTypes.RegistryVersion,
                CurrentAuditId = auditId
            });
        }
        await db.SaveChangesAsync(token);
    }
    private async Task<Guid?> LegacyProjectAsync(Notification notification, CancellationToken token)
    {
        var id = notification.SourceEntityId;
        switch (notification.SourceEntityType)
        {
            case "Project": return await db.Projects.Where(row => row.Id == id).Select(row => (Guid?)row.Id).SingleOrDefaultAsync(token);
            case "FieldTask": case "FieldInspectionTask": return await db.FieldInspectionTasks.Where(row => row.Id == id).Select(row => (Guid?)row.ProjectId).SingleOrDefaultAsync(token);
            case "Defect": return await db.Defects.Where(row => row.Id == id).Select(row => row.ProjectId).SingleOrDefaultAsync(token);
            case "Case": case "IncidentCase": return await db.IncidentCases.Where(row => row.Id == id).Select(row => row.ProjectId).SingleOrDefaultAsync(token);
            case "SurveyRequest": return await db.SurveyRequests.Where(row => row.Id == id).Select(row => (Guid?)row.ProjectId).SingleOrDefaultAsync(token);
            case "Survey": return await db.Surveys.Where(row => row.Id == id).Select(row => (Guid?)row.ProjectId).SingleOrDefaultAsync(token);
            case "RoadSectionVersion":
                return await (from version in db.RoadSectionVersions
                              join road in db.RoadSections on version.RoadSectionId equals road.Id
                              where version.Id == id
                              select (Guid?)road.ProjectId).SingleOrDefaultAsync(token);
        }
        var adapter = sources.SingleOrDefault(source => source.Supports(notification.SourceEntityType));
        return adapter is null ? null : await adapter.ScopeQuery().Where(source => source.SourceId == id && source.SourceKind == notification.SourceEntityType)
            .Select(source => (Guid?)source.ProjectId).Distinct().SingleOrDefaultAsync(token);
    }
}
