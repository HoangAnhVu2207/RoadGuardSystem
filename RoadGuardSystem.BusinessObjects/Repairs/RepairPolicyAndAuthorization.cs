using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Repairs;

public enum RepairFactState : byte { Unknown = 0, Confirmed = 1, Denied = 2 }
public enum RepairTimeProvenance : byte { Uncertain = 0, VerifiedOnline = 1, VerifiedOffline = 2 }
public enum RepairTaskMode : byte { MeasureOnly = 1, ConditionalFastTrack = 2 }
public enum RepairEligibilityReason : byte
{
    Eligible = 0, NotConfigured = 1, UnknownMeasurement = 2, WrongUnit = 3, ThresholdNotMet = 4,
    StopCondition = 5, MeasureOnly = 6, UnprovenTime = 7, Expired = 8, ScopeMismatch = 9,
    CurrentAuthorityDenied = 10, RoadNotHandedOver = 11, CoverageUnknown = 12, CoverageDenied = 13,
    PolicyUnavailable = 14, LocationUnverified = 15, ChecklistIncomplete = 16, Revoked = 17
}
public sealed record RepairEligibilityResult(bool Eligible, RepairEligibilityReason Reason);
public sealed record RepairMeasurementRule(string Code, string Unit, decimal Minimum, decimal Maximum);
public sealed record RepairMeasurementFact(string Code, decimal? Value, string Unit);
public sealed record RepairPolicyRevocation(Guid Id, Guid ActorId, string Reason, DateTimeOffset At);
public sealed record RepairExecutionFacts(Guid ProjectId, Guid DefectId, Guid TaskId, Guid AssignmentId,
    Guid CrewId, string LocationVersion, Guid PolicyRevisionId, bool CurrentAuthority, bool RoadHandedOver,
    RepairFactState Coverage, bool PolicyCurrentAuthority, bool MeasurementsEligible, bool LocationVerified,
    bool ChecklistIncomplete, bool KnownRevoked);

/// <summary>Immutable publication. Caller must prove current project authority and source facts.</summary>
public sealed class RepairPolicyRevision
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public int Revision { get; private set; }
    public Guid PublishedBy { get; private set; }
    public DateTimeOffset PublishedAt { get; private set; }
    public string DefectTypeCode { get; private set; } = "";
    public string ChecklistVersion { get; private set; } = "";
    public IReadOnlyList<RepairMeasurementRule> Measurements => _measurements.AsReadOnly();
    private readonly List<RepairMeasurementRule> _measurements = [];
    public IReadOnlyList<string> StopConditions { get; private set; } = [];
    public bool IsRevoked => _revocations.Count != 0;
    public IReadOnlyList<RepairPolicyRevocation> Revocations => _revocations.AsReadOnly();
    private readonly List<RepairPolicyRevocation> _revocations = [];
    private RepairPolicyRevision() { }
    public static RepairPolicyRevision Publish(Guid id, Guid project, int revision, Guid actor, UserRoleCode role,
        DateTimeOffset at, string defectType, string checklist, IReadOnlyList<RepairMeasurementRule> measurements,
        IReadOnlyList<string> stopConditions)
    {
        RepairGuards.Id(id); RepairGuards.Id(project); RepairGuards.Actor(actor, role, UserRoleCode.ProjectManager);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(revision);
        ArgumentNullException.ThrowIfNull(measurements); ArgumentNullException.ThrowIfNull(stopConditions);
        var rules = measurements.Select(rule =>
        {
            ArgumentNullException.ThrowIfNull(rule);
            const decimal maximumStoredRule = 99999999999999.999999m;
            if (rule.Minimum < 0 || rule.Maximum < rule.Minimum || rule.Maximum > maximumStoredRule ||
                decimal.Round(rule.Minimum, 6) != rule.Minimum || decimal.Round(rule.Maximum, 6) != rule.Maximum)
                throw new ArgumentException("Explicit measurement bounds must be ordered, nonnegative and exactly fit decimal(20,6) storage.");
            return new RepairMeasurementRule(RepairGuards.Text(rule.Code), RepairGuards.Text(rule.Unit), rule.Minimum, rule.Maximum);
        }).ToArray();
        var stops = stopConditions.Select(RepairGuards.Text).ToArray();
        if (rules.Select(rule => rule.Code).Distinct(StringComparer.Ordinal).Count() != rules.Length ||
            stops.Distinct(StringComparer.Ordinal).Count() != stops.Length) throw new ArgumentException("Configured fact identifiers must be unique.");
        var policy = new RepairPolicyRevision { Id = id, ProjectId = project, Revision = revision, PublishedBy = actor,
            PublishedAt = RepairGuards.Time(at), DefectTypeCode = RepairGuards.Text(defectType), ChecklistVersion = RepairGuards.Text(checklist),
            StopConditions = Array.AsReadOnly(stops) };
        policy._measurements.AddRange(rules); return policy;
    }
    public RepairEligibilityResult Evaluate(IReadOnlyList<RepairMeasurementFact> facts, IReadOnlyList<string> activeStopConditions)
    {
        ArgumentNullException.ThrowIfNull(facts); ArgumentNullException.ThrowIfNull(activeStopConditions);
        if (facts.Any(fact => fact is null || string.IsNullOrWhiteSpace(fact.Code) || string.IsNullOrWhiteSpace(fact.Unit)) ||
            facts.Select(fact => fact.Code).Distinct(StringComparer.Ordinal).Count() != facts.Count)
            throw new ArgumentException("Measurement facts require unique codes and explicit units.");
        if (IsRevoked) return new(false, RepairEligibilityReason.PolicyUnavailable);
        if (Measurements.Count == 0) return new(false, RepairEligibilityReason.NotConfigured);
        if (StopConditions.Any(condition => activeStopConditions.Contains(condition, StringComparer.Ordinal)))
            return new(false, RepairEligibilityReason.StopCondition);
        foreach (var rule in Measurements)
        {
            var fact = facts.SingleOrDefault(fact => fact.Code == rule.Code);
            if (fact?.Value is null) return new(false, RepairEligibilityReason.UnknownMeasurement);
            if (!string.Equals(fact.Unit, rule.Unit, StringComparison.Ordinal)) return new(false, RepairEligibilityReason.WrongUnit);
            if (fact.Value < rule.Minimum || fact.Value > rule.Maximum) return new(false, RepairEligibilityReason.ThresholdNotMet);
        }
        return new(true, RepairEligibilityReason.Eligible);
    }
    public void Revoke(Guid id, Guid actor, UserRoleCode role, string reason, DateTimeOffset at)
    {
        RepairGuards.Id(id); RepairGuards.Actor(actor, role, UserRoleCode.ProjectManager);
        var time = RepairGuards.Time(at); var why = RepairGuards.Text(reason);
        if (time < PublishedAt) throw new ArgumentException("Revocation cannot predate publication.");
        if (IsRevoked)
        {
            if (_revocations[0] == new RepairPolicyRevocation(id, actor, why, time)) return;
            throw new InvalidOperationException("Published policy revocation cannot be rewritten.");
        }
        _revocations.Add(new(id, actor, why, time));
    }
}

