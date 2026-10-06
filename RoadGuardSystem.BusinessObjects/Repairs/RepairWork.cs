using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Repairs;

public enum RepairScopeComparison : byte { Unknown = 0, Disjoint = 1, Overlapping = 2 }
public enum RepairObligationKind : byte { FormalRepair = 1, TemporarySafety = 2 }
public enum RepairMode : byte { Normal = 1, FastTrack = 2 }
public enum RepairPresentationState : byte { Unrepaired = 1, ReportedAwaitingReview = 2, Confirmed = 3 }
public enum RepairItemState : byte { AwaitingApproval = 1, ReadyToAssign = 2, Assigned = 3, InProgress = 4, Submitted = 5, Reviewed = 6, Confirmed = 7, Cancelled = 8, CorrectionRequired = 9 }
public enum RepairEvidencePurpose : byte { Before = 1, After = 2, Safety = 3 }
public enum RepairRecurrenceKind : byte { NewLinkedDefect = 1, CorrectionRequired = 2 }
public sealed record RepairObligationResolutionEvent(Guid DecisionId, bool Accepted, Guid? SupersedesDecisionId, DateTimeOffset At,
    Guid? PreviousHeadDecisionId = null);
public sealed record RepairProposalPlan(string RepairPlan, string ChecklistVersion);

