using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Projects;
using Xunit;

namespace RoadGuardSystem.ApiTests.Authentication;

[Collection(AuthenticationApiFixture.Name)]
public sealed class HuyFinalOwnedRouteCookieHttpTests(AuthenticationSqlServerFixture fixture)
{
    [Theory]
    [InlineData("route-systems")]
    [InlineData("field-inspection-tasks")]
    public async Task OwnedReadUsesCookieWithBearerPrecedenceAndCurrentSession(string resource)
        => await CheckOwnedRead(resource, HttpStatusCode.OK);

    [Fact]
    public async Task CurrentReportingUsesCookieWithBearerPrecedenceAndCurrentSession()
        => await CheckOwnedRead("reports/summary", HttpStatusCode.OK);

    [Fact]
    public async Task ExportReadUsesCookieBeforeScopedNotFound()
        => await CheckOwnedRead("exports/00000000-0000-0000-0000-000000000001", HttpStatusCode.NotFound);

    private async Task CheckOwnedRead(string resource, HttpStatusCode authorizedStatus)
    {
        var actor=await fixture.CreateUserAsync($"owned-cookie-{Guid.NewGuid():N}","Current1!",UserRoleCode.ProjectManager);
        var project=await SeedProject(actor.Id);
        await using var factory=new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost"),HandleCookies=true});
        await Login(client,actor.Email!);
        var path=$"/api/v1/projects/{project}/{resource}/";
        Assert.Equal(authorizedStatus,(await client.GetAsync(path)).StatusCode);
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer","invalid");
        Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync(path)).StatusCode);
        client.DefaultRequestHeaders.Authorization=null;
        var other=await fixture.CreateUserAsync($"owned-other-{Guid.NewGuid():N}","Current1!",UserRoleCode.ProjectManager);
        using(var bearer=factory.CreateClient())
        {
            var login=await bearer.PostAsJsonAsync("/api/v1/auth/login",new{email=other.Email,password="Current1!"});
            Assert.Equal(HttpStatusCode.OK,login.StatusCode);
            var access=(await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
            client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",access);
            Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync(path)).StatusCode);
            client.DefaultRequestHeaders.Authorization=null;
        }
        await using(var db=fixture.CreateDbContext())
            await db.Sessions.Where(row=>row.UserId==actor.Id).ExecuteUpdateAsync(update=>update.SetProperty(row=>row.RevokedAt,DateTimeOffset.UtcNow));
        Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync(path)).StatusCode);
    }

    [Theory]
    [InlineData("route-systems")]
    [InlineData("field-inspection-tasks")]
    [InlineData("pavement-layouts/as-built")]
    [InlineData("exports")]
    public async Task OwnedWriteRequiresCsrfBeforeControllerAdmission(string resource)
    {
        var actor=await fixture.CreateUserAsync($"owned-write-{Guid.NewGuid():N}","Current1!",UserRoleCode.ProjectManager);
        var project=await SeedProject(actor.Id);
        await using var factory=new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost"),HandleCookies=true});
        await Login(client,actor.Email!);
        var response=await client.PostAsJsonAsync($"/api/v1/projects/{project}/{resource}",new{});
        Assert.Equal(HttpStatusCode.Forbidden,response.StatusCode);
        Assert.Equal("csrf_failed",(await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await using var db=fixture.CreateDbContext();
        Assert.False(await db.IdempotencyRecords.AnyAsync(row=>row.ActorUserId==actor.Id));
    }

    private async Task<Guid> SeedProject(Guid actor)
    {
        await using var db=fixture.CreateDbContext();var now=DateTimeOffset.UtcNow;
        var project=Project.Create(Guid.NewGuid(),Guid.NewGuid().ToString(),"Owned cookie transport fixture",null,null,null,null,now);
        db.AddRange(project,ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(),project.Id,actor,DateOnly.FromDateTime(now.UtcDateTime)));
        await db.SaveChangesAsync();return project.Id;
    }

    private static async Task Login(HttpClient client,string email)
    {
        var csrf=(await (await client.GetAsync("/api/v1/auth/web/csrf")).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString()!;
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN",csrf);
        Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/v1/auth/web/login",new{email,password="Current1!"})).StatusCode);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
    }
}
