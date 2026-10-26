using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Identity;

[Collection(AuthenticationApiFixture.Name)]
public sealed class AdminResetExactInputAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task FrozenResetEnvelopeRejectsAdditionalMembersAndInvalidRequiredFieldsWithoutEffects()
    {
        var actor = await sql.CreateUserAsync("reset-exact-super-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        var target = await sql.CreateUserAsync("reset-exact-target-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        string version;
        await using (var db = sql.CreateDbContext()) version = Convert.ToBase64String((await db.Users.AsNoTracking().SingleAsync(u => u.Id == target.Id)).RowVersion);
        async Task<string> Snapshot()
        {
            await using var db = sql.CreateDbContext();
            return JsonSerializer.Serialize(new
            {
                user = await db.Users.AsNoTracking().Where(u => u.Id == target.Id).Select(u => new { u.PasswordHash, u.SecurityStamp, u.MustChangePassword, u.RowVersion }).SingleAsync(),
                logs = await db.PasswordResetLogs.Where(r => r.TargetUserId == target.Id).Select(r => r.Id).OrderBy(id => id).ToArrayAsync(),
                audits = await db.AuditLogs.Where(r => r.EntityId == target.Id).Select(r => r.Id).OrderBy(id => id).ToArrayAsync(),
                receipts = await db.IdempotencyRecords.Where(r => r.ActorUserId == actor.Id && r.Operation == "AdminPasswordReset").Select(r => r.Id).OrderBy(id => id).ToArrayAsync()
            });
        }
        var before = await Snapshot();
        var path = $"/api/v1/admin/users/{target.Id}/password-reset";
        var op = Guid.NewGuid();
        async Task Reject(object value)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path)
            { Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json") };
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(400, body.GetProperty("status").GetInt32());
            Assert.Equal("validation_error", body.GetProperty("code").GetString());
            Assert.Equal(Assert.Single(response.Headers.GetValues("X-Correlation-ID")), body.GetProperty("correlationId").GetString());
            Assert.Null(response.Headers.ETag); Assert.Null(response.Headers.Location);
            Assert.False(body.TryGetProperty("temporaryPassword", out _));
            Assert.DoesNotContain(target.Email!, body.GetRawText());
            Assert.Equal(before, await Snapshot());
        }
        foreach (var invalid in new string?[] { null, "", " ", "not-base64" })
            await Reject(new { expectedTargetRowVersion = invalid, operationId = op });
        await Reject(new { expectedTargetRowVersion = version });
        await Reject(new { expectedTargetRowVersion = version, operationId = (string?)null });
        await Reject(new { expectedTargetRowVersion = version, operationId = 123 });
        foreach (var field in new[] { "unknownField", "userId", "role", "password", "mustChangePassword", "temporaryPassword" })
            await Reject(new Dictionary<string, object> { ["expectedTargetRowVersion"] = version, ["operationId"] = op, [field] = "untrusted" });
    }
}