/// <summary>Execution permission, independent of session and review/sync deadline extensions.
/// VerifiedOffline is a supplied reconciler fact; this model does not verify phone clocks.</summary>
public sealed class RepairExecutionAuthorization
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid DefectId { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid AssignmentId { get; private set; }
    public Guid CrewId { get; private set; }
    public Guid IssuedBy { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public string Reason { get; private set; } = "";
    public RepairTaskMode Permission { get; private set; }
    public string LocationVersion { get; private set; } = "";
    public Guid PolicyRevisionId { get; private set; }
    public Guid? FirstStartOriginId { get; private set; }
    public string? FirstStartPayloadHash { get; private set; }
    public DateTimeOffset? FirstStartedAt { get; private set; }
    public DateTimeOffset? VerifiedStartedAt { get; private set; }
    public DateTimeOffset? FirstServerReceivedAt { get; private set; }
    public RepairTimeProvenance TimeProvenance { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    private RepairExecutionAuthorization() { }
    public static RepairExecutionAuthorization Issue(Guid id, Guid project, Guid defect, Guid task, Guid assignment,
        Guid crew, Guid actor, DateTimeOffset at, string reason, RepairTaskMode permission, string locationVersion,
        Guid policyRevision)
    {
        RepairGuards.Id(id); RepairGuards.Id(project); RepairGuards.Id(defect); RepairGuards.Id(task);
        RepairGuards.Id(assignment); RepairGuards.Id(crew); RepairGuards.Id(actor); RepairGuards.Id(policyRevision);
        RepairGuards.EnumValue(permission);
        return new RepairExecutionAuthorization { Id = id, ProjectId = project, DefectId = defect, TaskId = task,
            AssignmentId = assignment, CrewId = crew, IssuedBy = actor, IssuedAt = RepairGuards.Time(at), Reason = RepairGuards.Text(reason),
            Permission = permission, LocationVersion = RepairGuards.Text(locationVersion), PolicyRevisionId = policyRevision };
    }
    public void RecordFirstStart(Guid origin, string hash, DateTimeOffset startedAt, DateTimeOffset receivedAt, RepairTimeProvenance provenance)
    {
        RepairGuards.Id(origin); var normalizedHash = RepairGuards.Text(hash);
        var start = RepairGuards.Time(startedAt); var received = RepairGuards.Time(receivedAt); RepairGuards.EnumValue(provenance);
        if (FirstStartOriginId is not null)
        {
            if (FirstStartOriginId != origin || FirstStartPayloadHash != normalizedHash || FirstStartedAt != start)
                throw new InvalidOperationException("The original first start cannot be replaced.");
            return;
        }
        DateTimeOffset? verified = provenance switch
        {
            RepairTimeProvenance.VerifiedOnline => received,
            RepairTimeProvenance.VerifiedOffline => start,
            _ => null
        };
        if (verified < IssuedAt) throw new ArgumentException("An established start cannot predate authorization.");
        FirstStartOriginId = origin; FirstStartPayloadHash = normalizedHash; FirstStartedAt = start;
        FirstServerReceivedAt = received; TimeProvenance = provenance; VerifiedStartedAt = verified;
        ExpiresAt = verified?.AddHours(24);
    }
    /// <summary>Caller must prove original-event identity and actual original UTC time from the source.
    /// The time supplied here is never the later proof-receipt or sync timestamp. This method supplies no proof adapter.</summary>
    public void VerifyOriginalStart(Guid origin, string hash, DateTimeOffset startedAt, DateTimeOffset verifiedUtcStart, RepairTimeProvenance provenance)
    {
        RepairGuards.EnumValue(provenance);
        var verified = RepairGuards.Time(verifiedUtcStart);
        if (provenance == RepairTimeProvenance.Uncertain || FirstStartOriginId != origin ||
            FirstStartPayloadHash != hash || FirstStartedAt != startedAt.ToUniversalTime())
            throw new InvalidOperationException("Proof must resolve the unchanged original first start.");
        if (verified < IssuedAt || provenance == RepairTimeProvenance.VerifiedOnline && verified != FirstServerReceivedAt)
            throw new ArgumentException("Established original start must match authorization and its actual source.");
        if (VerifiedStartedAt is not null && VerifiedStartedAt != verified)
            throw new InvalidOperationException("Established original start cannot be moved by later proof.");
        if (TimeProvenance != RepairTimeProvenance.Uncertain && TimeProvenance != provenance)
            throw new InvalidOperationException("Established start proof cannot be rewritten.");
        TimeProvenance = provenance; VerifiedStartedAt = verified; ExpiresAt = verified.AddHours(24);
    }
    public RepairEligibilityResult Evaluate(DateTimeOffset newRepairStart, RepairExecutionFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var at = RepairGuards.Time(newRepairStart);
        if (Permission == RepairTaskMode.MeasureOnly) return new(false, RepairEligibilityReason.MeasureOnly);
        if (facts.ProjectId != ProjectId || facts.DefectId != DefectId || facts.TaskId != TaskId || facts.AssignmentId != AssignmentId ||
            facts.CrewId != CrewId || facts.LocationVersion != LocationVersion || facts.PolicyRevisionId != PolicyRevisionId)
            return new(false, RepairEligibilityReason.ScopeMismatch);
        if (!facts.CurrentAuthority) return new(false, RepairEligibilityReason.CurrentAuthorityDenied);
        if (facts.KnownRevoked) return new(false, RepairEligibilityReason.Revoked);
        if (ExpiresAt is null || at < VerifiedStartedAt) return new(false, RepairEligibilityReason.UnprovenTime);
        if (at >= ExpiresAt) return new(false, RepairEligibilityReason.Expired);
        if (!facts.RoadHandedOver) return new(false, RepairEligibilityReason.RoadNotHandedOver);
        if (facts.Coverage == RepairFactState.Unknown) return new(false, RepairEligibilityReason.CoverageUnknown);
        if (facts.Coverage != RepairFactState.Confirmed) return new(false, RepairEligibilityReason.CoverageDenied);
        if (!facts.PolicyCurrentAuthority) return new(false, RepairEligibilityReason.PolicyUnavailable);
        if (!facts.MeasurementsEligible) return new(false, RepairEligibilityReason.ThresholdNotMet);
        if (!facts.LocationVerified) return new(false, RepairEligibilityReason.LocationUnverified);
        if (facts.ChecklistIncomplete) return new(false, RepairEligibilityReason.ChecklistIncomplete);
        return new(true, RepairEligibilityReason.Eligible);
    }
}
