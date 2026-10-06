using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Offline;

public sealed class OfflineDeviceRegistration
{
    private OfflineDeviceRegistration() { }
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid ActorId { get; private set; }
    public Guid DeviceId { get; private set; }
    public int Revision { get; private set; }
    public UserRoleCode RoleSnapshot { get; private set; }
    public string EncryptionPublicKey { get; private set; } = "";
    public string SigningPublicKey { get; private set; } = "";
    public string KeyFingerprint { get; private set; } = "";
    public DateTimeOffset RegisteredAt { get; private set; }
    public static OfflineDeviceRegistration Register(Guid id, Guid project, Guid actor, Guid device, int revision,
        UserRoleCode role, string encryptionPublicKey, string signingPublicKey, DateTimeOffset at)
    {
        OfflineRuntimeGuards.Identity(id, project, actor, device); OfflineRuntimeGuards.Time(at);
        if (revision < 1 || role is not (UserRoleCode.RepairCrew or UserRoleCode.ProjectManager))
            throw new ArgumentException("A supported device actor and revision are required.");
        OfflinePackageAuthentication.ValidateRegistration(new(actor, device.ToString("D"), encryptionPublicKey, signingPublicKey));
        return new() { Id=id, ProjectId=project, ActorId=actor, DeviceId=device, Revision=revision,
            RoleSnapshot=role, EncryptionPublicKey=encryptionPublicKey, SigningPublicKey=signingPublicKey,
            KeyFingerprint=OfflineRuntimeGuards.Digest(encryptionPublicKey+"\n"+signingPublicKey), RegisteredAt=at.ToUniversalTime() };
    }
}
public sealed class OfflineDeviceRevocation
{
    private OfflineDeviceRevocation() { }
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid DeviceRegistrationId { get; private set; }
    public Guid RevokedBy { get; private set; }
    public DateTimeOffset RevokedAt { get; private set; }
    public string Reason { get; private set; } = "";
    public static OfflineDeviceRevocation Record(Guid id, Guid project, Guid registration, Guid actor,
        DateTimeOffset at, string reason)
    {
        OfflineRuntimeGuards.Identity(id, project, registration, actor); OfflineRuntimeGuards.Time(at);
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 2000) throw new ArgumentException("Revocation reason is required.");
        return new() { Id=id, ProjectId=project, DeviceRegistrationId=registration, RevokedBy=actor,
            RevokedAt=at.ToUniversalTime(), Reason=reason.Trim() };
    }
}
