using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Identity;

public sealed class AdminResetReplayRaceAcceptanceTests(AuthenticationSqlServerFixture sql)
    : IClassFixture<AuthenticationSqlServerFixture>
{
    [Fact]
    public async Task OperationKeyIsActorScopedAndTargetAndVersionBound()
    {
        var first = await sql.CreateUserAsync("reset-scope-first-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        var second = await sql.CreateUserAsync("reset-scope-second-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        var a = await sql.CreateUserAsync("reset-scope-a-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter);
        var b = await sql.CreateUserAsync("reset-scope-b-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.DroneOperator);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var one = factory.CreateClient(); using var two = factory.CreateClient();
        await Login(one, first.Email!); await Login(two, second.Email!);
        var key = Guid.NewGuid(); var va = await Version(a.Id); var vb = await Version(b.Id);
        using (var result = await one.PostAsJsonAsync(Path(a.Id), new { expectedTargetRowVersion = va, operationId = key })) Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        using (var result = await two.PostAsJsonAsync(Path(b.Id), new { expectedTargetRowVersion = vb, operationId = key })) Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var after = await Snapshot(a.Id, b.Id);
        using (var wrongTarget = await one.PostAsJsonAsync(Path(b.Id), new { expectedTargetRowVersion = vb, operationId = key })) await Error(wrongTarget, "duplicate_request");
        Assert.Equal(after, await Snapshot(a.Id, b.Id));
        using (var wrongVersion = await one.PostAsJsonAsync(Path(a.Id), new { expectedTargetRowVersion = await Version(a.Id), operationId = key })) await Error(wrongVersion, "duplicate_request");
        Assert.Equal(after, await Snapshot(a.Id, b.Id));
        using var replay = await one.PostAsJsonAsync(Path(a.Id), new { expectedTargetRowVersion = va, operationId = key });
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        var body = await replay.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(a.Id, body.GetProperty("userId").GetGuid()); Assert.Equal(JsonValueKind.Null, body.GetProperty("temporaryPassword").ValueKind);
        Assert.Equal(after, await Snapshot(a.Id, b.Id));
        await using var db = sql.CreateDbContext();
        var receipts = await db.IdempotencyRecords.Where(r => r.Operation == "AdminPasswordReset" && r.IdempotencyKey == key.ToString("N")).ToArrayAsync();
        Assert.Equal(2, receipts.Length); Assert.Equal(new[] { first.Id, second.Id }.Order(), receipts.Select(r => r.ActorUserId!.Value).Order());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RacingResetCommitsOneCredentialAndHistory(bool sameOperation)
    {
        var actor = await sql.CreateUserAsync("reset-race-super-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        var target = await sql.CreateUserAsync("reset-race-target-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter);
        using var errors = new ExceptionKinds();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, loggerProvider: errors);
        using var one = factory.CreateClient(); using var two = factory.CreateClient();
        await Login(one, actor.Email!); await Login(two, actor.Email!);
        var version = await Version(target.Id); var key = Guid.NewGuid();
        var results = await Task.WhenAll(one.PostAsJsonAsync(Path(target.Id), new { expectedTargetRowVersion = version, operationId = key }),
            two.PostAsJsonAsync(Path(target.Id), new { expectedTargetRowVersion = version, operationId = sameOperation ? key : Guid.NewGuid() }));
        try
        {
            Assert.True(results.All(r => r.StatusCode != HttpStatusCode.InternalServerError), string.Join(";", errors.Kinds.Distinct()));
            if (sameOperation) Assert.InRange(results.Count(r => r.StatusCode == HttpStatusCode.OK), 1, 2);
            else Assert.Equal(1, results.Count(r => r.StatusCode == HttpStatusCode.OK));
            var bodies = new List<JsonElement>();
            foreach (var result in results)
            {
                if (result.StatusCode == HttpStatusCode.OK) bodies.Add(await result.Content.ReadFromJsonAsync<JsonElement>());
                else await Error(result, "auth_concurrency_conflict");
            }
            Assert.Single(bodies.Where(b => b.GetProperty("temporaryPassword").ValueKind == JsonValueKind.String));
            Assert.All(bodies, b => { Assert.Equal(target.Id, b.GetProperty("userId").GetGuid()); Assert.True(b.GetProperty("mustChangePassword").GetBoolean()); });
            await using var db = sql.CreateDbContext();
            Assert.Single(await db.PasswordResetLogs.Where(r => r.TargetUserId == target.Id).ToArrayAsync());
            Assert.Single(await db.AuditLogs.Where(a => a.EntityId == target.Id && a.EventType == "user_password_reset").ToArrayAsync());
            Assert.Single(await db.IdempotencyRecords.Where(r => r.ActorUserId == actor.Id && r.Operation == "AdminPasswordReset").ToArrayAsync());
            Assert.NotEqual(version, await Version(target.Id));
            if (sameOperation)
            {
                var committedVersion = await Version(target.Id);
                using var durableReplay = await one.PostAsJsonAsync(Path(target.Id), new { expectedTargetRowVersion = version, operationId = key });
                Assert.Equal(HttpStatusCode.OK, durableReplay.StatusCode);
                var replay = await durableReplay.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal(target.Id, replay.GetProperty("userId").GetGuid());
                Assert.Equal(JsonValueKind.Null, replay.GetProperty("temporaryPassword").ValueKind);
                Assert.Equal(committedVersion, await Version(target.Id));
                Assert.Single(await db.PasswordResetLogs.Where(r => r.TargetUserId == target.Id).ToArrayAsync());
                Assert.Single(await db.AuditLogs.Where(a => a.EntityId == target.Id && a.EventType == "user_password_reset").ToArrayAsync());
                Assert.Single(await db.IdempotencyRecords.Where(r => r.ActorUserId == actor.Id && r.Operation == "AdminPasswordReset").ToArrayAsync());
            }
        }
        finally { foreach (var response in results) response.Dispose(); }
    }
    private static string Path(Guid id) => $"/api/v1/admin/users/{id}/password-reset";
    private async Task<string> Version(Guid id)
    { await using var db = sql.CreateDbContext(); return Convert.ToBase64String((await db.Users.AsNoTracking().SingleAsync(u => u.Id == id)).RowVersion); }
    private async Task<string> Snapshot(Guid a, Guid b)
    {
        await using var db = sql.CreateDbContext();
        return JsonSerializer.Serialize(new
        {
            users = await db.Users.AsNoTracking().Where(u => u.Id == a || u.Id == b).OrderBy(u => u.Id).ToArrayAsync(),
            resets = await db.PasswordResetLogs.AsNoTracking().Where(r => r.TargetUserId == a || r.TargetUserId == b).OrderBy(r => r.Id).ToArrayAsync(),
            audits = await db.AuditLogs.AsNoTracking().Where(r => r.EntityId == a || r.EntityId == b).OrderBy(r => r.Id).ToArrayAsync()
        });
    }
    private static async Task Login(HttpClient client, string email)
    {
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
    }
    private static async Task Error(HttpResponseMessage response, string code)
    {
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(409, body.GetProperty("status").GetInt32()); Assert.Equal(code, body.GetProperty("code").GetString());
        Assert.Equal(Assert.Single(response.Headers.GetValues("X-Correlation-ID")), body.GetProperty("correlationId").GetString());
        Assert.DoesNotContain("temporaryPassword", body.GetRawText());
    }
    private sealed class ExceptionKinds : ILoggerProvider
    {
        public System.Collections.Concurrent.ConcurrentQueue<string> Kinds { get; } = new();
        public ILogger CreateLogger(string categoryName) => new Sink(Kinds);
        public void Dispose() { }
        private sealed class Sink(System.Collections.Concurrent.ConcurrentQueue<string> kinds) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                for (var e = exception; e is not null; e = e.InnerException)
                    kinds.Enqueue(e is SqlException sql ? "SQL:" + sql.Number : e.GetType().Name);
            }
        }
    }
}
