using System.Text.Json.Serialization;
using RoadGuardSystem.BusinessObjects.Repairs;

namespace RoadGuardSystem.DTOs.Repairs;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RoadCoverageInputDto(Guid ScopeObligationId, string ExpectedScopeHash,
    Guid HandoverDocumentId, string HandoverVersion, string SourceKind, Guid SourceId, string SourceVersion,
    DateTimeOffset ApplicableFromUtc, DateTimeOffset ApplicableToUtc, string Provenance, string Reason,
    Guid? SupersedesId = null)
{
    public RoadCoverageInputFact ToFact() => new(ScopeObligationId, ExpectedScopeHash, HandoverDocumentId, HandoverVersion,
        SourceKind, SourceId, SourceVersion, ApplicableFromUtc, ApplicableToUtc, Provenance, Reason, SupersedesId);
}
public sealed record RoadCoverageServiceResult(int Status, string? Code = null, RoadCoverageReadFact? Value = null);
