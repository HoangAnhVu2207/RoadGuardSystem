using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Repositories.Extensions;
using RoadGuardSystem.Services.Extensions;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Reports;

[Collection(AuthenticationApiFixture.Name)]
public sealed class Huy01ReporterReportsApiTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [Trait("Package", "HUY-01")]
    [InlineData("{}", "evidence[0].location.latitude")]
    [InlineData("{\"latitude\":10}", "evidence[0].location.longitude")]
    [InlineData("{\"longitude\":106}", "evidence[0].location.latitude")]
    public async Task CreateReport_LocationMissingCoordinates_ReturnsFieldValidationProblem(string location, string field)
    {
        var reporter = await sql.CreateUserAsync($"reporter-location-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.AddHuy01ReporterPersistence();
            services.AddHuy01ReporterServices();
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginAsync(client, reporter.UserName!);

        var json = """
            {
              "description":"missing coordinate",
              "evidence":[{
                "fileId":"11111111-1111-1111-1111-111111111111",
                "fileVersion":"version",
                "locationSource":"CAPTURE",
                "location":LOCATION_VALUE
              }]
            }
            """.Replace("LOCATION_VALUE", location, StringComparison.Ordinal);
        var response = await SendRawAsync(client, json, "location-key");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("validation_error");
        problem.GetProperty("errors").TryGetProperty(field, out _).Should().BeTrue();
    }

    [Fact]
    [Trait("Package", "HUY-01")]
    public async Task CreateReport_ExplicitZeroCoordinates_ReplaysWithNormalizedKeyAndExactOutcome()
    {
        var reporter = await sql.CreateUserAsync($"reporter-key-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IUploadObjectStorage>();
            services.AddSingleton<IUploadObjectStorage>(new VerifiedPhotoStorage());
            services.AddHuy01ReporterPersistence();
            services.AddHuy01ReporterServices();
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginAsync(client, reporter.UserName!);
        var file = await UploadVerifiedAsync(client, factory);
        var payload = new
        {
            description = "Zero coordinate report",
            evidence = new[] { new { fileId = file.FileId, fileVersion = file.Version, locationSource = "CAPTURE", location = new { latitude = 0, longitude = 0 } } }
        };

        var first = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", payload, "  normalized-key  ");
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstBody = await first.Content.ReadAsStringAsync();
        var firstLocation = first.Headers.Location;
        var firstEtag = first.Headers.ETag;

        var replay = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", payload, "normalized-key");
        replay.StatusCode.Should().Be(HttpStatusCode.Created);
        (await replay.Content.ReadAsStringAsync()).Should().Be(firstBody);
        replay.Headers.Location.Should().Be(firstLocation);
        replay.Headers.ETag.Should().Be(firstEtag);
    }

    [Theory]
    [Trait("Package", "HUY-01")]
    [InlineData(" \t ")]
    [InlineData("k\u00e9y")]
    public async Task CreateReport_InvalidIdempotencyKey_ReturnsHeaderFieldValidationProblem(string key)
    {
        var reporter = await sql.CreateUserAsync($"reporter-invalid-key-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.AddHuy01ReporterPersistence();
            services.AddHuy01ReporterServices();
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginAsync(client, reporter.UserName!);

        var response = await SendRawAsync(client, """
            {"description":"invalid key","evidence":[{"fileId":"11111111-1111-1111-1111-111111111111","fileVersion":"version","locationSource":"UNKNOWN"}]}
            """, key);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("validation_error");
        problem.GetProperty("errors").TryGetProperty("Idempotency-Key", out _).Should().BeTrue();
    }

    [Fact]
    [Trait("Package", "HUY-01")]
    public async Task CreateReport_WithVerifiedOwnedEvidence_CreatesOneIntakeCase_AndReplaysExactly()
    {
        var reporter = await sql.CreateUserAsync($"reporter-report-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IUploadObjectStorage>();
            services.AddSingleton<IUploadObjectStorage>(new VerifiedPhotoStorage());
            services.AddHuy01ReporterPersistence();
            services.AddHuy01ReporterServices();
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        await LoginAsync(client, reporter.UserName!);
        var file = await UploadVerifiedAsync(client, factory);
        var key = Guid.NewGuid().ToString();
        var request = new
        {
            description = "Pothole beside the lane",
            evidence = new[]
            {
                new { fileId = file.FileId, fileVersion = file.Version, locationSource = "UNKNOWN" }
            }
        };

        var first = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", request, key);
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var reportId = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var afterFirst = await CountIntakeRowsAsync(sql, reporter.Id, reportId, key);
        afterFirst.Should().Be(new ReporterIntakeRowCounts(1, 1, 1, 1, 1, 1, 1, 1, 1, 1));

        var replay = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", request, key);
        replay.StatusCode.Should().Be(HttpStatusCode.Created);
        (await CountIntakeRowsAsync(sql, reporter.Id, reportId, key)).Should().Be(afterFirst);

        var conflictingReplay = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", new
        {
            description = "Different request payload",
            evidence = request.evidence
        }, key);
        conflictingReplay.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CountIntakeRowsAsync(sql, reporter.Id, reportId, key)).Should().Be(afterFirst);
    }

    private static async Task<ReporterIntakeRowCounts> CountIntakeRowsAsync(AuthenticationSqlServerFixture sql, Guid actorUserId,
        Guid reportId, string idempotencyKey)
    {
        await using var db = sql.CreateDbContext();
        var activeLink = await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink>()
            .SingleAsync(link => link.ReportId == reportId && link.EndedAt == null);
        var actorReports = db.Reports.Where(report => report.ReporterUserId == actorUserId).Select(report => report.Id);
        var actorActiveLinks = db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink>()
            .Where(link => actorReports.Contains(link.ReportId) && link.EndedAt == null);
        var actorIntakeCases = actorActiveLinks.Select(link => link.CaseId);

        return new ReporterIntakeRowCounts(
            await db.Reports.CountAsync(report => report.Id == reportId && report.ReporterUserId == actorUserId),
            await db.IncidentCases.CountAsync(@case => @case.Id == activeLink.CaseId),
            await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink>()
                .CountAsync(link => link.CaseId == activeLink.CaseId && link.ReportId == reportId && link.EndedAt == null),
            await db.AuditLogs.CountAsync(audit => audit.ActorUserId == actorUserId && audit.EventType == "report_received" &&
                audit.EntityType == "Report" && audit.EntityId == reportId),
            await db.IdempotencyRecords.CountAsync(record => record.ActorUserId == actorUserId && record.ProjectId == null &&
                record.Operation == "huy01.report.create.v1" && record.IdempotencyKey == idempotencyKey && record.OperationId == reportId),
            await actorReports.CountAsync(),
            await db.IncidentCases.CountAsync(@case => actorIntakeCases.Contains(@case.Id)),
            await actorActiveLinks.CountAsync(),
            await db.AuditLogs.CountAsync(audit => audit.ActorUserId == actorUserId && audit.EventType == "report_received" &&
                audit.EntityType == "Report" && audit.Source == "huy01.reporter-intake"),
            await db.IdempotencyRecords.CountAsync(record => record.ActorUserId == actorUserId && record.ProjectId == null &&
                record.Operation == "huy01.report.create.v1"));
    }

    private sealed record ReporterIntakeRowCounts(
        int TargetReports, int TargetIntakeCases, int TargetActiveLinks, int TargetAudits, int TargetReceipts,
        int ActorReports, int ActorIntakeCases, int ActorActiveLinks, int ActorAudits, int ActorReceipts);

    private static async Task<(Guid FileId, string Version)> UploadVerifiedAsync(HttpClient client, AuthenticationWebApplicationFactory factory)
    {
        var hash = Convert.ToHexString(SHA256.HashData([1, 2, 3, 4])).ToLowerInvariant();
        var created = await SendAsync(client, HttpMethod.Post, "/api/v1/reporter-evidence/uploads", new
        {
            fileName = "report.jpg", mediaType = "image/jpeg", sizeBytes = 4L, checksumSha256 = hash
        }, Guid.NewGuid().ToString());
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var session = await created.Content.ReadFromJsonAsync<JsonElement>();
        var uploadId = session.GetProperty("id").GetGuid();
        var fileId = session.GetProperty("fileId").GetGuid();

        var parts = await SendAsync(client, HttpMethod.Post, $"/api/v1/reporter-evidence/uploads/{uploadId}/part-urls", new { partNumbers = new[] { 1 } }, Guid.NewGuid().ToString());
        parts.StatusCode.Should().Be(HttpStatusCode.OK);
        var current = await client.GetAsync($"/api/v1/reporter-evidence/uploads/{uploadId}");
        var complete = await SendAsync(client, HttpMethod.Post, $"/api/v1/reporter-evidence/uploads/{uploadId}/complete", new
        {
            checksumSha256 = hash, parts = new[] { new { partNumber = 1, eTag = "part" } }
        }, Guid.NewGuid().ToString(), current.Headers.ETag!.ToString());
        complete.StatusCode.Should().Be(HttpStatusCode.Accepted);

        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Files.IUploadService>().ProcessOneVerificationAsync();
        var metadata = await client.GetAsync($"/api/v1/reporter-evidence/files/{fileId}");
        metadata.StatusCode.Should().Be(HttpStatusCode.OK);
        return (fileId, metadata.Headers.ETag!.Tag!.Trim('"'));
    }

    private static async Task LoginAsync(HttpClient client, string userName)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = AuthenticationSqlServerFixture.EmailFor(userName), password = "Current1!"
        });
        response.EnsureSuccessStatusCode();
        var token = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object body, string idempotencyKey, string? etag = null)
    {
        var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
        request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        if (etag is not null) request.Headers.TryAddWithoutValidation("If-Match", etag);
        return client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> SendRawAsync(HttpClient client, string json, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reports")
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        return client.SendAsync(request);
    }

    private sealed class VerifiedPhotoStorage : IUploadObjectStorage
    {
        public Task<string> InitiateAsync(string objectKey, string mediaType, CancellationToken cancellationToken = default)
            => Task.FromResult("upload");
        public Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(string objectKey, string uploadId, IReadOnlyList<int> partNumbers, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PresignedUploadPart>>(partNumbers.Select(number => new PresignedUploadPart(number, "https://example.test/part", expiresAt)).ToArray());
        public Task<UploadObjectVerification> CompleteAndVerifyAsync(string objectKey, string uploadId, IReadOnlyList<CompletedStoragePart> parts, CancellationToken cancellationToken = default)
            => Task.FromResult(new UploadObjectVerification(4, Convert.ToHexString(SHA256.HashData([1, 2, 3, 4])).ToLowerInvariant(), "image/jpeg"));
        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(new MemoryStream([1, 2, 3, 4]));
    }
}
