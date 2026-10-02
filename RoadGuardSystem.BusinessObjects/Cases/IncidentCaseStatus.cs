namespace RoadGuardSystem.BusinessObjects.Cases;

public enum IncidentCaseStatus
{
    Unknown = 0,
    Unassigned = 1,
    Open = 2,
    AwaitingEvidence = 3,
    Concluded = 4,
    Linked = 5
}
