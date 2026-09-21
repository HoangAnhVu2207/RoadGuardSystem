using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Warranties;

public sealed record WarrantyCreationFacts(
    ProjectStatus? ProjectStatus,
    bool RoadSectionMatchesProject,
    bool HandoverDocumentMatchesProject,
    bool SourceDocumentExists);
