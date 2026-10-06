namespace RoadGuardSystem.BusinessObjects.Projects;

public enum ProjectLifecycleFactKind : byte
{
    ConstructionCompletion = 1, OperationalClosure = 2, ObligationTransferGrant = 3,
    ObligationTransferAcceptance = 4, RenewedHandlingScope = 5, InventoryObservation = 6,
    DefectClosure = 7, LinkedRecurrence = 8
}

// A recorded source is not command authority. Pending generic lifecycle commands
// cannot create verified facts through an API; technical import candidates stay candidates.
public sealed class ProjectLifecycleHistoryRecord
{
    private ProjectLifecycleHistoryRecord() { }
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public ProjectLifecycleFactKind Kind { get; private set; }
    public Guid ActorId { get; private set; }
    public DateTimeOffset RecordedAtUtc { get; private set; }
    public Guid? ObligationId { get; private set; }
    public Guid? GrantId { get; private set; }
    public Guid? ReceiverId { get; private set; }
    public Guid? OperationalClosureId { get; private set; }
    public Guid? DefectId { get; private set; }
    public Guid? LinkedDefectId { get; private set; }
    public string Reason { get; private set; } = "";
    public string BasisReference { get; private set; } = "";
    public string AuthoritySourceReference { get; private set; } = "";
    public string SourceDisposition { get; private set; } = "CANDIDATE";
    public string FactsJson { get; private set; } = "{}";

    public static ProjectLifecycleHistoryRecord RecordRenewed(Guid id,ProjectLifecycleHistoryRecord closure,
        Guid actor,RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode role,bool currentProjectAuthority,
        bool verifiedClosureSource,DateTimeOffset at,string reason,string basis,string handlingScope)
    {
        ArgumentNullException.ThrowIfNull(closure);
        if(closure.Kind!=ProjectLifecycleFactKind.OperationalClosure || closure.SourceDisposition!="TARGET_CONFIRMED" ||
            string.IsNullOrWhiteSpace(closure.AuthoritySourceReference) ||
            ProjectLifecycleAuthority.Evaluate(ProjectLifecycleCommandKind.RenewedHandlingScope,role,currentProjectAuthority,verifiedClosureSource)!="success" ||
            string.IsNullOrWhiteSpace(handlingScope) || handlingScope.Length>4000 || at<closure.RecordedAtUtc)
            throw new InvalidOperationException("Verified actual closure and current Supervisor scope authority are required.");
        var record=RecordCandidate(id,closure.ProjectId,ProjectLifecycleFactKind.RenewedHandlingScope,actor,at,reason,basis,
            System.Text.Json.JsonSerializer.Serialize(new {handlingScope=handlingScope.Trim()}));
        record.OperationalClosureId=closure.Id;record.SourceDisposition="TARGET_CONFIRMED";
        record.AuthoritySourceReference="OWNER_UPDATE_2026-10-06:R26_SUPERVISOR_RENEWED_SCOPE";
        return record;
    }

    public static ProjectLifecycleHistoryRecord RecordCandidate(Guid id, Guid project, ProjectLifecycleFactKind kind,
        Guid actor, DateTimeOffset at, string reason, string basisReference, string factsJson,
        Guid? obligation=null, Guid? grant=null, Guid? receiver=null,Guid? defect=null,Guid? linkedDefect=null)
    {
        if(id==Guid.Empty || project==Guid.Empty || actor==Guid.Empty || at==default || !Enum.IsDefined(kind) ||
            string.IsNullOrWhiteSpace(reason) || reason.Length>2000 || string.IsNullOrWhiteSpace(basisReference) || basisReference.Length>2000 ||
            obligation==Guid.Empty || grant==Guid.Empty || receiver==Guid.Empty || defect==Guid.Empty || linkedDefect==Guid.Empty)
            throw new ArgumentException("Actual identity, finite kind and source basis are required.");
        using var json=System.Text.Json.JsonDocument.Parse(factsJson);
        if(json.RootElement.ValueKind!=System.Text.Json.JsonValueKind.Object || factsJson.Length>1048576)
            throw new ArgumentException("Bounded object source facts are required.");
        var transfer=kind is ProjectLifecycleFactKind.ObligationTransferGrant or ProjectLifecycleFactKind.ObligationTransferAcceptance;
        if(transfer!=obligation.HasValue || transfer!=grant.HasValue || transfer!=receiver.HasValue)
            throw new ArgumentException("Transfer candidates require exact obligation, grant and receiver pins.");
        if(kind==ProjectLifecycleFactKind.ObligationTransferGrant && grant!=id)
            throw new ArgumentException("A transfer grant candidate retains its own exact grant identity.");
        if((kind is ProjectLifecycleFactKind.DefectClosure or ProjectLifecycleFactKind.LinkedRecurrence)!=defect.HasValue ||
            (kind==ProjectLifecycleFactKind.LinkedRecurrence)!=linkedDefect.HasValue || defect.HasValue && defect==linkedDefect)
            throw new ArgumentException("Defect lifecycle candidates require exact distinct Defect source pins.");
        return new(){Id=id,ProjectId=project,Kind=kind,ActorId=actor,RecordedAtUtc=at.ToUniversalTime(),
            Reason=reason.Trim(),BasisReference=basisReference.Trim(),FactsJson=factsJson,
            ObligationId=obligation,GrantId=grant,ReceiverId=receiver,DefectId=defect,LinkedDefectId=linkedDefect};
    }
}
