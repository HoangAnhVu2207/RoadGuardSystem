using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Offline;

// Protected admission is an audit of actual caller/scope checks, never a client-selectable permission.
public sealed class OfflineOperationAdmission
{
    private OfflineOperationAdmission() { }
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid BatchId { get; private set; }
    public Guid BindingId { get; private set; }
    public Guid CurrentImporterId { get; private set; }
    public UserRoleCode ImporterRole { get; private set; }
    public Guid? GrantId { get; private set; }
    public string AdmissionMode { get; private set; } = "";
    public string ScopeFactsJson { get; private set; } = "{}";
    public DateTimeOffset AdmittedAt { get; private set; }
    public static OfflineOperationAdmission Record(Guid id, Guid project, Guid batch, Guid binding, Guid importer,
        UserRoleCode role, Guid? grant, string scopeFactsJson, DateTimeOffset at)
    {
        OfflineRuntimeGuards.Identity(id, project, batch, binding, importer); OfflineRuntimeGuards.Time(at);
        OfflineRuntimeGuards.Json(scopeFactsJson, 1048576);
        if (role is not (UserRoleCode.ProjectManager or UserRoleCode.RepairCrew) || grant == Guid.Empty)
            throw new ArgumentException("An actual importing actor is required.");
        return new()
        {
            Id = id,
            ProjectId = project,
            BatchId = batch,
            BindingId = binding,
            CurrentImporterId = importer,
            ImporterRole = role,
            GrantId = grant,
            AdmissionMode = grant.HasValue ? "HANDOVER" : "DIRECT_SYNC",
            ScopeFactsJson = scopeFactsJson,
            AdmittedAt = at.ToUniversalTime()
        };
    }
}
