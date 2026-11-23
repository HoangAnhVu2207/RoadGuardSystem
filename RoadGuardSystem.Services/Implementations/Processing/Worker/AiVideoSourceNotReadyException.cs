namespace RoadGuardSystem.Services.Processing.Worker;

/// <summary>Retryable source readiness condition before any immutable job has been prepared.</summary>
public sealed class AiVideoSourceNotReadyException() : Exception("GEOMETRY_NOT_READY")
{
    public string Code { get; } = "GEOMETRY_NOT_READY";
}