/// <summary>Physical road identity is explicit, never inferred from polygon overlap or route label.
/// Bounds must share a sourced coordinate/version frame; unknown comparisons fail reservation.</summary>
public sealed class RepairActualScope
{
    public Guid Id { get; private set; }
    public Guid PhysicalRoadId { get; private set; }
    public string LocationVersion { get; private set; } = "";
    public string RouteLabel { get; private set; } = "";
    public decimal From { get; private set; }
    public decimal To { get; private set; }
    public decimal OffsetFrom { get; private set; }
    public decimal OffsetTo { get; private set; }
    private RepairActualScope() { }
    public static RepairActualScope Create(Guid id, Guid road, string version, string label, decimal from, decimal to, decimal offsetFrom, decimal offsetTo)
    {
        RepairGuards.Id(id); RepairGuards.Id(road);
        const decimal maximumStoredBound = 999999999999999.999m;
        if (new[] { from, to, offsetFrom, offsetTo }.Any(value => value < -maximumStoredBound || value > maximumStoredBound || value != decimal.Round(value, 3)))
            throw new ArgumentException("Scope bounds must fit exact decimal(18,3) storage; rounding cannot establish overlap admission.");
        if (from < 0 || to <= from || offsetTo <= offsetFrom) throw new ArgumentException("Physical scope bounds must have positive extent.");
        return new RepairActualScope
        {
            Id = id,
            PhysicalRoadId = road,
            LocationVersion = RepairGuards.Text(version),
            RouteLabel = RepairGuards.Text(label),
            From = from,
            To = to,
            OffsetFrom = offsetFrom,
            OffsetTo = offsetTo
        };
    }
    public RepairScopeComparison Compare(RepairActualScope other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (PhysicalRoadId != other.PhysicalRoadId) return RepairScopeComparison.Disjoint;
        if (LocationVersion != other.LocationVersion) return RepairScopeComparison.Unknown;
        return From < other.To && other.From < To && OffsetFrom < other.OffsetTo && other.OffsetFrom < OffsetTo
            ? RepairScopeComparison.Overlapping : RepairScopeComparison.Disjoint;
    }
}
public static class RepairScopeReservation
{
    // The repository must lock the Defect + obligation-kind anchor before reading active scopes.
    // This pure comparison alone cannot establish concurrent SQL uniqueness.
    public static void EnsureAvailable(RepairActualScope scope, IReadOnlyList<RepairActualScope> activeScopes)
    {
        ArgumentNullException.ThrowIfNull(scope); ArgumentNullException.ThrowIfNull(activeScopes);
        if (activeScopes.Any(active => scope.Compare(active) != RepairScopeComparison.Disjoint))
            throw new InvalidOperationException("An active scope overlaps or cannot be proved disjoint.");
    }
}
public sealed class RepairObligation
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid DefectId { get; private set; }
    public RepairObligationKind Kind { get; private set; }
    public bool Mandatory { get; private set; }
    public RepairActualScope Scope { get; private set; } = null!;
    public Guid? EffectiveResolutionDecisionId { get; private set; }
    public Guid? EffectiveResolutionHeadDecisionId { get; private set; }
    public Guid? CurrentRepairItemId { get; private set; }
    public Guid? OriginalCrewFirstStartId { get; private set; }
    public bool IsResolved => EffectiveResolutionDecisionId is not null;
    public IReadOnlyList<RepairObligationResolutionEvent> ResolutionHistory => _resolutionHistory.AsReadOnly();
    private readonly List<RepairObligationResolutionEvent> _resolutionHistory = [];
    private RepairObligation() { }
    public static RepairObligation Create(Guid id, Guid project, Guid defect, RepairObligationKind kind, bool mandatory, RepairActualScope scope)
    {
        RepairGuards.Id(id); RepairGuards.Id(project); RepairGuards.Id(defect); RepairGuards.EnumValue(kind); ArgumentNullException.ThrowIfNull(scope);
        return new RepairObligation { Id = id, ProjectId = project, DefectId = defect, Kind = kind, Mandatory = mandatory, Scope = scope };
    }
    public void Resolve(RepairDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        if (decision.ObligationId != Id || decision.DefectId != DefectId || !decision.Accepted || decision.SupersedesDecisionId is not null ||
            Kind == RepairObligationKind.TemporarySafety)
            throw new InvalidOperationException("Resolution requires the final acceptance of this formal repair obligation.");
        if (EffectiveResolutionDecisionId is not null && EffectiveResolutionDecisionId != decision.Id)
            throw new InvalidOperationException("An existing resolution cannot be silently replaced.");
        if (EffectiveResolutionDecisionId == decision.Id) return;
        if (decision.PreviousObligationHeadDecisionId != EffectiveResolutionHeadDecisionId)
            throw new InvalidOperationException("Acceptance must preserve the previous obligation decision head.");
        _resolutionHistory.Add(new(decision.Id, true, null, decision.At, decision.PreviousObligationHeadDecisionId));
        EffectiveResolutionHeadDecisionId = decision.Id;
        EffectiveResolutionDecisionId = decision.Id;
    }
    public void Reopen(RepairDecision correction)
    {
        ArgumentNullException.ThrowIfNull(correction);
        if (correction.ObligationId != Id || correction.DefectId != DefectId || correction.Accepted ||
            correction.SupersedesDecisionId != EffectiveResolutionDecisionId || EffectiveResolutionDecisionId is null)
            throw new InvalidOperationException("Correction must supersede this obligation's current acceptance.");
        _resolutionHistory.Add(new(correction.Id, false, correction.SupersedesDecisionId, correction.At, correction.PreviousObligationHeadDecisionId));
        EffectiveResolutionHeadDecisionId = correction.Id;
        EffectiveResolutionDecisionId = null;
    }
    internal void EnsureCorrectionHead(Guid supersedes)
    {
        if (Kind != RepairObligationKind.FormalRepair || EffectiveResolutionHeadDecisionId != supersedes)
            throw new InvalidOperationException("Correction must continue the formal obligation's latest resolution history.");
    }
    internal void ApplyCorrection(RepairDecision correction)
    {
        // Apply is only reached after both targets and the head were validated, before item mutation.
        _resolutionHistory.Add(new(correction.Id, correction.Accepted, correction.SupersedesDecisionId, correction.At, correction.PreviousObligationHeadDecisionId));
        EffectiveResolutionHeadDecisionId = correction.Id;
        EffectiveResolutionDecisionId = correction.Accepted ? correction.Id : null;
    }
}
public sealed record RepairEvidenceReference(Guid FileId, string? FileVersion, string? Hash, RepairEvidencePurpose Purpose,
    bool Verified, bool Related, string SourceKind, Guid SourceId, DateTimeOffset? CapturedAt, Guid? ReuseDecisionId, bool ReusedSource);
