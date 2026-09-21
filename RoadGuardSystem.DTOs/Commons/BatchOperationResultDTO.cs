namespace RoadGuardSystem.DTOs.Commons;

public sealed class BatchOperationResultDTO
{
    public int TotalRequested { get; set; }

    public int SuccessCount { get; set; }

    public int FailureCount { get; set; }

    public List<string> SuccessIds { get; set; } = [];

    public List<BatchOperationErrorDTO> Errors { get; set; } = [];

    public string Message { get; set; } = string.Empty;
}
