using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RoadGuardSystem.BusinessObjects.Repairs;

namespace RoadGuardSystem.BusinessObjects.Projects;

public enum LD06ActionKind : byte
{
    DeclareConstruction = 1, ConfirmConstruction = 2, CloseDefect = 3, OperationalClose = 4,
    LinkRecurrence = 5, IssueTransfer = 6, AcceptTransfer = 7
}

// Production facts are distinct from historical import candidates.
public sealed class LD06LifecycleAction
{
    private LD06LifecycleAction() { }
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid ActorId { get; private set; }
    public LD06ActionKind Kind { get; private set; }
    public Guid? SourceActionId { get; private set; }
    public Guid? DefectId { get; private set; }
    public Guid? LinkedDefectId { get; private set; }
    public Guid? ObligationId { get; private set; }
    public Guid? ReceivingProjectId { get; private set; }
    public Guid? PriorRepairDecisionId { get; private set; }
    public DateTimeOffset At { get; private set; }
    public string Reason { get; private set; } = "";
    public string ScopeHash { get; private set; } = "";
    public string FactsJson { get; private set; } = "{}";
    public static LD06LifecycleAction Create(Guid id, Guid project, Guid actor, LD06ActionKind kind,
        DateTimeOffset at, string reason, string facts, Guid? source = null, Guid? defect = null,
        Guid? linked = null, Guid? obligation = null, Guid? receiving = null, Guid? decision = null, string scopeHash = "")
    {
        if (id == Guid.Empty || project == Guid.Empty || actor == Guid.Empty || !Enum.IsDefined(kind) || at == default ||
            string.IsNullOrWhiteSpace(reason) || reason.Length > 2000 || facts.Length > 1048576)
            throw new ArgumentException("Finite lifecycle identity, reason and facts are required.");
        using var document = JsonDocument.Parse(facts);
        if (document.RootElement.ValueKind != JsonValueKind.Object) throw new ArgumentException("Object facts required.");
        return new()
        {
            Id = id,
            ProjectId = project,
            ActorId = actor,
            Kind = kind,
            At = at.ToUniversalTime(),
            Reason = reason,
            FactsJson = facts,
            SourceActionId = source,
            DefectId = defect,
            LinkedDefectId = linked,
            ObligationId = obligation,
            ReceivingProjectId = receiving,
            PriorRepairDecisionId = decision,
            ScopeHash = scopeHash
        };
    }
    public static string HashScope(RepairObligation obligation) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        JsonSerializer.Serialize(new
        {
            obligation.Id,
            obligation.ProjectId,
            obligation.DefectId,
            obligation.Kind,
            obligation.Mandatory,
            scopeId = obligation.Scope.Id,
            obligation.Scope.PhysicalRoadId,
            obligation.Scope.LocationVersion,
            obligation.Scope.RouteLabel,
            from = obligation.Scope.From.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture),
            to = obligation.Scope.To.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture),
            offsetFrom = obligation.Scope.OffsetFrom.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture),
            offsetTo = obligation.Scope.OffsetTo.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)
        })))).ToLowerInvariant();
}

public sealed class ObligationResponsibility
{
    private ObligationResponsibility() { }
    public Guid ObligationId { get; private set; }
    public Guid OriginProjectId { get; private set; }
    public Guid CurrentProjectId { get; private set; }
    public Guid AcceptanceActionId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public static ObligationResponsibility Accept(Guid obligation, Guid origin, Guid receiver, Guid acceptance)
        => new() { ObligationId = obligation, OriginProjectId = origin, CurrentProjectId = receiver, AcceptanceActionId = acceptance };
    public void Transfer(Guid receiver, Guid acceptance) { CurrentProjectId = receiver; AcceptanceActionId = acceptance; }
}

public sealed class LD06ActionEvidence
{
    private LD06ActionEvidence() { }
    public Guid ActionId { get; private set; }
    public Guid FileId { get; private set; }
    public string Checksum { get; private set; } = "";
    public static LD06ActionEvidence Pin(Guid action, Guid file, string hash) => new() { ActionId = action, FileId = file, Checksum = hash };
}
