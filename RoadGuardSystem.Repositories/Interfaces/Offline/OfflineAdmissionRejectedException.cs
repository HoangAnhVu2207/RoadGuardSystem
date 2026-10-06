namespace RoadGuardSystem.Repositories.Offline;

// Finite scope/identity rejection only; unexpected SQL/storage/cancellation faults are never an ACK.
public sealed class OfflineAdmissionRejectedException(int status, string code) : Exception(code)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}
