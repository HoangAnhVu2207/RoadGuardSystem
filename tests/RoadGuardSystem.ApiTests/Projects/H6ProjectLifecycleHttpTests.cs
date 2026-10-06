using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.DTOs.Projects;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

[Collection(AuthenticationApiFixture.Name)]
public sealed class H6ProjectLifecycleHttpTests(AuthenticationSqlServerFixture fixture)
{
    [Fact]
    public async Task CookieRenewedScopeRequiresCsrfBeforeAnyHistoryOrReceipt()
    {
        var actor=await fixture.CreateUserAsync("lifecycle-cookie-"+Guid.NewGuid().ToString("N"),"Current1!",UserRoleCode.Supervisor);
        await using var factory=new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost"),HandleCookies=true});
        var csrf=(await (await client.GetAsync("/api/v1/auth/web/csrf")).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString();
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN",csrf);
        Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/v1/auth/web/login",new{email=actor.Email,password="Current1!"})).StatusCode);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");client.DefaultRequestHeaders.Add("Idempotency-Key",Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match",$"\"{new string('a',64)}\"");
        var response=await client.PostAsJsonAsync($"/api/v1/projects/{Guid.NewGuid()}/lifecycle/renewed-handling-scope",
            new RenewedHandlingScopeInput(Guid.NewGuid(),"renew","scope","basis"));
        Assert.Equal(HttpStatusCode.Forbidden,response.StatusCode);
        Assert.Equal("csrf_failed",(await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await using var db=fixture.CreateDbContext();Assert.False(await db.IdempotencyRecords.AnyAsync(row=>row.ActorUserId==actor.Id));
        Assert.False(await db.Set<ProjectLifecycleHistoryRecord>().AnyAsync(row=>row.ActorId==actor.Id));
    }
    [Fact]
    public async Task RenewedScopeRequiresPreconditionsBeforeAnyLifecycleEffect()
    {
        var actor=await fixture.CreateUserAsync("lifecycle-http-"+Guid.NewGuid().ToString("N"),"Current1!",UserRoleCode.Supervisor);
        await using var factory=new AuthenticationWebApplicationFactory(fixture.ConnectionString);using var client=factory.CreateClient();
        Assert.NotNull(actor.Email);await Login(client,actor.Email);
        var response=await client.PostAsJsonAsync($"/api/v1/projects/{Guid.NewGuid()}/lifecycle/renewed-handling-scope",
            new RenewedHandlingScopeInput(Guid.NewGuid(),"renewed scope","bounded handling","basis"));
        Assert.Equal(HttpStatusCode.PreconditionRequired,response.StatusCode);
        await using var db=fixture.CreateDbContext();Assert.False(await db.IdempotencyRecords.AnyAsync(row=>row.ActorUserId==actor.Id));
        Assert.False(await db.Set<ProjectLifecycleHistoryRecord>().AnyAsync(row=>row.ActorId==actor.Id));
    }

    [Fact]
    public async Task CurrentPrivateLifecycleReadAndCandidateSourceDenialUseActualHttp()
    {
        var actor=await fixture.CreateUserAsync("lifecycle-source-http-"+Guid.NewGuid().ToString("N"),"Current1!",UserRoleCode.Supervisor);
        await using var db=fixture.CreateDbContext();var now=DateTimeOffset.UtcNow;
        var project=Project.Create(Guid.NewGuid(),Guid.NewGuid().ToString(),"Candidate lifecycle HTTP source",null,null,null,null,now);
        var member=new ProjectMember{Id=Guid.NewGuid(),ProjectId=project.Id,UserId=actor.Id,RoleCode=UserRoleCode.Supervisor,
            Status=ProjectMemberStatus.Active,ValidFrom=DateOnly.FromDateTime(now.UtcDateTime).AddDays(-1)};
        var closure=ProjectLifecycleHistoryRecord.RecordCandidate(Guid.NewGuid(),project.Id,ProjectLifecycleFactKind.OperationalClosure,
            actor.Id,now,"candidate source","TEST_ONLY no generic close authority","{}");
        db.AddRange(project,member,closure);await db.SaveChangesAsync();
        await using var factory=new AuthenticationWebApplicationFactory(fixture.ConnectionString);using var client=factory.CreateClient();
        Assert.NotNull(actor.Email);await Login(client,actor.Email);var path=$"/api/v1/projects/{project.Id}/lifecycle";
        var read=await client.GetAsync(path);Assert.Equal(HttpStatusCode.OK,read.StatusCode);
        var view=await read.Content.ReadFromJsonAsync<ProjectLifecycleViewDto>();Assert.NotNull(view);
        Assert.Equal("UNKNOWN",view.OperationalClosure);Assert.Equal("UNKNOWN",view.ObligationInventory);Assert.True(view.AcceptsNewReports);
        Assert.NotNull(read.Headers.ETag);Assert.Equal($"\"{view.Version}\"",read.Headers.ETag.ToString());
        client.DefaultRequestHeaders.Add("Idempotency-Key",Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match",read.Headers.ETag.ToString());
        var denial=await client.PostAsJsonAsync(path+"/renewed-handling-scope",new RenewedHandlingScopeInput(closure.Id,"renewed","scope","candidate basis"));
        Assert.Equal(HttpStatusCode.Conflict,denial.StatusCode);
        Assert.Equal("operational_closure_source_unavailable",(await denial.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await db.ProjectMembers.Where(row=>row.Id==member.Id).ExecuteUpdateAsync(update=>update.SetProperty(row=>row.Status,ProjectMemberStatus.Ended));
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync(path)).StatusCode);
        Assert.Single(await db.Set<ProjectLifecycleHistoryRecord>().Where(row=>row.ProjectId==project.Id).ToArrayAsync());
        Assert.False(await db.IdempotencyRecords.AnyAsync(row=>row.ActorUserId==actor.Id));
    }

    private static async Task Login(HttpClient client,string email)
    {
        var response=await client.PostAsJsonAsync("/api/v1/auth/login",new{email,password="Current1!"});Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",
            (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
    }
}
