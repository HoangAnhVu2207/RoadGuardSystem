namespace RoadGuardSystem.BusinessObjects.Cases;

/// <summary>Append-only intake decision or triage supersession; no technical conclusion or publication.</summary>
public sealed class CaseRoutingEvent
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public int Sequence { get; set; }
    public string Kind { get; set; } = "DECISION";
    public string? Outcome { get; set; }
    public string Reason { get; set; } = "";
    public Guid ActorId { get; set; }
    public DateTimeOffset At { get; set; }
    public Guid ReportId { get; set; }
    public Guid? EvidenceId { get; set; }
    public string CaseSourceVersion { get; set; } = "";
    public string? EvaluationVersion { get; set; }
    public string RegistryVersion { get; set; } = "";
    public string TemporalBasis { get; set; } = "SERVER_RECEIVED_AT";
    public Guid? SupersedesId { get; set; }
    public string? SourceSnapshotJson { get; set; }
    public string? TriageSourceJson { get; set; }
}
