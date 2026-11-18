using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Web;

namespace RoadGuardSystem.Repositories.Web;

public interface IWebCoordinationRepository
{
    Task<bool> AuthorizeAsync(Guid actor, UserRoleCode role, Guid[] projects, CancellationToken token);
    Task<OrganizationProfile?> OrganizationAsync(Guid user, CancellationToken token);
    Task<TaskAssignee[]> TaskAssigneesAsync(Guid project, Guid[] tasks, CancellationToken token);
    Task<ProjectOverview[]> OverviewsAsync(Guid[] ids, CancellationToken token);
    Task<ProjectContact[]> ContactsAsync(Guid project, Guid? after, int take, CancellationToken token);
    Task<PolicyDiscovery[]> PoliciesAsync(Guid project, string kind, Guid? after, int take, string? defectType, bool applicableOnly, CancellationToken token);
    Task<PackagePlanning?> PackageAsync(Guid project, Guid package, CancellationToken token);
    Task<ItemPlanning?> ItemAsync(Guid project, Guid package, Guid item, CancellationToken token);
    Task<DefectSourceReview?> DefectAsync(Guid project, Guid package, Guid item, CancellationToken token);
    Task<WebMetadataResult> WriteAsync(Guid actor, UserRoleCode role, WebMetadataCommand command, CancellationToken token);
}
