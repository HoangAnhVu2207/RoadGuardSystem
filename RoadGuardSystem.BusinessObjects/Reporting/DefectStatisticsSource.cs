using RoadGuardSystem.aBusinessObjects.Commons;
namespace RoadGuardSystem.BusinessObjects.Reporting;

public sealed record DefectStatisticsSource(Guid Id, Guid ProjectId, Guid DefectId, string DefectVersion,
    Guid ObligationId, string ScopeHash, Guid RoadSectionId, Guid RouteVersionId, string LocationVersion,
    decimal From, decimal To, decimal OffsetFrom, decimal OffsetTo, Guid SharedPartId,
    string SegmentIdsJson, string QuantitiesJson, string SourceFactsJson, string Provenance,
    Guid ActorId, DateTimeOffset ConfirmedAtUtc, string Reason, Guid? SupersedesId);
public sealed record StatisticsQuantityInput(Guid MeasurementId, string SourceVersion, Guid? SegmentId);
public sealed record DefectStatisticsInput(Guid ObligationId, string ScopeHash, string DefectVersion, Guid SharedPartId,
    Guid[] SegmentIds, StatisticsQuantityInput[] Quantities, string Provenance, string Reason, Guid? SupersedesId = null);
public sealed record DefectStatisticsCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId,
    DefectStatisticsInput Input, string Key, string ExpectedVersion);
public sealed record StatisticsScope(Guid ObligationId, Guid DefectId, string DefectVersion, string ScopeHash,
    Guid RoadSectionId, Guid RouteVersionId, string LocationVersion, decimal From, decimal To, decimal OffsetFrom, decimal OffsetTo);
public sealed record StatisticsSegment(Guid Id, Guid RouteVersionId, Guid SegmentSetId, string Version);
public sealed record StatisticsMeasurement(Guid Id, Guid DefectId, Guid RouteVersionId, string Type,
    decimal? Value, string ValueState, string Unit, string Version, string? UnknownReason, Guid? EvidenceFileId);
public sealed record StatisticsSegmentCount(Guid SegmentId, long? RelatedDefects, long KnownRelatedDefects, string Availability);
public sealed record StatisticsQuantity(Guid SharedPartId, Guid MeasurementId, string SourceVersion,
    string Type, string Unit, decimal? Value, string ValueState, Guid? SegmentId, string Provenance, string? Reason);
public sealed record StatisticsDefect(Guid Id, string Version);
public sealed record DefectStatisticsSnapshot(Guid ProjectId, StatisticsDefect[] Defects, long ProjectDistinctDefects, long MatchedDistinctDefects,
    long SharedParts, long UnmappedDefects, StatisticsSegmentCount[] Segments,
    StatisticsQuantity[] Quantities, string[] MissingReasons, DefectStatisticsSource[] Sources, string Version);
public sealed record DefectStatisticsRead(StatisticsScope[] Scopes, StatisticsSegment[] Segments,
    StatisticsMeasurement[] Measurements, DefectStatisticsSource[] History, DefectStatisticsSnapshot Statistics, string Version);
public sealed record DefectStatisticsResult(int Status, string? Code = null, DefectStatisticsRead? Value = null);
