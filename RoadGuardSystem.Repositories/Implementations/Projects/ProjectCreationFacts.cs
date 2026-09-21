namespace RoadGuardSystem.Repositories.Projects;

public sealed record ProjectCreationFacts(
    bool PrimaryProjectManagerIsEligible,
    bool HandoverFileExists);
