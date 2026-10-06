namespace RoadGuardSystem.BusinessObjects.Repairs;

public enum RepairSafetyCheckResult : byte { Safe = 1, NeedsReplacement = 2, Danger = 3, Unknown = 4 }
public sealed record RepairSafetyEvidenceReference(Guid FileId, string? FileVersion, string? Hash,
    RepairEvidencePurpose Purpose, bool Verified, bool Related, string SourceKind, Guid SourceId,
    DateTimeOffset? CapturedAt, Guid? ReuseDecisionId, bool ReusedSource, Guid? ActualUploaderId);
public sealed record RepairSafetyCheck(Guid Id, Guid MeasureId, Guid ActorId, DateTimeOffset At,
    RepairSafetyCheckResult Result, string Findings, IReadOnlyList<Guid> EvidenceIds)
{
    private List<RepairSafetyEvidenceReference> _evidence = [];
    public IReadOnlyList<RepairSafetyEvidenceReference> Evidence
    { get => _evidence.AsReadOnly(); init => _evidence = value.ToList(); }
}
public sealed record RepairDangerWarning(Guid Id, Guid SourceId, Guid ResponsibleActorId,
    DateTimeOffset ServerReceivedAt, DateTimeOffset OriginalAcknowledgementDueAt, string Reason)
{
    public Guid MonitoringId { get; init; }
}
public sealed record RepairDangerAcknowledgement(Guid Id, Guid WarningId, Guid ActorId, DateTimeOffset At,
    string Reason, bool AfterOriginalDue)
{
    public Guid MonitoringId { get; init; }
}

/// <summary>History and original clock facts only; SQL/current action authority remain production command responsibilities.</summary>
public sealed class RepairSafetyMonitoring
{
    public Guid Id { get; private set; }
    public Guid MeasureId { get; private set; }
    public Guid SafetyObligationId { get; private set; }
    public Guid FormalObligationId { get; private set; }
    public Guid? CurrentCheckId { get; private set; }
    public TemporarySafetyMeasure Measure { get; private set; } = null!;
    public RepairObligation SafetyObligation { get; private set; } = null!;
    public RepairObligation FormalObligation { get; private set; } = null!;
    public IReadOnlyList<RepairSafetyCheck> Checks => _checks.AsReadOnly();
    public IReadOnlyList<RepairDangerWarning> Warnings => _warnings.AsReadOnly();
    public IReadOnlyList<RepairDangerAcknowledgement> Acknowledgements => _acknowledgements.AsReadOnly();
    private readonly List<RepairSafetyCheck> _checks = [];
    private readonly List<RepairDangerWarning> _warnings = [];
    private readonly List<RepairDangerAcknowledgement> _acknowledgements = [];
    private RepairSafetyMonitoring() { }
    public static RepairSafetyMonitoring Create(TemporarySafetyMeasure measure, RepairObligation safety, RepairObligation formal)
    {
        ArgumentNullException.ThrowIfNull(measure); ArgumentNullException.ThrowIfNull(safety); ArgumentNullException.ThrowIfNull(formal);
        if (safety.Kind != RepairObligationKind.TemporarySafety || formal.Kind != RepairObligationKind.FormalRepair ||
            safety.ProjectId != measure.ProjectId || formal.ProjectId != measure.ProjectId ||
            safety.DefectId != measure.DefectId || formal.DefectId != measure.DefectId || formal.Id != measure.FormalRepairObligationId)
            throw new InvalidOperationException("Monitoring requires the actual related safety and formal obligations.");
        return new RepairSafetyMonitoring { Id = measure.Id, MeasureId = measure.Id, SafetyObligationId = safety.Id,
            FormalObligationId = formal.Id, Measure = measure, SafetyObligation = safety, FormalObligation = formal };
    }
    public RepairSafetyCheck RecordCheck(Guid id, Guid actor, DateTimeOffset at, RepairSafetyCheckResult result,
        string findings, IReadOnlyList<Guid> evidence)
        => RecordCheckCore(id, actor, at, result, findings, evidence, []);

