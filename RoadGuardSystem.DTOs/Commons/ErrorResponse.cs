namespace RoadGuardSystem.DTOs.Commons;

public sealed class ErrorResponse
{
    public bool IsSuccess { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? Details { get; set; }

    public string? ErrorType { get; set; }

    public string? ErrorCode { get; set; }

    public DateTime Timestamp { get; set; }

    public Dictionary<string, object>? AdditionalData { get; set; }
}
