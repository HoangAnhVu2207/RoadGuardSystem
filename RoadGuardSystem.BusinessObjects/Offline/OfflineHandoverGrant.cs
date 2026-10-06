using RoadGuardSystem.aBusinessObjects.Commons;
using System.Text.Json;

namespace RoadGuardSystem.BusinessObjects.Offline;

public sealed class OfflineHandoverGrant
{
    private OfflineHandoverGrant() { }
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid PackageId { get; private set; }
    public Guid SourceActorId { get; private set; }
    public Guid SourceDeviceRegistrationId { get; private set; }
    public Guid RecipientActorId { get; private set; }
    public Guid RecipientDeviceRegistrationId { get; private set; }
    public UserRoleCode RecipientRole { get; private set; }
    public Guid IssuedBy { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public string ManifestHash { get; private set; } = "";
    public string ScopeJson { get; private set; } = "{}";
    public string Reason { get; private set; } = "";
    public static OfflineHandoverGrant Issue(Guid id, Guid project, Guid package, Guid sourceActor,
        Guid sourceDeviceRegistration, Guid recipientActor, Guid recipientDeviceRegistration, UserRoleCode recipientRole,
        Guid issuer, UserRoleCode issuerRole, DateTimeOffset at, string manifestHash, string scopeJson, string reason)
    {
        foreach (var value in new[] { id, project, package, sourceActor, sourceDeviceRegistration,
                     recipientActor, recipientDeviceRegistration, issuer }) OfflineCryptoFormat.Id(value);
        if (issuerRole != UserRoleCode.Supervisor || recipientRole is not (UserRoleCode.ProjectManager or UserRoleCode.RepairCrew))
            throw new ArgumentException("Supervisor issues scoped data handover; the recipient retains its actual role.");
        if (at == default || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 2000 ||
            string.IsNullOrWhiteSpace(scopeJson) || scopeJson.Length > 262144)
            throw new ArgumentException("An explicit bounded scope, origin and reason are required.");
        OfflineCryptoFormat.HashBytes(manifestHash);
        using var scope = JsonDocument.Parse(scopeJson);
        if (scope.RootElement.ValueKind is not (JsonValueKind.Array or JsonValueKind.Object))
            throw new ArgumentException("Grant scope must describe explicit items.");
        var origin = at.ToUniversalTime();
        return new OfflineHandoverGrant { Id = id, ProjectId = project, PackageId = package,
            SourceActorId = sourceActor, SourceDeviceRegistrationId = sourceDeviceRegistration,
            RecipientActorId = recipientActor, RecipientDeviceRegistrationId = recipientDeviceRegistration,
            RecipientRole = recipientRole, IssuedBy = issuer, IssuedAt = origin, ExpiresAt = origin.AddHours(24),
            ManifestHash = manifestHash, ScopeJson = scopeJson, Reason = reason.Trim() };
    }
    public bool AllowsNewAdmissionAt(DateTimeOffset at) => at.ToUniversalTime() >= IssuedAt && at.ToUniversalTime() < ExpiresAt;
}
