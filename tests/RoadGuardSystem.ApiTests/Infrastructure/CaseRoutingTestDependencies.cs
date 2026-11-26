using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Implementations.Cases;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Services.Implementations.Cases;
using RoadGuardSystem.Services.Options;

namespace RoadGuardSystem.ApiTests.Infrastructure;

internal static class CaseRoutingTestDependencies
{
    internal static IRoutingMapRepository Create(RoadGuardDbContext db) => new RoutingMapRepository(db,
        new CaseWorkflowRepository(db), new AnhHuyFactsRepository(db),
        new RoutingCursorCodec(new JwtOptions
        {
            ActiveKeyId = "routing-test-only",
            SigningKeys = new() { ["routing-test-only"] = Convert.ToBase64String(new byte[32]) }
        }));
}
