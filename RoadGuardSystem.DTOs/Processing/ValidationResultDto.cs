namespace RoadGuardSystem.DTOs.Processing;

public sealed record ValidationResultDto(
    Guid Id,
    int UsedCount,
    int ExcludedCount,
    decimal? Bias,
    decimal? Mae,
    decimal? Rmse,
    string Unit,
    IReadOnlyList<string> ExclusionReasons);
