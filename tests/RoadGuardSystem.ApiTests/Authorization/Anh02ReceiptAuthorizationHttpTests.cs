using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Exports;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Retention;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.ApiTests.Authorization;

public sealed class Anh02ReceiptAuthorizationHttpTests : IAsyncLifetime
{
    private readonly AuthenticationSqlServerFixture sql = new();
    public Task InitializeAsync() => sql.InitializeAsync();
    public Task DisposeAsync() => sql.DisposeAsync();

    [Theory]
    [InlineData("export", false)] [InlineData("export", true)]
    [InlineData("hold", false)] [InlineData("hold", true)]
    [InlineData("evaluate", false)] [InlineData("evaluate", true)]
    public async Task Http_denies_replay_and_conflict_after_preflight_without_effects(string command, bool conflict)
    {
        var supervisor = command == "hold";
        var user = await sql.CreateUserAsync("anh02-receipt-" + Guid.NewGuid().ToString("N"), "Current1!", supervisor ? UserRoleCode.Supervisor : UserRoleCode.ProjectManager);
        var project = Guid.NewGuid(); var file = Guid.NewGuid();
        await using (var db = sql.CreateDbContext())
        {
            db.Projects.Add(Project.Create(project, project.ToString(), "ANH02 receipt HTTP", null, null, null, null, DateTimeOffset.UtcNow));
            if (!supervisor) db.ProjectMembers.Add(ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project, user.Id, new DateOnly(2000, 1, 1)));
            db.Files.Add(StoredFile.Create(file, $"fixture/{file}", "evidence.jpg", "image/jpeg", 10, new string('a', 64), user.Id, DateTimeOffset.UtcNow, null));
            db.FileScopes.Add(FileScope.Create(Guid.NewGuid(), file, project, null, user.Id, "SURVEY_MEDIA", DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }
        var barrier = new Anh02ReceiptRevocationInterceptor(async token =>
        {
            await using var db = sql.CreateDbContext();
            if (command == "export") await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProjectMembers SET Status=2 WHERE UserId={user.Id}", token);
            else await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Roles SET IsActive=0 WHERE Code={user.RoleCode.ToDbCode()}", token);
        });
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IHostedService>();
            services.RemoveAll<RoadGuardDbContext>();
            services.AddScoped(sp => new RoadGuardDbContext(new DbContextOptionsBuilder<RoadGuardDbContext>(sp.GetRequiredService<DbContextOptions<RoadGuardDbContext>>()).AddInterceptors(barrier).Options));
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = user.Email, password = "Current1!" });
        login.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        var key = Guid.NewGuid().ToString();
        var path = command switch { "export" => $"/api/v1/projects/{project}/exports", "hold" => "/api/v1/retention/holds", _ => $"/api/v1/projects/{project}/retention/evaluations" };
        object Payload(bool changed) => command switch
        {
            "export" => new { kind = "DOSSIER", format = changed ? "ZIP" : "PDF", includeOriginalFiles = false },
            "hold" => new { scopeType = "PROJECT", scopeId = project, reason = changed ? "Changed" : "Original" },
            _ => new { fileIds = changed ? new[] { file } : null }
        };
        async Task<HttpResponseMessage> Send(bool changed)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(Payload(changed)) };
            request.Headers.Add("Idempotency-Key", key); return await client.SendAsync(request);
        }
        var first = await Send(false); Assert.Equal(command == "hold" ? HttpStatusCode.Created : HttpStatusCode.Accepted, first.StatusCode);
        await using var verify = sql.CreateDbContext();
        var audits = await verify.AuditLogs.CountAsync(x => x.ActorUserId == user.Id);
        var receipts = await verify.Set<IdempotencyRecord>().CountAsync(x => x.ActorUserId == user.Id);
        barrier.Armed = true;
        var denied = await Send(conflict);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(command == "export" ? "forbidden" : "access_forbidden", (await denied.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.Null(denied.Headers.ETag); Assert.Null(denied.Headers.Location); Assert.Equal(1, barrier.Calls);
        Assert.Equal(receipts, await verify.Set<IdempotencyRecord>().CountAsync(x => x.ActorUserId == user.Id));
        Assert.Equal(audits, await verify.AuditLogs.CountAsync(x => x.ActorUserId == user.Id));
        Assert.Equal(command == "export" ? 1 : 0, await verify.Set<ExportJob>().CountAsync(x => x.ProjectId == project));
        Assert.Equal(command == "hold" ? 1 : 0, await verify.Set<RetentionHold>().CountAsync(x => x.CreatedBy == user.Id));
        Assert.Equal(command == "evaluate" ? 1 : 0, await verify.Set<RetentionEvaluation>().CountAsync(x => x.ProjectId == project));
    }
}