public sealed record RepairWorkHandover(Guid Id, Guid FromActorId, Guid ToActorId, string PerformedScope, string SafetyState, DateTimeOffset At);
public sealed record RepairDecision(Guid Id, Guid ItemId, Guid ObligationId, Guid DefectId, RepairMode Mode,
    Guid ActorId, UserRoleCode Role, string Reason, DateTimeOffset At, Guid? SupersedesDecisionId,
    RepairPresentationState Result, RepairCorrectionBasis? Basis = null, Guid? PreviousObligationHeadDecisionId = null)
{
    private RepairDecision() : this(Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, default, Guid.Empty, default,
        string.Empty, default, null, default)
    { }
    public bool Accepted => Result == RepairPresentationState.Confirmed;
}
/// <summary>Caller-resolved current project authority facts. Confirmed role is PM for FT, Supervisor for normal.
/// Matching supplied facts cannot override that role rule; the repository rechecks existing membership/assignment under locks.</summary>
public sealed record RepairCorrectionAuthority(Guid ProjectMembershipId, Guid ItemId, Guid ActorId, UserRoleCode Role, string SourceLabel);
public sealed record RepairRecurrenceLink(Guid NewDefectId, Guid PreviousDefectId, Guid PreviousAcceptanceId, Guid ActorId, string Reason, DateTimeOffset At);
public sealed class RepairAttempt
{
    public Guid Id { get; private set; }
    public Guid OriginId { get; private set; }
    public string PayloadHash { get; private set; } = "";
    public Guid ItemId { get; private set; }
    public Guid ObligationId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid DefectId { get; private set; }
    public Guid CrewId { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid AssignmentId { get; private set; }
    public Guid? AuthorizationId { get; private set; }
    public string LocationVersion { get; private set; } = "";
    public Guid? PolicyRevisionId { get; private set; }
    public bool Performed { get; private set; }
    public string? UnperformedReason { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public DateTimeOffset ServerReceivedAt { get; private set; }
    public RepairTimeProvenance TimeProvenance { get; private set; }
    public IReadOnlyList<RepairEvidenceReference> Evidence => _evidence.AsReadOnly();
    private readonly List<RepairEvidenceReference> _evidence = [];
    private RepairAttempt() { }
    public static RepairAttempt Submit(Guid id, Guid origin, string hash, Guid item, Guid obligation, Guid project, Guid defect,
        Guid crew, Guid task, Guid assignment, Guid? authorization, string location, Guid? policy, bool performed, string? reason,
        DateTimeOffset? start, DateTimeOffset? finish, DateTimeOffset received, RepairTimeProvenance provenance,
        IReadOnlyList<RepairEvidenceReference> evidence)
    {
        RepairGuards.Id(id); RepairGuards.Id(origin); RepairGuards.Id(item); RepairGuards.Id(obligation); RepairGuards.Id(project);
        RepairGuards.Id(defect); RepairGuards.Id(crew); RepairGuards.Id(task); RepairGuards.Id(assignment);
        if (authorization == Guid.Empty) throw new ArgumentException("A supplied authorization identity must be non-empty.");
        if (policy == Guid.Empty) throw new ArgumentException("A supplied policy identity must be non-empty.");
        RepairGuards.EnumValue(provenance); ArgumentNullException.ThrowIfNull(evidence);
        DateTimeOffset? started = start is null ? null : RepairGuards.Time(start.Value);
        DateTimeOffset? finished = finish is null ? null : RepairGuards.Time(finish.Value);
        var intake = RepairGuards.Time(received);
        if (finished < started) throw new ArgumentException("Finish precedes start.");
        var unperformedReason = string.IsNullOrWhiteSpace(reason) ? null : RepairGuards.Text(reason);
        if (!performed && unperformedReason is null) throw new ArgumentException("Unperformed work requires its reason.");
        if (performed && unperformedReason is not null) throw new ArgumentException("An unperformed reason contradicts performed work.");
        foreach (var file in evidence)
        {
            ArgumentNullException.ThrowIfNull(file); RepairGuards.Id(file.FileId); RepairGuards.Id(file.SourceId); RepairGuards.EnumValue(file.Purpose);
            RepairGuards.Text(file.SourceKind);
            if (file.FileVersion is not null) RepairGuards.Text(file.FileVersion);
            if (file.Hash is not null) RepairGuards.Text(file.Hash);
            if (file.CapturedAt is not null) RepairGuards.Time(file.CapturedAt.Value);
            if (file.Verified && (file.FileVersion is null || file.Hash is null))
                throw new ArgumentException("Verified evidence requires its immutable file version and hash.");
            if (file.ReuseDecisionId == Guid.Empty) throw new ArgumentException("Reuse decision must be a non-empty identity.");
            if (file.Purpose == RepairEvidencePurpose.After && (file.ReusedSource || file.ReuseDecisionId is not null || file.CapturedAt < finished))
                throw new ArgumentException("AFTER evidence must be a fresh capture after completed repair, per the attempt checklist.");
            if (file.Purpose == RepairEvidencePurpose.Before && file.ReusedSource && file.ReuseDecisionId is null)
                throw new ArgumentException("Reused source evidence requires the PM appropriateness decision.");
        }
        if (evidence.Select(file => (file.FileId, file.Purpose)).Distinct().Count() != evidence.Count)
            throw new ArgumentException("Duplicate evidence-purpose references are not distinct facts.");
        if (evidence.GroupBy(file => file.FileId).Any(group => group.Any(file => file.Purpose == RepairEvidencePurpose.Before) &&
            group.Any(file => file.Purpose == RepairEvidencePurpose.After)))
            throw new ArgumentException("The same file cannot serve as both BEFORE and fresh AFTER.");
        var attempt = new RepairAttempt
        {
            Id = id,
            OriginId = origin,
            PayloadHash = RepairGuards.Text(hash),
            ItemId = item,
            ObligationId = obligation,
            ProjectId = project,
            DefectId = defect,
            CrewId = crew,
            TaskId = task,
            AssignmentId = assignment,
            AuthorizationId = authorization,
            LocationVersion = RepairGuards.Text(location),
            PolicyRevisionId = policy,
            Performed = performed,
            UnperformedReason = unperformedReason,
            StartedAt = started,
            FinishedAt = finished,
            ServerReceivedAt = intake,
            TimeProvenance = provenance
        };
        attempt._evidence.AddRange(evidence); return attempt;
    }
}
public sealed class RepairItem
{
    public string? RepairPlan { get; private set; }
    public string? ChecklistVersion { get; private set; }
    public string? ProposalPlanHash { get; private set; }
    public string? ApprovedPlanHash { get; private set; }
    public Guid Id { get; private set; }
    public Guid ObligationId { get; private set; }
    public Guid DefectId { get; private set; }
    public Guid ProjectId { get; private set; }
    public RepairMode Mode { get; private set; }
    public RepairItemState State { get; private set; }
    public Guid? CrewId { get; private set; }
    public RepairWorkHandover? Handover { get; private set; }
    public IReadOnlyList<RepairAttempt> Attempts => _attempts.AsReadOnly();
    public IReadOnlyList<RepairDecision> Decisions => _decisions.AsReadOnly();
    public IReadOnlyList<RepairReviewRequest> ReviewRequests => _reviewRequests.AsReadOnly();
    public Guid? EffectiveDecisionId { get; private set; }
    public Guid? CurrentBindingId { get; private set; }
    public Guid? CurrentAssessmentId { get; private set; }
    public Guid? CurrentExecutionStartId { get; private set; }
    public Guid? CurrentExecutionFinishId { get; private set; }
    public Guid? CurrentAttemptId { get; private set; }
    public Guid? CurrentIntakeLinkId { get; private set; }
    public Guid? EffectiveIntakeSubmissionId { get; private set; }
    public Guid? CurrentReviewId { get; private set; }
    public Guid? PredecessorItemId { get; private set; }
    public Guid? SupersededByItemId { get; private set; }
    public RepairDecision? EffectiveDecision => _decisions.SingleOrDefault(candidate => candidate.Id == EffectiveDecisionId);
    public bool IsEffectivelyConfirmed => EffectiveDecision?.Accepted == true;
    public RepairPresentationState Presentation => EffectiveDecision?.Result ?? (_attempts.LastOrDefault()?.Performed == true ? RepairPresentationState.ReportedAwaitingReview : RepairPresentationState.Unrepaired);
    private readonly List<RepairAttempt> _attempts = [];
    private readonly List<RepairDecision> _decisions = [];
    private readonly List<RepairReviewRequest> _reviewRequests = [];
    private RepairItem() { }
    public Guid ProposedBy { get; private set; }
    public DateTimeOffset ProposedAt { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public Guid? AssignedBy { get; private set; }
    public DateTimeOffset? AssignedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public Guid? CancelledBy { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public static RepairItem Propose(Guid id, RepairObligation obligation, RepairMode mode, Guid actor, UserRoleCode role, DateTimeOffset at)
    {
        RepairGuards.Id(id); ArgumentNullException.ThrowIfNull(obligation); RepairGuards.EnumValue(mode);
        RepairGuards.Actor(actor, role, UserRoleCode.ProjectManager);
        if (obligation.IsResolved) throw new InvalidOperationException("A resolved obligation cannot receive a new active item.");
        return new RepairItem
        {
            Id = id,
            ObligationId = obligation.Id,
            DefectId = obligation.DefectId,
            ProjectId = obligation.ProjectId,
            Mode = mode,
            State = mode == RepairMode.Normal ? RepairItemState.AwaitingApproval : RepairItemState.ReadyToAssign,
            ProposedBy = actor,
            ProposedAt = RepairGuards.Time(at)
        };
    }
    public static RepairItem ProposeWithPlan(Guid id, RepairObligation obligation, RepairMode mode, Guid actor,
        UserRoleCode role, DateTimeOffset at, RepairProposalPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var repairPlan = RepairGuards.Text(plan.RepairPlan); var checklist = RepairGuards.Text(plan.ChecklistVersion);
        if (checklist.Length > 200) throw new ArgumentException("Checklist version exceeds its storage contract.");
        var item = Propose(id, obligation, mode, actor, role, at);
        item.RepairPlan = repairPlan; item.ChecklistVersion = checklist;
        item.ProposalPlanHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new { schemaVersion = 1, repairPlan, checklistVersion = checklist }))).ToLowerInvariant();
        return item;
    }
    public void Approve(Guid actor, UserRoleCode role, DateTimeOffset at)
    {
        RepairGuards.Actor(actor, role, UserRoleCode.Supervisor); Require(RepairItemState.AwaitingApproval);
        var time = EventTime(at); ApprovedBy = actor; ApprovedAt = time; ApprovedPlanHash = ProposalPlanHash; State = RepairItemState.ReadyToAssign;
    }
    public void Assign(Guid crew, Guid actor, UserRoleCode role, DateTimeOffset at)
    {
        RepairGuards.Id(crew); RepairGuards.Actor(actor, role, UserRoleCode.ProjectManager); Require(RepairItemState.ReadyToAssign);
        var time = EventTime(at); CrewId = crew; AssignedBy = actor; AssignedAt = time; State = RepairItemState.Assigned;
    }
    /// <summary>Records work starting; caller must separately establish normal/FT execution permission.
    /// Measuring start in H3 is distinct and is not inferred from this transition.</summary>
    public void Start(Guid crew, DateTimeOffset at)
    {
        RepairGuards.Id(crew); Require(RepairItemState.Assigned);
        if (crew != CrewId) throw new InvalidOperationException("Only the assigned Crew records work start.");
        var time = EventTime(at); StartedAt = time; State = RepairItemState.InProgress;
    }
    public void Submit(RepairAttempt attempt)
    {
        ArgumentNullException.ThrowIfNull(attempt); Require(RepairItemState.InProgress);
        if (attempt.ItemId != Id || attempt.ObligationId != ObligationId || attempt.DefectId != DefectId ||
            attempt.ProjectId != ProjectId || attempt.CrewId != CrewId || attempt.StartedAt is not null && attempt.StartedAt != StartedAt ||
            Mode == RepairMode.FastTrack && attempt.Performed && attempt.AuthorizationId is null)
            throw new InvalidOperationException("Attempt must describe this assigned item and original work start.");
        _attempts.Add(attempt); State = RepairItemState.Submitted;
    }
    public void Review(Guid actor, UserRoleCode role, DateTimeOffset at)
        => Review(actor, role, at, _attempts.Count == 0 ? [] : _attempts[^1].Evidence);
    public void Review(Guid actor, UserRoleCode role, DateTimeOffset at,
        IReadOnlyList<RepairEvidenceReference> effectiveEvidence)
    {
        ArgumentNullException.ThrowIfNull(effectiveEvidence);
        RepairGuards.Actor(actor, role, UserRoleCode.ProjectManager); Require(RepairItemState.Submitted);
        var latest = _attempts[^1];
        if (!latest.Performed || latest.TimeProvenance == RepairTimeProvenance.Uncertain || latest.StartedAt is null || latest.FinishedAt is null ||
            !effectiveEvidence.Any(file => file.Purpose == RepairEvidencePurpose.After) ||
            effectiveEvidence.Any(file => !file.Verified || !file.Related || file.Purpose == RepairEvidencePurpose.After && file.CapturedAt is null))
            throw new InvalidOperationException("Performed claim requires sufficient verified evidence and established execution facts.");
        var time = EventTime(at); ReviewedBy = actor; ReviewedAt = time; State = RepairItemState.Reviewed;
    }
    public RepairDecision Confirm(Guid id, Guid actor, UserRoleCode role, string reason, DateTimeOffset at,
        Guid? previousObligationHeadDecisionId = null)
    {
        RepairGuards.Id(id); RepairGuards.Actor(actor, role, Mode == RepairMode.Normal ? UserRoleCode.Supervisor : UserRoleCode.ProjectManager);
        Require(RepairItemState.Reviewed);
        if (previousObligationHeadDecisionId == Guid.Empty) throw new ArgumentException("A previous head must be an actual decision.");
        var decision = new RepairDecision(id, Id, ObligationId, DefectId, Mode, actor, role, RepairGuards.Text(reason), EventTime(at), null,
            RepairPresentationState.Confirmed, PreviousObligationHeadDecisionId: previousObligationHeadDecisionId);
        _decisions.Add(decision); EffectiveDecisionId = decision.Id; State = RepairItemState.Confirmed; return decision;
    }
    public RepairDecision Correct(Guid id, Guid supersedes, Guid actor, UserRoleCode role, string reason, DateTimeOffset at,
        RepairCorrectionAuthority? permission)
        => CorrectResult(id, supersedes, actor, role, reason, at, permission, RepairPresentationState.ReportedAwaitingReview,
            RepairCorrectionBasis.Create(reason, []));
    public RepairDecision CorrectResult(Guid id, Guid supersedes, Guid actor, UserRoleCode role, string reason, DateTimeOffset at,
        RepairCorrectionAuthority? authority, RepairPresentationState result, RepairCorrectionBasis basis)
    {
        RepairGuards.Id(id); RepairGuards.Id(supersedes); RepairGuards.EnumValue(result); ArgumentNullException.ThrowIfNull(basis);
        RepairGuards.Actor(actor, role, Mode == RepairMode.Normal ? UserRoleCode.Supervisor : UserRoleCode.ProjectManager);
        if (SupersededByItemId is not null || State is not (RepairItemState.Confirmed or RepairItemState.CorrectionRequired) ||
            authority is null || authority.ProjectMembershipId == Guid.Empty || authority.ItemId != Id || authority.ActorId != actor ||
            authority.Role != role || string.IsNullOrWhiteSpace(authority.SourceLabel) || EffectiveDecision?.Id != supersedes ||
            _decisions.Any(decision => decision.Id == id))
            throw new InvalidOperationException("Correction requires current scoped authority and the unique effective decision head.");
        var decision = new RepairDecision(id, Id, ObligationId, DefectId, Mode, actor, role, RepairGuards.Text(reason), EventTime(at), supersedes, result, basis, supersedes);
        _decisions.Add(decision);
        EffectiveDecisionId = decision.Id;
        State = decision.Accepted ? RepairItemState.Confirmed : RepairItemState.CorrectionRequired;
        return decision;
    }
    public RepairReviewRequest RequestReview(Guid id, Guid actor, UserRoleCode role, string reason, DateTimeOffset at)
    {
        RepairGuards.Id(id); RepairGuards.Id(actor);
        if (role is not (UserRoleCode.ProjectManager or UserRoleCode.RepairCrew) ||
            role == UserRoleCode.RepairCrew && actor != CrewId || EffectiveDecision is null ||
            _reviewRequests.Any(request => request.Id == id))
            throw new InvalidOperationException("An authorized PM or assigned Crew can request review without replacing the decision.");
        var request = new RepairReviewRequest(id, ProjectId, Id, EffectiveDecision.Id, actor, role, RepairGuards.Text(reason), EventTime(at));
        _reviewRequests.Add(request); return request;
    }
    public void Cancel(Guid actor, UserRoleCode role, string reason, DateTimeOffset at, RepairWorkHandover? handover)
    {
        RepairGuards.Actor(actor, role, UserRoleCode.ProjectManager);
        if (State is RepairItemState.Submitted or RepairItemState.Reviewed or RepairItemState.Confirmed or RepairItemState.Cancelled or RepairItemState.CorrectionRequired)
            throw new InvalidOperationException("Submitted and later history cannot be cancelled.");
        var time = EventTime(at); var cancellationReason = RepairGuards.Text(reason);
        if (State == RepairItemState.InProgress)
        {
            if (handover is null) throw new InvalidOperationException("In-progress cancellation requires performed scope and handover.");
            RepairGuards.Id(handover.Id); RepairGuards.Id(handover.FromActorId); RepairGuards.Id(handover.ToActorId);
            RepairGuards.Text(handover.PerformedScope); RepairGuards.Text(handover.SafetyState);
            if (handover.FromActorId != CrewId || handover.At.ToUniversalTime() != time)
                throw new InvalidOperationException("Handover must identify current Crew and cancellation milestone.");
        }
        Handover = handover; CancelledBy = actor; CancelledAt = time; CancellationReason = cancellationReason; State = RepairItemState.Cancelled;
    }
    internal void LinkNormalSuccessor(RepairItem next)
    {
        if (SupersededByItemId is not null || State is not (RepairItemState.Cancelled or RepairItemState.CorrectionRequired) ||
            next.Mode != RepairMode.Normal || next.State != RepairItemState.AwaitingApproval ||
            next.ObligationId != ObligationId || next.ProjectId != ProjectId || next.DefectId != DefectId ||
            next.Id == Id || next.PredecessorItemId is not null)
            throw new InvalidOperationException("Continuation must preserve the actual unresolved obligation.");
        _ = EventTime(next.ProposedAt);
        SupersededByItemId = next.Id; next.PredecessorItemId = Id;
    }
    private void Require(RepairItemState expected)
    {
        if (State != expected) throw new InvalidOperationException("Invalid repair state transition.");
    }
    private DateTimeOffset EventTime(DateTimeOffset at)
    {
        var time = RepairGuards.Time(at);
        var previous = EffectiveDecision?.At ?? ReviewedAt ?? _attempts.LastOrDefault()?.ServerReceivedAt ?? StartedAt ?? AssignedAt ?? ApprovedAt ?? ProposedAt;
        if (time < previous) throw new ArgumentException("Transition cannot predate its predecessor.");
        return time;
    }
}
public static class RepairPackageCompletion
{
    public static bool AllMandatoryResolved(IReadOnlyList<RepairObligation> obligations)
    {
        ArgumentNullException.ThrowIfNull(obligations); return obligations.All(obligation => !obligation.Mandatory || obligation.IsResolved);
    }
    // Explicit command authority/history is the service responsibility; this is only the obligation gate.
    public static void EnsureDefectClosable(Guid defect, IReadOnlyList<RepairObligation> obligations)
    {
        RepairGuards.Id(defect); ArgumentNullException.ThrowIfNull(obligations);
        if (obligations.Any(obligation => obligation.DefectId != defect) || !AllMandatoryResolved(obligations))
            throw new InvalidOperationException("Every mandatory obligation must be resolved before explicit closure.");
    }
}
public static class RepairRecurrence
{
    public static RepairRecurrenceKind Classify(RepairItem item)
    {
        ArgumentNullException.ThrowIfNull(item); return item.IsEffectivelyConfirmed ? RepairRecurrenceKind.NewLinkedDefect : RepairRecurrenceKind.CorrectionRequired;
    }
    public static RepairRecurrenceLink LinkNewDefect(Guid newDefect, RepairItem item, Guid actor, string reason, DateTimeOffset at)
    {
        RepairGuards.Id(newDefect); RepairGuards.Id(actor); ArgumentNullException.ThrowIfNull(item);
        if (newDefect == item.DefectId || !item.IsEffectivelyConfirmed)
            throw new InvalidOperationException("True recurrence requires a new Defect and effective successful prior repair.");
        var time = RepairGuards.Time(at);
        if (time < item.EffectiveDecision!.At) throw new ArgumentException("Recurrence cannot predate successful acceptance.");
        return new(newDefect, item.DefectId, item.EffectiveDecision.Id, actor, RepairGuards.Text(reason), time);
    }
}
