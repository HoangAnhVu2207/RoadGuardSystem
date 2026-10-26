using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Identity;

[Collection(AuthenticationApiFixture.Name)]
public sealed class AdminResetWireAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task ExactAdminResetWireReturnsCredentialOnceAndPersistsSanitizedAudit()
    {
        var actor = await sql.CreateUserAsync("reset-wire-super-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        var target = await sql.CreateUserAsync("reset-wire-target-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.DroneOperator);
        var unaffected = await sql.CreateUserAsync("reset-wire-other-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var victim = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await Login(client, actor.Email!); await Login(victim, target.Email!);
        string version, initialHash, otherHash;
        await using (var db = sql.CreateDbContext())
        {
            var original = await db.Users.AsNoTracking().SingleAsync(u => u.Id == target.Id);
            version = Convert.ToBase64String(original.RowVersion); initialHash = original.PasswordHash!;
            otherHash = (await db.Users.AsNoTracking().SingleAsync(u => u.Id == unaffected.Id)).PasswordHash!;
        }
        var path = $"/api/v1/admin/users/{target.Id}/password-reset";
        var operation = Guid.NewGuid(); var start = DateTimeOffset.UtcNow;
        var request = new { expectedTargetRowVersion = version, operationId = operation };
        using var response = await client.PostAsJsonAsync(path, request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Null(response.Headers.ETag); Assert.Null(response.Headers.Location);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { "mustChangePassword", "temporaryPassword", "userId" }, body.EnumerateObject().Select(p => p.Name).Order());
        Assert.Equal(target.Id, body.GetProperty("userId").GetGuid()); Assert.True(body.GetProperty("mustChangePassword").GetBoolean());
        var temporary = body.GetProperty("temporaryPassword").GetString(); Assert.False(string.IsNullOrWhiteSpace(temporary));
        var correlation = Assert.Single(response.Headers.GetValues("X-Correlation-ID")); Assert.True(Guid.TryParse(correlation, out _));
        string persistedState;
        async Task<string> Snapshot()
        {
            await using var db = sql.CreateDbContext();
            return JsonSerializer.Serialize(new
            {
                user = await db.Users.AsNoTracking().Where(u => u.Id == target.Id).Select(u => new { u.PasswordHash, u.SecurityStamp, u.MustChangePassword, u.RowVersion }).SingleAsync(),
                sessions = await db.Sessions.AsNoTracking().Where(s => s.UserId == target.Id).OrderBy(s => s.Id).Select(s => new { s.Id, s.RevokedAt, s.RowVersion }).ToArrayAsync(),
                tokens = await db.RefreshTokens.AsNoTracking().Where(t => t.Session.UserId == target.Id).OrderBy(t => t.Id).Select(t => new { t.Id, t.RevokedAt, t.RowVersion }).ToArrayAsync(),
                resets = await db.PasswordResetLogs.Where(r => r.TargetUserId == target.Id).OrderBy(r => r.Id).Select(r => r.Id).ToArrayAsync(),
                audits = await db.AuditLogs.Where(a => a.EntityId == target.Id).OrderBy(a => a.Id).Select(a => a.Id).ToArrayAsync(),
                receipts = await db.IdempotencyRecords.Where(r => r.ActorUserId == actor.Id && r.Operation == "AdminPasswordReset").OrderBy(r => r.Id).Select(r => r.Id).ToArrayAsync()
            });
        }
        await using (var db = sql.CreateDbContext())
        {
            var changed = await db.Users.AsNoTracking().SingleAsync(u => u.Id == target.Id);
            Assert.True(changed.PasswordHash != initialHash); Assert.True(changed.MustChangePassword);
            Assert.True(Convert.ToBase64String(changed.RowVersion) != version);
            Assert.Equal(otherHash, (await db.Users.AsNoTracking().SingleAsync(u => u.Id == unaffected.Id)).PasswordHash);
            Assert.Single(await db.Sessions.Where(s => s.UserId == target.Id && s.RevokedAt != null).ToListAsync());
            Assert.Single(await db.RefreshTokens.Where(t => t.Session.UserId == target.Id && t.RevokedAt != null).ToListAsync());
            var reset = await db.PasswordResetLogs.SingleAsync(r => r.TargetUserId == target.Id);
            Assert.Equal(actor.Id, reset.PerformedByUserId); Assert.Equal(Guid.Parse(correlation), reset.CorrelationId);
            Assert.InRange(reset.OccurredAt, start, DateTimeOffset.UtcNow);
            var audit = await db.AuditLogs.SingleAsync(a => a.EntityId == target.Id && a.EventType == "user_password_reset");
            Assert.Equal(actor.Id, audit.ActorUserId); Assert.Equal("User", audit.EntityType); Assert.Equal("IdentityRepository", audit.Source);
            Assert.Equal(Guid.Parse(correlation), audit.CorrelationId); Assert.Null(audit.Reason);
            Assert.Equal("{\"must_change_password\":false}", audit.BeforeSnapshot); Assert.Equal("{\"must_change_password\":true}", audit.AfterSnapshot);
            Assert.InRange(audit.OccurredAtUtc, start, DateTimeOffset.UtcNow);
            var receipt = await db.IdempotencyRecords.SingleAsync(r => r.ActorUserId == actor.Id && r.Operation == "AdminPasswordReset");
            Assert.Equal(operation.ToString("N"), receipt.IdempotencyKey); Assert.Null(receipt.ProjectId);
            Assert.Equal("{\"status\":\"success\"}", receipt.OutcomeJson);
            foreach (var raw in new[] { JsonSerializer.Serialize(reset), audit.BeforeSnapshot!, audit.AfterSnapshot!, receipt.OutcomeJson })
            { Assert.DoesNotContain(temporary!, raw); Assert.DoesNotContain(initialHash, raw); }
        }
        persistedState = await Snapshot();
        using var replay = await client.PostAsJsonAsync(path, request);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        var replayBody = await replay.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { "mustChangePassword", "temporaryPassword", "userId" }, replayBody.EnumerateObject().Select(p => p.Name).Order());
        Assert.Equal(target.Id, replayBody.GetProperty("userId").GetGuid()); Assert.True(replayBody.GetProperty("mustChangePassword").GetBoolean());
        Assert.Equal(JsonValueKind.Null, replayBody.GetProperty("temporaryPassword").ValueKind);
        Assert.DoesNotContain(temporary!, replayBody.GetRawText()); Assert.Equal(persistedState, await Snapshot());
        using var conflict = await client.PostAsJsonAsync(path, new { expectedTargetRowVersion = Convert.ToBase64String(new byte[8]), operationId = operation });
        await Error(conflict, 409, "duplicate_request"); Assert.Equal(persistedState, await Snapshot());
        using var stale = await client.PostAsJsonAsync(path, new { expectedTargetRowVersion = version, operationId = Guid.NewGuid() });
        await Error(stale, 409, "auth_concurrency_conflict"); Assert.Equal(persistedState, await Snapshot());
    }

    [Theory]
    [InlineData(UserRoleCode.ProjectManager)]
    [InlineData(UserRoleCode.DroneOperator)]
    [InlineData(UserRoleCode.RepairCrew)]
    [InlineData(UserRoleCode.Reporter)]
    public async Task EveryOtherRoleDeniesResetWithoutCredentialOrAuditEffects(UserRoleCode role)
    {
        var actor = await sql.CreateUserAsync("reset-denied-" + Guid.NewGuid().ToString("N"), "Current1!", role);
        var target = await sql.CreateUserAsync("reset-private-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.DroneOperator);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(); await Login(client, actor.Email!);
        string version, hash;
        await using (var db = sql.CreateDbContext()) { var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == target.Id); version = Convert.ToBase64String(user.RowVersion); hash = user.PasswordHash!; }
        using var response = await client.PostAsJsonAsync($"/api/v1/admin/users/{target.Id}/password-reset", new { expectedTargetRowVersion = version, operationId = Guid.NewGuid() });
        await Error(response, 403, "access_forbidden");
        await using (var db = sql.CreateDbContext())
        {
            var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == target.Id);
            Assert.Equal(hash, user.PasswordHash); Assert.Equal(version, Convert.ToBase64String(user.RowVersion));
            Assert.Empty(await db.PasswordResetLogs.Where(r => r.TargetUserId == target.Id).ToListAsync());
            Assert.Empty(await db.AuditLogs.Where(a => a.EntityId == target.Id).ToListAsync());
            Assert.Empty(await db.IdempotencyRecords.Where(r => r.ActorUserId == actor.Id && r.Operation == "AdminPasswordReset").ToListAsync());
        }
    }

    private static async Task Login(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
    }
    private static async Task Error(HttpResponseMessage response, int status, string code)
    {
        Assert.Equal((HttpStatusCode)status, response.StatusCode); Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(status, error.GetProperty("status").GetInt32()); Assert.Equal(code, error.GetProperty("code").GetString());
        Assert.Equal(Assert.Single(response.Headers.GetValues("X-Correlation-ID")), error.GetProperty("correlationId").GetString());
        Assert.All(error.EnumerateObject(), p => Assert.Contains(p.Name, new[] { "type", "title", "status", "detail", "instance", "code", "correlationId" }));
        Assert.Null(response.Headers.ETag); Assert.Null(response.Headers.Location);
    }
}
