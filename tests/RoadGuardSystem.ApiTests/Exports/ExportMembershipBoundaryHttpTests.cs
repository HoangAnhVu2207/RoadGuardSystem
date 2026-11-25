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
using RoadGuardSystem.BusinessObjects.Exports;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Services.Exports;
using Xunit;

namespace RoadGuardSystem.ApiTests.Exports;

public sealed class ExportMembershipBoundaryHttpTests : IAsyncLifetime
{
    private readonly AuthenticationSqlServerFixture sql = new();
    public Task InitializeAsync() => sql.InitializeAsync();
    public Task DisposeAsync() => sql.DisposeAsync();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CurrentMembershipAndRoleProtectEveryExportBoundaryWhileCollectionStaysCreatorOnly(bool web)
    {
        await using var minio = await OwnedWebMinio.StartAsync(createBucket: true);
        var supervisor = await sql.CreateUserAsync("export-boundary-sup-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        var manager = await sql.CreateUserAsync("export-boundary-pm-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var project = Guid.NewGuid(); var otherProject = Guid.NewGuid();
        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project,
            UserId = supervisor.Id,
            RoleCode = UserRoleCode.Supervisor,
            Status = ProjectMemberStatus.Active,
            ValidFrom = new DateOnly(2000, 1, 1)
        };
        await using (var db = sql.CreateDbContext())
        {
            db.Projects.Add(Project.Create(project, project.ToString(), "TEST_ONLY export authority", null, null, null, null, DateTimeOffset.UtcNow));
            db.Projects.Add(Project.Create(otherProject, otherProject.ToString(), "TEST_ONLY other project", null, null, null, null, DateTimeOffset.UtcNow));
            db.ProjectMembers.Add(ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project, manager.Id, new DateOnly(2000, 1, 1)));
            await db.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IHostedService>();
            minio.Configure(services);
            services.RemoveAll<IAnh02ArtifactStore>(); services.AddSingleton<IAnh02ArtifactStore>(provider => new MinioAnh02ArtifactStore(provider.GetRequiredService<IOptions<MinioStorageOptions>>()));
            services.Configure<ExportOptions>(o => o.UnicodeFontPath = Environment.GetEnvironmentVariable("RG_TEST_UNICODE_FONT") ?? throw new InvalidOperationException("Test Unicode font required."));
        });
        async Task<HttpClient> Login(string email)
        {
            if (web) return await WebCookieTestClient.Login(factory, email);
            var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString()); return client;
        }
        using var sup = await Login(supervisor.Email!); using var pm = await Login(manager.Email!);
        var path = $"/api/v1/projects/{project}/exports";
        async Task<HttpResponseMessage> Create(HttpClient client, string key, string format = "PDF")
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(new { kind = "DOSSIER", format }) };
            request.Headers.Add("Idempotency-Key", key); return await client.SendAsync(request);
        }
        async Task<JsonElement> Body(HttpResponseMessage response, HttpStatusCode expected)
        {
            Assert.Equal(expected, response.StatusCode); return await response.Content.ReadFromJsonAsync<JsonElement>();
        }
        async Task DeniedReads(HttpClient client, Guid id)
        {
            foreach (var suffix in new[] { "", $"/{id}", $"/{id}/manifest", $"/{id}/content" })
            {
                using var response = await client.GetAsync(path + suffix);
                var problem = await Body(response, HttpStatusCode.Forbidden);
                Assert.Equal(suffix.Length == 0 ? "access_forbidden" : "forbidden", problem.GetProperty("code").GetString());
                Assert.True(response.Headers.TryGetValues("X-Correlation-ID", out var correlations));
                Assert.True(Guid.TryParse(Assert.Single(correlations), out _));
            }
        }
        await DeniedReads(sup, Guid.NewGuid());
        using (var denied = await Create(sup, Guid.NewGuid().ToString("N"))) await Body(denied, HttpStatusCode.Forbidden);
        await using (var db = sql.CreateDbContext()) { Assert.Empty(await db.Set<ExportJob>().ToListAsync()); db.ProjectMembers.Add(member); await db.SaveChangesAsync(); }
        var pmJob = (await Body(await Create(pm, Guid.NewGuid().ToString("N")), HttpStatusCode.Accepted)).GetProperty("id").GetGuid();
        var ownIds = new List<Guid>(); var key = Guid.NewGuid().ToString("N");
        for (var i = 0; i < 3; i++) ownIds.Add((await Body(await Create(sup, i == 0 ? key : Guid.NewGuid().ToString("N")), HttpStatusCode.Accepted)).GetProperty("id").GetGuid());
        Assert.Equal(ownIds[0], (await Body(await Create(sup, key), HttpStatusCode.Accepted)).GetProperty("id").GetGuid());
        await Body(await Create(sup, key, "ZIP"), HttpStatusCode.Conflict);
        if (web)
        {
            sup.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
            await Body(await Create(sup, Guid.NewGuid().ToString("N")), HttpStatusCode.Forbidden);
            await WebCookieTestClient.RefreshCsrf(sup);
        }
        var seen = new List<Guid>(); string? cursor = null;
        do
        {
            var page = await Body(await sup.GetAsync(path + "?limit=1" + (cursor is null ? "" : "&cursor=" + Uri.EscapeDataString(cursor))), HttpStatusCode.OK);
            seen.AddRange(page.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetGuid()));
            cursor = page.GetProperty("nextCursor").GetString();
            if (cursor is not null) await Body(await pm.GetAsync(path + "?limit=1&cursor=" + Uri.EscapeDataString(cursor)), HttpStatusCode.BadRequest);
        } while (cursor is not null);
        Assert.Equal(ownIds.Order(), seen.Order()); Assert.DoesNotContain(pmJob, seen);
        // Detail is project/source authorized, separately from the creator-only list.
        await Body(await sup.GetAsync(path + $"/{pmJob}"), HttpStatusCode.OK);
        await Body(await sup.GetAsync($"/api/v1/projects/{otherProject}/exports/{pmJob}"), HttpStatusCode.Forbidden);
        await Body(await sup.GetAsync(path + $"/{Guid.NewGuid()}"), HttpStatusCode.NotFound);
        await Body(await sup.GetAsync(path + $"/{ownIds[0]}/content"), HttpStatusCode.Conflict);
        for (var i = 0; i < 4; i++) { using var scope = factory.Services.CreateScope(); Assert.True(await scope.ServiceProvider.GetRequiredService<IExportService>().ProcessNextAsync(default)); }
        await Body(await sup.GetAsync(path + $"/{ownIds[0]}/manifest"), HttpStatusCode.OK);
        using (var content = await sup.GetAsync(path + $"/{ownIds[0]}/content"))
        {
            Assert.Equal(HttpStatusCode.OK, content.StatusCode); var bytes = await content.Content.ReadAsByteArrayAsync(); Assert.True(bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8));
        }
        async Task SetMember(UserRoleCode role, ProjectMemberStatus status, DateOnly? to = null)
        {
            await using var db = sql.CreateDbContext();
            await db.ProjectMembers.Where(m => m.Id == member.Id).ExecuteUpdateAsync(s => s.SetProperty(m => m.RoleCode, role).SetProperty(m => m.Status, status).SetProperty(m => m.ValidTo, to));
        }
        await SetMember(UserRoleCode.ProjectManager, ProjectMemberStatus.Active);
        await DeniedReads(sup, ownIds[0]);
        await Body(await Create(sup, key), HttpStatusCode.Forbidden);
        await SetMember(UserRoleCode.Supervisor, ProjectMemberStatus.Active, new DateOnly(2001, 1, 1)); await DeniedReads(sup, ownIds[0]);
        await using (var db = sql.CreateDbContext()) await db.ProjectMembers.Where(m => m.Id == member.Id).ExecuteUpdateAsync(s => s.SetProperty(m => m.ValidFrom, new DateOnly(2099, 1, 1)).SetProperty(m => m.ValidTo, (DateOnly?)null));
        await DeniedReads(sup, ownIds[0]);
        await using (var db = sql.CreateDbContext()) await db.ProjectMembers.Where(m => m.Id == member.Id).ExecuteUpdateAsync(s => s.SetProperty(m => m.ValidFrom, new DateOnly(2000, 1, 1)));
        await SetMember(UserRoleCode.Supervisor, ProjectMemberStatus.Ended); await DeniedReads(sup, ownIds[0]);
        await Body(await Create(sup, key), HttpStatusCode.Forbidden);
        await SetMember(UserRoleCode.Supervisor, ProjectMemberStatus.Active);
        var queuedId = (await Body(await Create(sup, Guid.NewGuid().ToString("N")), HttpStatusCode.Accepted)).GetProperty("id").GetGuid();
        await SetMember(UserRoleCode.Supervisor, ProjectMemberStatus.Ended);
        using (var scope = factory.Services.CreateScope()) Assert.True(await scope.ServiceProvider.GetRequiredService<IExportService>().ProcessNextAsync(default));
        await using (var db = sql.CreateDbContext())
        {
            var failed = await db.Set<ExportJob>().SingleAsync(j => j.Id == queuedId); Assert.Equal("FAILED", failed.Status); Assert.Equal("export_authority_revoked", failed.ErrorCode);
            Assert.Equal(5, await db.Set<ExportJob>().CountAsync());
        }
        await SetMember(UserRoleCode.Supervisor, ProjectMemberStatus.Active);
        await using (var db = sql.CreateDbContext()) await db.Roles.Where(r => r.Code == UserRoleCode.Supervisor).ExecuteUpdateAsync(s => s.SetProperty(r => r.IsActive, false));
        foreach (var suffix in new[] { "", $"/{ownIds[0]}", $"/{ownIds[0]}/manifest", $"/{ownIds[0]}/content" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await sup.GetAsync(path + suffix)).StatusCode);
        await using (var db = sql.CreateDbContext()) await db.Roles.Where(r => r.Code == UserRoleCode.Supervisor).ExecuteUpdateAsync(s => s.SetProperty(r => r.IsActive, true));
        using var fresh = await Login(supervisor.Email!);
        await using (var db = sql.CreateDbContext()) await db.Users.Where(u => u.Id == supervisor.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.Status, UserStatus.Suspended));
        foreach (var suffix in new[] { "", $"/{ownIds[0]}", $"/{ownIds[0]}/manifest", $"/{ownIds[0]}/content" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await fresh.GetAsync(path + suffix)).StatusCode);
    }
}
