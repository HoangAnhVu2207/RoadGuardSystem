namespace RoadGuardSystem.DTOs.Commons;

public sealed class BatchOperationErrorDTO
{
    public string Id { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;

    public string ErrorCode { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; }
}
