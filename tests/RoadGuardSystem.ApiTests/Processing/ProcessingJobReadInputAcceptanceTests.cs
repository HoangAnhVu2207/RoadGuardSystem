using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Processing;

[Collection(AuthenticationApiFixture.Name)]
public sealed class ProcessingJobReadInputAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task ExactJobReadRejectsUnknownZeroAndMalformedIdentifiersWithoutBusinessWrites()
    {
        var actor = await sql.CreateUserAsync("job-read-input-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var credential = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credential);
        async Task<string> Snapshot()
        {
            await using var db = sql.CreateDbContext();
            return JsonSerializer.Serialize(new
            {
                jobs = await db.ProcessingJobs.CountAsync(),
                attempts = await db.ProcessingAttempts.CountAsync(),
                receipts = await db.IdempotencyRecords.CountAsync(),
                audits = await db.AuditLogs.CountAsync()
            });
        }
        var before = await Snapshot();
        foreach (var value in new[] { Guid.NewGuid().ToString(), Guid.Empty.ToString(), "not-a-guid", "123", "00000000-0000-0000-0000-00000000000g" })
        {
            using var response = await client.GetAsync("/api/v1/processing-jobs/" + value);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(404, problem.GetProperty("status").GetInt32());
            Assert.Equal("not_found", problem.GetProperty("code").GetString());
            var correlation = Assert.Single(response.Headers.GetValues("X-Correlation-ID"));
            Assert.True(Guid.TryParse(correlation, out _));
            Assert.Equal(correlation, problem.GetProperty("correlationId").GetString());
            Assert.Null(response.Headers.ETag); Assert.Null(response.Headers.Location);
            Assert.Empty(response.Headers.TryGetValues("Set-Cookie", out var cookies) ? cookies : []);
            var raw = problem.GetRawText();
            foreach (var secret in new[] { credential!, actor.Email!, "storageKey", "stackTrace", "connectionString" }) Assert.DoesNotContain(secret, raw);
            Assert.Equal(before, await Snapshot());
        }
    }
}
