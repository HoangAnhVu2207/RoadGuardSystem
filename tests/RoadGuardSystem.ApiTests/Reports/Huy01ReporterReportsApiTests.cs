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
        var replay = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", request, key);
        replay.StatusCode.Should().Be(HttpStatusCode.Created);

        await using var db = sql.CreateDbContext();
        (await db.Reports.CountAsync()).Should().Be(1);
        (await db.IncidentCases.CountAsync()).Should().Be(1);
        (await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink>().CountAsync(link => link.EndedAt == null)).Should().Be(1);
    }

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
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        if (etag is not null) request.Headers.TryAddWithoutValidation("If-Match", etag);
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
