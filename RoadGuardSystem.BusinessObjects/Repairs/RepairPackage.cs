namespace RoadGuardSystem.BusinessObjects.Repairs;

/// <summary>Derived package completion is separate from pending explicit Defect/project closure authority.</summary>
public sealed class RepairPackage
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid DefectId { get; private set; }
    // Nullable preserves legacy unknown provenance; only the actual package producer captures this source status.
    public RoadGuardSystem.aBusinessObjects.Commons.DefectStatus? DefectStatusAtAnchor { get; private set; }
    public IReadOnlyList<RepairObligation> Obligations => _obligations.AsReadOnly();
    private readonly List<RepairObligation> _obligations = [];
    public IReadOnlyList<RepairItem> Items => _items.AsReadOnly();
    private readonly List<RepairItem> _items = [];
    public bool IsComplete => RepairPackageCompletion.AllMandatoryResolved(Obligations);
    private RepairPackage() { }
    public static RepairPackage Create(Guid id, Guid project, Guid defect, IReadOnlyList<RepairObligation> obligations)
    {
        RepairGuards.Id(id); RepairGuards.Id(project); RepairGuards.Id(defect); ArgumentNullException.ThrowIfNull(obligations);
        if (obligations.Count == 0 || obligations.Any(obligation => obligation is null) ||
            obligations.Select(obligation => obligation.Id).Distinct().Count() != obligations.Count)
            throw new ArgumentException("A package requires distinct actual obligations.");
        if (obligations.Any(obligation => obligation.ProjectId != project || obligation.DefectId != defect))
            throw new InvalidOperationException("Package obligations must belong to its project and Defect.");
        var package = new RepairPackage { Id = id, ProjectId = project, DefectId = defect };
        package._obligations.AddRange(obligations); return package;
    }
    public void AddItem(RepairItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var obligation = Obligations.SingleOrDefault(obligation => obligation.Id == item.ObligationId);
        if (obligation is null || item.ProjectId != ProjectId || item.DefectId != DefectId || _items.Any(existing => existing.Id == item.Id))
            throw new InvalidOperationException("A distinct item must target an actual package obligation.");
        if (IsActive(item))
        {
            var activeScopes = _items.Where(IsActive).Select(existing => Obligations.Single(obligation => obligation.Id == existing.ObligationId))
                .Where(existing => existing.Kind == obligation.Kind).Select(existing => existing.Scope).ToArray();
            RepairScopeReservation.EnsureAvailable(obligation.Scope, activeScopes);
        }
        _items.Add(item);
    }
    public RepairItem ContinueNormally(Guid sourceItemId, Guid successorId, Guid actor, DateTimeOffset at,
        RepairProposalPlan plan)
    {
        var source = _items.SingleOrDefault(item => item.Id == sourceItemId)
            ?? throw new InvalidOperationException("The predecessor must belong to this package.");
        var obligation = _obligations.Single(row => row.Id == source.ObligationId);
        if (source.SupersededByItemId is not null ||
            source.State is not (RepairItemState.Cancelled or RepairItemState.CorrectionRequired) ||
            obligation.IsResolved || obligation.Kind != RepairObligationKind.FormalRepair ||
            _items.Any(item => item.Id == successorId))
            throw new InvalidOperationException("Normal continuation requires one unresolved, explicitly retired predecessor.");
        var next = RepairItem.ProposeWithPlan(successorId, obligation, RepairMode.Normal, actor,
            RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode.ProjectManager, at, plan);
        var competingScopes = _items.Where(item => item.Id != source.Id && IsActive(item))
            .Select(item => _obligations.Single(row => row.Id == item.ObligationId))
            .Where(row => row.Kind == obligation.Kind).Select(row => row.Scope).ToArray();
        RepairScopeReservation.EnsureAvailable(obligation.Scope, competingScopes);
        source.LinkNormalSuccessor(next);
        _items.Add(next);
        return next;
    }
    private static bool IsActive(RepairItem item) => item.SupersededByItemId is null &&
        item.State is not (RepairItemState.Confirmed or RepairItemState.Cancelled);
}
