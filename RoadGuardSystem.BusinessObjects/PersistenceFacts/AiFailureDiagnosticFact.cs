namespace RoadGuardSystem.BusinessObjects.PersistenceFacts.Processing;

public sealed record AiFailureDiagnosticFact(string ReasonCode, string Step, string? Field, string Recovery,
    Guid? OriginCorrelationId);
