namespace RoadGuardSystem.BusinessObjects.Repairs;

/// <summary>Immutable Supervisor confirmation of sourced coverage for one exact road frame and interval.
/// Provenance is retained; synthetic sources cannot establish execution permission.</summary>
public sealed record RoadCoverageMapping(Guid Id, Guid ProjectId, Guid ScopeObligationId, Guid RoadSectionId,
    string LocationVersion, decimal From, decimal To, decimal OffsetFrom, decimal OffsetTo,
    DateTimeOffset ApplicableFromUtc, DateTimeOffset ApplicableToUtc, Guid HandoverDocumentId,
    string HandoverVersion, string SourceKind, Guid SourceId, string SourceVersion,
    Guid HandoverFileId, Guid CoverageFileId, string SourceFactsJson, string Provenance,
    Guid ActorId, DateTimeOffset ConfirmedAtUtc, string Reason, Guid? SupersedesId);

public sealed record RoadCoverageInputFact(Guid ScopeObligationId, string ExpectedScopeHash,
    Guid HandoverDocumentId, string HandoverVersion, string SourceKind, Guid SourceId, string SourceVersion,
    DateTimeOffset ApplicableFromUtc, DateTimeOffset ApplicableToUtc, string Provenance, string Reason,
    Guid? SupersedesId = null);
public sealed record RoadCoverageScopeFact(Guid ObligationId, Guid RoadSectionId, string LocationVersion,
    decimal From, decimal To, decimal OffsetFrom, decimal OffsetTo, string ScopeHash);
public sealed record RoadCoverageSourceFact(string Kind, Guid Id, Guid FileId, string Version,
    DateOnly? From = null, DateOnly? To = null, string Provenance = "SOURCE_NOT_EXTERNALLY_VERIFIED");
public sealed record RoadCoverageReadFact(Guid ProjectId, RoadCoverageScopeFact[] Scopes,
    RoadCoverageSourceFact[] Sources, RoadCoverageMapping[] History, string Version);
public sealed record RoadCoverageResolution(string State, RoadCoverageMapping? Mapping = null)
{
    public bool PermitsExecution => State == "CONFIRMED" && Mapping?.Provenance == "REAL_SOURCE";
}
