using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Retention;
using RoadGuardSystem.Services.Retention;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;
namespace RoadGuardSystem.ApiTests.Retention;

[Collection(AuthenticationApiFixture.Name)]
public sealed class Anh02RetentionHttpTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task Hold_HTTP_headers_replay_multi_hold_and_private_scope_are_durable()
    {
        var seed = await SeedAsync(); await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") }); await LoginAsync(client, seed.Supervisor);
        var payload = new { scopeType = "FILE", scopeId = seed.File, reason = "Review requested" }; var key = Guid.NewGuid().ToString();
        Assert.Equal((HttpStatusCode)428, (await client.PostAsJsonAsync("/api/v1/retention/holds", payload)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Command(client, "/api/v1/retention/holds", new { scopeType = "FILE", scopeId = seed.File, reason = "Review", execute = true }, Guid.NewGuid().ToString())).StatusCode);
        var created = await Command(client, "/api/v1/retention/holds", payload, key); Assert.Equal(HttpStatusCode.Created, created.StatusCode); Assert.NotNull(created.Headers.ETag); Assert.NotNull(created.Headers.Location);
        var body = await created.Content.ReadAsStringAsync(); var id = JsonDocument.Parse(body).RootElement.GetProperty("id").GetGuid();
        var replay = await Command(client, "/api/v1/retention/holds", payload, key); Assert.Equal(body, await replay.Content.ReadAsStringAsync()); Assert.Equal(created.Headers.ETag, replay.Headers.ETag);
        Assert.Equal(HttpStatusCode.Conflict, (await Command(client, "/api/v1/retention/holds", new { scopeType = "FILE", scopeId = seed.File, reason = "Changed" }, key)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Command(client, "/api/v1/retention/holds", new { scopeType = "PROJECT", scopeId = seed.Project, reason = "Review requested" }, key)).StatusCode);
        var second = await Command(client, "/api/v1/retention/holds", payload, Guid.NewGuid().ToString()); Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var release = await Command(client, $"/api/v1/retention/holds/{id}/release", new { reason = "Completed review" }, Guid.NewGuid().ToString(), created.Headers.ETag!.Tag); Assert.Equal(HttpStatusCode.OK, release.StatusCode);
        Assert.Equal(HttpStatusCode.PreconditionFailed, (await Command(client, $"/api/v1/retention/holds/{id}/release", new { reason = "Stale" }, Guid.NewGuid().ToString(), created.Headers.ETag.Tag)).StatusCode);
        var privateHold = await Command(client, "/api/v1/retention/holds", new { scopeType = "FILE", scopeId = seed.PrivateFile, reason = "Private review" }, Guid.NewGuid().ToString()); var privateId = (await privateHold.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await LoginAsync(client, seed.Pm);
        Assert.Equal(HttpStatusCode.Forbidden, (await Command(client, "/api/v1/retention/holds", payload, Guid.NewGuid().ToString())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/retention/holds/{privateId}")).StatusCode);
        var file = await client.GetAsync($"/api/v1/projects/{seed.Project}/retention/files/{seed.File}"); Assert.Equal(HttpStatusCode.OK, file.StatusCode);
        var view = await file.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal(1, view.GetProperty("activeHoldCount").GetInt32()); Assert.Equal("BLOCKED_HOLD", view.GetProperty("evaluation").GetProperty("eligibility").GetString());
        await using var db = sql.CreateDbContext(); Assert.Equal(3, await db.Set<RetentionHold>().CountAsync(x => x.ScopeId == seed.File || x.ScopeId == seed.PrivateFile));
    }
    [Fact]
    public async Task Missing_inventory_basis_fails_closed_evaluation_GET_has_no_source_writes()
    {
        var seed = await SeedAsync(); await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") }); await LoginAsync(client, seed.Supervisor);
        var path = $"/api/v1/projects/{seed.Project}/retention/files/{seed.File}"; var initial = await client.GetAsync(path); Assert.Equal(HttpStatusCode.OK, initial.StatusCode); Assert.Equal("\"none\"", initial.Headers.ETag!.Tag);
        var view = await initial.Content.ReadFromJsonAsync<JsonElement>(); Assert.False(view.GetProperty("inventoryComplete").GetBoolean());
        using var put = new HttpRequestMessage(HttpMethod.Put, path + "/basis") { Content = JsonContent.Create(new { warrantyIds = Array.Empty<Guid>(), expectedReferenceInventoryVersion = view.GetProperty("inventoryVersion").GetString(), reason = "Known references" }) };
        put.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString()); put.Headers.Add("If-Match", "\"none\"");
        var rejected = await client.SendAsync(put); Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode); Assert.Equal("retention_inventory_incomplete", (await rejected.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await LoginAsync(client, seed.Pm);
        var admitted = await Command(client, $"/api/v1/projects/{seed.Project}/retention/evaluations", new { fileIds = new[] { seed.File } }, Guid.NewGuid().ToString()); Assert.Equal(HttpStatusCode.Accepted, admitted.StatusCode); Assert.NotNull(admitted.Headers.Location);
        using (var scope = factory.Services.CreateScope()) { while (await scope.ServiceProvider.GetRequiredService<IRetentionService>().ProcessOneAsync(default)) { } }
        var result = await client.GetAsync(admitted.Headers.Location); Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var body = await result.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal("COMPLETE", body.GetProperty("status").GetString()); Assert.Equal("WAITING_RETENTION_BASIS", body.GetProperty("items")[0].GetProperty("eligibility").GetString());
        await using var check = sql.CreateDbContext(); var before = await check.Files.AsNoTracking().SingleAsync(x => x.Id == seed.File); var audits = await check.AuditLogs.CountAsync(); var jobs = await check.Set<RetentionEvaluation>().CountAsync();
        await client.GetAsync(admitted.Headers.Location); await client.GetAsync(path);
        Assert.Equal(audits, await check.AuditLogs.CountAsync()); Assert.Equal(jobs, await check.Set<RetentionEvaluation>().CountAsync()); var after = await check.Files.AsNoTracking().SingleAsync(x => x.Id == seed.File); Assert.Equal(before.Checksum, after.Checksum); Assert.Equal(before.RetentionUntil, after.RetentionUntil);
    }
    private async Task<(string Supervisor, string Pm, Guid Project, Guid File, Guid PrivateFile)> SeedAsync()
    {
        var supervisor = await sql.CreateUserAsync($"anh02_retention_sup_{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var pm = await sql.CreateUserAsync($"anh02_retention_pm_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var project = Guid.NewGuid(); var file = Guid.NewGuid(); var privateFile = Guid.NewGuid(); await using var db = sql.CreateDbContext();
        db.Projects.Add(Project.Create(project, project.ToString(), "Retention HTTP fixture", null, null, null, null, DateTimeOffset.UtcNow));
        var membership = ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project, pm.Id, new DateOnly(2000, 1, 1)); membership.IsPrimary = false; db.ProjectMembers.Add(membership);
        db.Files.AddRange(StoredFile.Create(file, $"fixture/{file}", "public.jpg", "image/jpeg", 10, new string('a', 64), supervisor.Id, DateTimeOffset.UtcNow, null), StoredFile.Create(privateFile, $"fixture/{privateFile}", "private.jpg", "image/jpeg", 10, new string('b', 64), supervisor.Id, DateTimeOffset.UtcNow, null));
        db.FileScopes.AddRange(FileScope.Create(Guid.NewGuid(), file, project, null, supervisor.Id, "SURVEY_MEDIA", DateTimeOffset.UtcNow), FileScope.CreatePrivate(Guid.NewGuid(), privateFile, supervisor.Id, DateTimeOffset.UtcNow)); await db.SaveChangesAsync();
        return (supervisor.UserName!, pm.UserName!, project, file, privateFile);
    }
    private static async Task LoginAsync(HttpClient client, string user)
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(user), password = "Current1!" }); Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
    }
    private static Task<HttpResponseMessage> Command(HttpClient client, string path, object body, string key, string? version = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) }; request.Headers.Add("Idempotency-Key", key); if (version is not null) request.Headers.Add("If-Match", version); return client.SendAsync(request);
    }
}
