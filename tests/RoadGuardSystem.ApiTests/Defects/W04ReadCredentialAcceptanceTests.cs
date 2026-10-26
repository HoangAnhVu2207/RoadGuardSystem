using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.Services.Factories;
using RoadGuardSystem.Services.Options;
using Xunit;

namespace RoadGuardSystem.ApiTests.Defects;

[Collection(AuthenticationApiFixture.Name)]
public sealed class W04ReadCredentialAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData("API-060", "candidate-decisions/{resource}", 404)]
    [InlineData("API-061", "candidates?sourceKind=REPORT&sourceId={resource}", 404)]
    [InlineData("API-070", "defects", 200)]
    [InlineData("API-071", "defects/{resource}", 404)]
    [InlineData("API-105", "labels", 200)]
    [InlineData("API-107", "labels/{resource}", 404)]
    public async Task ExactReadChecksBothTransportsAndRejectsInvalidExpiredAndRevokedCredentials(
        string operation, string suffix, int admittedStatus)
    {
        Assert.StartsWith("API-", operation);
        var actor = await sql.CreateUserAsync("w04-read-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var project = Project.Create(Guid.NewGuid(), "W04-" + Guid.NewGuid().ToString("N"), "Owned W04 read boundary", null, null, null, null, DateTimeOffset.UtcNow);
        await using (var setup = sql.CreateDbContext())
        {
            setup.Add(project);
            setup.Add(new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                UserId = actor.Id,
                RoleCode = UserRoleCode.ProjectManager,
                Status = ProjectMemberStatus.Active,
                ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
            });
            await setup.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString,
            configureTestServices: services => services.RemoveAll<IHostedService>());
        using var bearer = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        using var anonymous = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        using var web = await WebCookieTestClient.Login(factory, actor.Email!);
        using var login = await bearer.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;
        var sid = Guid.Parse(new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.Single(x => x.Type == "sid").Value);
        var expired = new AccessTokenFactory(factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value)
            .Create(actor.Id, sid, actor.RoleCode, DateTimeOffset.UtcNow.AddMinutes(-20), 15);
        var path = $"/api/v1/projects/{project.Id}/" + suffix.Replace("{resource}", Guid.NewGuid().ToString());
        await using var db = sql.CreateDbContext();
        var auditIds = await db.AuditLogs.Where(x => x.ActorUserId == actor.Id).OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync();
        bearer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using (var admitted = await bearer.GetAsync(path)) Assert.Equal(admittedStatus, (int)admitted.StatusCode);
        using (var admitted = await web.GetAsync(path)) Assert.Equal(admittedStatus, (int)admitted.StatusCode);
        await Denied(anonymous, "auth_unauthorized");
        foreach (var bad in new[] { "malformed", expired })
        {
            bearer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bad);
            await Denied(bearer, "auth_unauthorized");
            web.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bad);
            await Denied(web, "auth_unauthorized");
        }
        web.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using (var admitted = await web.GetAsync(path)) Assert.Equal(admittedStatus, (int)admitted.StatusCode);
        await db.Sessions.Where(x => x.Id == sid).ExecuteUpdateAsync(x => x.SetProperty(v => v.RevokedAt, DateTimeOffset.UtcNow));
        bearer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await Denied(bearer, "auth_session_revoked");
        await Denied(web, "auth_session_revoked");
        web.DefaultRequestHeaders.Authorization = null;
        using (var stillLive = await web.GetAsync(path)) Assert.Equal(admittedStatus, (int)stillLive.StatusCode);
        await db.Sessions.Where(x => x.UserId == actor.Id && x.Transport == RoadGuardSystem.BusinessObjects.Identity.SessionTransport.Web)
            .ExecuteUpdateAsync(x => x.SetProperty(v => v.RevokedAt, DateTimeOffset.UtcNow));
        await Denied(web, "auth_unauthorized");
        Assert.Equal(auditIds, await db.AuditLogs.Where(x => x.ActorUserId == actor.Id).OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync());
        Assert.Empty(await db.IdempotencyRecords.Where(x => x.ActorUserId == actor.Id).ToArrayAsync());
        Assert.Empty(await db.Defects.Where(x => x.ProjectId == project.Id).ToArrayAsync());
        Assert.Empty(await db.SourceDecisions.Where(x => x.ProjectId == project.Id).ToArrayAsync());

        async Task Denied(HttpClient client, string code)
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(401, body.GetProperty("status").GetInt32());
            Assert.Equal(code, body.GetProperty("code").GetString());
            var correlation = Assert.Single(response.Headers.GetValues("X-Correlation-ID"));
            Assert.True(Guid.TryParse(correlation, out _));
            Assert.Equal(correlation, body.GetProperty("correlationId").GetString());
            Assert.Null(response.Headers.ETag);
            Assert.Null(response.Headers.Location);
        }
    }
}
