using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.Services.Factories;
using RoadGuardSystem.Services.Options;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

public sealed class StaffReadCredentialBoundaryHttpTests(AuthenticationSqlServerFixture sql)
    : IClassFixture<AuthenticationSqlServerFixture>
{
    [Fact]
    public async Task AllFourStaffReadsEnforceBothCredentialLifetimesAndInvalidBearerPrecedence()
    {
        var supervisor = await sql.CreateUserAsync("STAFF-AUTH-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        var project = Project.Create(Guid.NewGuid(), "STAFF-AUTH-P-" + Guid.NewGuid().ToString("N"), "Owned staff credential fixture", null, null, null, null, DateTimeOffset.UtcNow);
        await using (var db = sql.CreateDbContext()) { db.Add(project); await db.SaveChangesAsync(); }
        var clock = new MutableClock(DateTimeOffset.UtcNow);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString,
            configureTestServices: services => { services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(clock); });
        using var cookie = await WebCookieTestClient.Login(factory, supervisor.Email!);
        using var bearer = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        using var anonymous = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        using var malformedCookie = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        malformedCookie.DefaultRequestHeaders.Add("Cookie", "__Host-RoadGuardSession=malformed");
        var login = await bearer.PostAsJsonAsync("/api/v1/auth/login", new { email = supervisor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;
        await using var state = sql.CreateDbContext();
        var session = await state.Sessions.AsNoTracking().Where(x => x.UserId == supervisor.Id).OrderByDescending(x => x.IssuedAt).FirstAsync();
        var options = factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
        var expired = new AccessTokenFactory(options).Create(supervisor.Id, session.Id, supervisor.RoleCode, DateTimeOffset.UtcNow.AddMinutes(-20), 15);
        var paths = new[] { "/api/v1/users", $"/api/v1/users/{supervisor.Id}", $"/api/v1/projects/{project.Id}/members", $"/api/v1/projects/{project.Id}/primary-project-manager" };
        var before = await state.Users.AsNoTracking().Where(x => x.Id == supervisor.Id).Select(x => x.RowVersion).SingleAsync();
        var auditCount = await state.AuditLogs.CountAsync(x => x.ActorUserId == supervisor.Id);
        foreach (var path in paths)
        {
            bearer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var admitted = await bearer.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, admitted.StatusCode);
            using var webAdmitted = await cookie.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, webAdmitted.StatusCode);
            foreach (var client in new[] { anonymous, malformedCookie }) await Denied(client, path);
            foreach (var credential in new[] { "malformed", expired })
            {
                bearer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credential);
                await Denied(bearer, path);
            }
            cookie.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "malformed");
            await Denied(cookie, path);
            cookie.DefaultRequestHeaders.Authorization = null;
        }
        clock.Now += TimeSpan.FromHours(13);
        foreach (var path in paths) await Denied(cookie, path);
        Assert.Equal(before, await state.Users.AsNoTracking().Where(x => x.Id == supervisor.Id).Select(x => x.RowVersion).SingleAsync());
        Assert.Equal(auditCount, await state.AuditLogs.CountAsync(x => x.ActorUserId == supervisor.Id));
        Assert.Empty(await state.ProjectMembers.Where(x => x.UserId == supervisor.Id).ToArrayAsync());
    }

    private static async Task Denied(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed class MutableClock(DateTimeOffset initial) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = initial;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
