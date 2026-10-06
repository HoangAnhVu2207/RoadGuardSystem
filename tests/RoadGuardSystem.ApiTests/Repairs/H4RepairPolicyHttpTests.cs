using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Repairs;

[Collection(AuthenticationApiFixture.Name)]
public sealed class H4RepairPolicyHttpTests(AuthenticationSqlServerFixture fixture)
{
    [Fact]
    public async Task CurrentPmHttpDraftPublishRevokeAndProtectedReplayUseProductionPolicy()
    {
        var actor = await fixture.CreateUserAsync("policy-http-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "policy HTTP", null, null, null, null, DateTimeOffset.UtcNow);
        var type = DefectType.Create("PH" + Guid.NewGuid().ToString("N"), "controlled policy");
        await using (var db = fixture.CreateDbContext())
        {
            db.AddRange(project, type, ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, actor.Id, new(2000, 1, 1)));
            await db.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", (await login.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("accessToken").GetString());
        var root = $"/api/v1/projects/{project.Id}/repair-policies";
        var definition = new RepairPolicyDefinitionInput(type.Code, "http-v1", [], [], "explicit NOT_CONFIGURED fixture");
        Assert.Equal(HttpStatusCode.PreconditionRequired, (await client.PostAsJsonAsync(root + "/drafts", definition)).StatusCode);
        client.DefaultRequestHeaders.Add("Idempotency-Key", "draft-" + project.Id);
        var response = await client.PostAsJsonAsync(root + "/drafts", definition);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var draft = (await response.Content.ReadFromJsonAsync<RepairPolicyView>())!;
        Assert.Equal('"' + draft.Version + '"', response.Headers.ETag!.ToString());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(root + "/drafts", definition)).StatusCode);
        client.DefaultRequestHeaders.Remove("Idempotency-Key"); client.DefaultRequestHeaders.Add("Idempotency-Key", "publish-" + project.Id);
        client.DefaultRequestHeaders.IfMatch.Add(new EntityTagHeaderValue('"' + draft.Version + '"'));
        response = await client.PostAsJsonAsync(root + $"/drafts/{draft.Id}/publish", new RepairPolicyReasonInput("publish exact draft"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var revision = (await response.Content.ReadFromJsonAsync<RepairPolicyView>())!;
        Assert.Empty(revision.Measurements); Assert.Equal("PUBLISHED", revision.State);
        client.DefaultRequestHeaders.IfMatch.Clear(); client.DefaultRequestHeaders.IfMatch.Add(new('"' + revision.Version + '"'));
        client.DefaultRequestHeaders.Remove("Idempotency-Key"); client.DefaultRequestHeaders.Add("Idempotency-Key", "revoke-" + project.Id);
        var revokePath = root + $"/revisions/{revision.Id}/revoke";
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(revokePath, new RepairPolicyReasonInput("withdraw"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(revokePath, new RepairPolicyReasonInput("withdraw"))).StatusCode);
        await using (var db = fixture.CreateDbContext())
        {
            var member = await db.ProjectMembers.SingleAsync(row => row.ProjectId == project.Id && row.UserId == actor.Id);
            member.Status = ProjectMemberStatus.Ended; await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(revokePath, new RepairPolicyReasonInput("withdraw"))).StatusCode);
    }
}
