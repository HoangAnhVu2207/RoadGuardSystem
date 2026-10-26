using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Processing;

[Collection(AuthenticationApiFixture.Name)]
public sealed class ProcessingCommandHeaderBoundaryAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExactCreateAndRetryHeaderBoundariesPreserveDurableState(bool retry)
    {
        var actor = await sql.CreateUserAsync("processing-header-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient();
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(actor.UserName!), password = "Current1!" });
        login.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Body(login)).GetProperty("accessToken").GetString());
        var path = retry ? $"/api/v1/processing-jobs/{Guid.NewGuid()}/retry" : "/api/v1/processing-jobs";
        object input = retry ? new { reason = "Header contract boundary" } : new
        {
            datasetId = Guid.NewGuid(),
            scope = new { routeVersionId = Guid.NewGuid(), segmentSetId = Guid.NewGuid(), segmentIds = new[] { Guid.NewGuid() }, targetBand = "SURFACE" },
            modelVersionId = Guid.NewGuid(),
            preprocessingVersion = "fixture.v1",
            configVersion = "fixture.v1",
            mode = "MOCK"
        };
        async Task<string> Snapshot()
        {
            await using var db = sql.CreateDbContext();
            return JsonSerializer.Serialize(new
            {
                jobs = await db.ProcessingJobs.CountAsync(),
                attempts = await db.ProcessingAttempts.CountAsync(),
                receipts = await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == actor.Id),
                audits = await db.AuditLogs.CountAsync(a => a.ActorUserId == actor.Id),
                outbox = await db.OutboxMessages.CountAsync()
            });
        }
        async Task Check(string? key, string? version, HttpStatusCode expected, string code, string? raw = null, string media = "application/json")
        {
            var before = await Snapshot();
            using var message = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = raw is null ? JsonContent.Create(input) : new StringContent(raw, System.Text.Encoding.UTF8, media)
            };
            if (key is not null) message.Headers.TryAddWithoutValidation("Idempotency-Key", key);
            if (version is not null) message.Headers.TryAddWithoutValidation("If-Match", version);
            using var response = await client.SendAsync(message);
            response.StatusCode.Should().Be(expected);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
            var error = await Body(response); error.GetProperty("status").GetInt32().Should().Be((int)expected);
            error.GetProperty("code").GetString().Should().Be(code);
            Guid.TryParse(response.Headers.GetValues("X-Correlation-ID").Single(), out _).Should().BeTrue();
            response.Headers.ETag.Should().BeNull(); response.Headers.Location.Should().BeNull();
            (await Snapshot()).Should().Be(before);
        }
        const string version = "\"AAAAAAAAAAE=\"";
        await Check(null, version, HttpStatusCode.PreconditionRequired, "validation_error");
        await Check("   ", version, HttpStatusCode.PreconditionRequired, "validation_error");
        if (retry) await Check(Guid.NewGuid().ToString("N"), null, HttpStatusCode.PreconditionRequired, "validation_error");
        await Check(new string('k', 200), version, HttpStatusCode.NotFound, "not_found");
        await Check(new string('k', 201), version, HttpStatusCode.BadRequest, "validation_error");
        await Check(Guid.NewGuid().ToString("N"), version, HttpStatusCode.BadRequest, "validation_error", "{");
        await Check(Guid.NewGuid().ToString("N"), version, HttpStatusCode.UnsupportedMediaType, "unsupported_media_type", "{}", "text/plain");
    }

    private static async Task<JsonElement> Body(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }
}