    public RepairSafetyCheck RecordVerifiedCheck(Guid id, Guid actor, DateTimeOffset at, RepairSafetyCheckResult result,
        string findings, IReadOnlyList<Guid> evidenceLinks, IReadOnlyList<RepairSafetyEvidenceReference> verifiedEvidence)
    {
        ArgumentNullException.ThrowIfNull(evidenceLinks);
        ArgumentNullException.ThrowIfNull(verifiedEvidence);
        if (verifiedEvidence.Count != evidenceLinks.Count ||
            verifiedEvidence.Any(row => row.Purpose != RepairEvidencePurpose.Safety || !row.Verified || !row.Related ||
                row.SourceKind != "FIELD_MEASUREMENT" || row.ActualUploaderId is not Guid uploader ||
                uploader == Guid.Empty) ||
            !verifiedEvidence.Select(row => row.SourceId).Order().SequenceEqual(evidenceLinks.Order()))
            throw new ArgumentException("Safety evidence must retain its verified scoped source links.", nameof(verifiedEvidence));
        return RecordCheckCore(id, actor, at, result, findings, evidenceLinks, verifiedEvidence);
    }

    private RepairSafetyCheck RecordCheckCore(Guid id, Guid actor, DateTimeOffset at, RepairSafetyCheckResult result,
        string findings, IReadOnlyList<Guid> evidence, IReadOnlyList<RepairSafetyEvidenceReference> verifiedEvidence)
    {
        RepairGuards.Id(id); RepairGuards.Id(actor); RepairGuards.EnumValue(result);
        var time = RepairGuards.Time(at); var text = RepairGuards.Text(findings); ArgumentNullException.ThrowIfNull(evidence);
        if (Measure.InstalledAt is null) throw new InvalidOperationException("Check requires completed installation.");
        if (time < Measure.InstalledAt || _checks.Count > 0 && time < _checks[^1].At)
            throw new ArgumentException("Check cannot predate installation or recorded checks.");
        if (_checks.Any(check => check.Id == id)) throw new InvalidOperationException("Check history cannot be overwritten.");
        if (evidence.Any(value => value == Guid.Empty) || evidence.Distinct().Count() != evidence.Count)
            throw new ArgumentException("Evidence references must be distinct actual identities.");
        var check = new RepairSafetyCheck(id, Measure.Id, actor, time, result, text, Array.AsReadOnly(evidence.ToArray()))
        { Evidence = verifiedEvidence };
        _checks.Add(check); CurrentCheckId = check.Id; return check;
    }
    public RepairDangerWarning ReceiveDangerWarning(Guid id, Guid source, DateTimeOffset serverReceivedAt, string reason)
    {
        RepairGuards.Id(id); RepairGuards.Id(source); var received = RepairGuards.Time(serverReceivedAt); var why = RepairGuards.Text(reason);
        var existing = _warnings.SingleOrDefault(warning => warning.Id == id || warning.SourceId == source);
        if (existing is not null)
        {
            if (existing.Id != id || existing.SourceId != source || existing.Reason != why)
                throw new InvalidOperationException("Warning occurrence identity and content cannot be replaced.");
            return existing;
        }
        var warning = new RepairDangerWarning(id, source, Measure.ResponsibleActorId, received, received.AddHours(1), why)
        { MonitoringId = Id };
        _warnings.Add(warning); return warning;
    }
    public RepairDangerAcknowledgement AcknowledgeDanger(Guid id, Guid warning, Guid actor, DateTimeOffset at, string reason)
    {
        RepairGuards.Id(id); RepairGuards.Id(actor); var time = RepairGuards.Time(at); var why = RepairGuards.Text(reason);
        var source = _warnings.SingleOrDefault(value => value.Id == warning) ?? throw new InvalidOperationException("Unknown warning occurrence.");
        if (time < source.ServerReceivedAt) throw new ArgumentException("Acknowledgement cannot predate warning receipt.");
        if (_acknowledgements.Any(value => value.Id == id || value.WarningId == warning))
            throw new InvalidOperationException("Original acknowledgement cannot be rewritten.");
        var acknowledgement = new RepairDangerAcknowledgement(id, warning, actor, time, why, time >= source.OriginalAcknowledgementDueAt)
        { MonitoringId = Id };
        _acknowledgements.Add(acknowledgement); return acknowledgement;
    }
}
