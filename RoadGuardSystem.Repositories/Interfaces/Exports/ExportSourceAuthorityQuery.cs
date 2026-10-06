namespace RoadGuardSystem.Repositories.Exports;

// Current authority over source identities, independent of the immutable snapshot's historical versions.
// A present empty case inventory still requires current private-source authority.
public sealed record ExportSourceAuthorityReference(string Kind, Guid Id);
public sealed record ExportCaseDefectAuthorityFacts(Guid ProjectId, Guid[] CaseIds, Guid[] DefectIds);
public sealed record ExportSourceAuthorityQuery(Guid ManifestProjectId, ExportSourceAuthorityReference[] RepairSources,
    bool HasAvailableRepairMetric, ExportCaseDefectAuthorityFacts? CaseFacts);
