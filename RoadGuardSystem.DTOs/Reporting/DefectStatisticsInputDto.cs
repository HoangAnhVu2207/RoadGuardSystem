using System.Text.Json.Serialization;
using RoadGuardSystem.BusinessObjects.Reporting;
namespace RoadGuardSystem.DTOs.Reporting;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record StatisticsQuantityInputDto(Guid MeasurementId, string SourceVersion, Guid? SegmentId);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DefectStatisticsInputDto(Guid ObligationId, string ScopeHash, string DefectVersion, Guid SharedPartId,
    Guid[] SegmentIds, StatisticsQuantityInputDto[] Quantities, string Provenance, string Reason, Guid? SupersedesId = null)
{
    public DefectStatisticsInput ToFact() => new(ObligationId, ScopeHash, DefectVersion, SharedPartId, SegmentIds,
        Quantities?.Select(row => row is null ? null! : new StatisticsQuantityInput(row.MeasurementId, row.SourceVersion, row.SegmentId)).ToArray()!,
        Provenance, Reason, SupersedesId);
}
