using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Offline;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Offline;
using RoadGuardSystem.TestFixtures;
using Xunit;

namespace RoadGuardSystem.ApiTests.Offline;

[Collection(AuthenticationApiFixture.Name)]
public sealed class ND01OriginalWindowHttpTests(AuthenticationSqlServerFixture fixture)
{
    [Fact]
    public async Task ActualBearerSignedSyncCommitsEligibleOriginalWindowFtExactlyOnce()
    {
        var pm = await fixture.CreateUserAsync("nd01-pm-" + Guid.NewGuid(), "Current1!", UserRoleCode.ProjectManager);
        var crew = await fixture.CreateUserAsync("nd01-crew-" + Guid.NewGuid(), "Current1!", UserRoleCode.RepairCrew);
        var supervisor = await fixture.CreateUserAsync("nd01-sup-" + Guid.NewGuid(), "Current1!", UserRoleCode.Supervisor);
        var reporter = await fixture.CreateUserAsync("nd01-reporter-" + Guid.NewGuid(), "Current1!", UserRoleCode.Reporter);
        var source = await new H5OfflinePositiveHttpTests(fixture).SeedAsync(pm.Id, crew.Id, supervisor.Id, reporter.Id);
        await using var db = fixture.CreateDbContext();
        var road = await db.RoadSectionVersions.Where(row => row.Id == source.Route).Select(row => row.RoadSectionId).SingleAsync();
        var state = await ND01OriginalWindowFixture.Prepare(db,
            new(pm.Id, crew.Id, supervisor.Id, source.Project, road, source.Route, source.Set, source.Defect));
        using var keys = state.Keys;
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var scope = factory.Services.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IOfflineRepairCommandAdapter>());
        using var client = factory.CreateClient(new() { BaseAddress = new("https://localhost") });
        await H5OfflinePositiveHttpTests.LoginAsync(client, crew.UserName!);
        var route = $"/api/v1/projects/{source.Project}/offline/sync";
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var response = await H5OfflinePositiveHttpTests.PostAsync(client, route, state.Batch);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items")[0];
            Assert.True(result.GetProperty("durableAcknowledgment").GetBoolean(), result.GetRawText());
            Assert.Equal(state.Operation.EffectId, result.GetProperty("effectId").GetGuid());
            Assert.Equal("UNCERTAIN", result.GetProperty("timeProvenance").GetString());
        }
        Assert.Equal(1, await db.Set<RepairExecutionStart>().CountAsync(row => row.ItemId == state.Item));
        Assert.Equal(1, await db.Set<OfflineOperationResult>().CountAsync(row => row.ProjectId == source.Project && row.DurableAck));
        var authorization = await db.Set<RepairExecutionAuthorization>().AsNoTracking().SingleAsync(row => row.Id == state.Authorization);
        Assert.Equal(state.First.Id, authorization.FirstStartOriginId); Assert.Equal(state.Expiry, authorization.ExpiresAt);
        Assert.Null((await db.Set<RepairExecutionStart>().AsNoTracking().SingleAsync(row => row.ItemId == state.Item)).VerifiedOriginalAt);
    }
}
