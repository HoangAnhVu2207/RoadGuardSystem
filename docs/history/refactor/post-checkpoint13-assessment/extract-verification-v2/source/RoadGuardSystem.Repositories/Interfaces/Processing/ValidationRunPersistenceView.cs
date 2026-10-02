namespace RoadGuardSystem.Repositories.Processing;

public sealed record ValidationRunPersistenceView(Guid Id, Guid ProjectId, string Status, string Version, int UsedCount, int ExcludedCount, decimal? Bias, decimal? Mae, decimal? Rmse, string Unit, string ExclusionReasonsJson);
