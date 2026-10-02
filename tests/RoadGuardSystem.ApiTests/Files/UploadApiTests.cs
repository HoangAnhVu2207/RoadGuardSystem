using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Repositories.Storage;
using Xunit;

namespace RoadGuardSystem.ApiTests.Files;

[Trait("TaskId", "P2-023/P2-024/P2-025/P2-026/P2-027/P2-028")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class UploadApiTests
{
    private readonly AuthenticationSqlServerFixture _sql;

    public UploadApiTests(AuthenticationSqlServerFixture sql)
    {
        _sql = sql;
    }

    [Fact]
    public async Task UploadCreate_CurrentIntBoundaryOverflowsValidationWithoutWriting()
    {
        var supervisor = await _sql.CreateUserAsync($"upload-limit-supervisor-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var manager = await _sql.CreateUserAsync($"upload-limit-manager-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        await AuthenticateAsync(client, supervisor.UserName!);
        var projectId = await CreateProjectAsync(client, manager.Id);
        await AuthenticateAsync(client, manager.UserName!);

        int beforeSessions;
        int beforeFiles;
        await using (var before = _sql.CreateDbContext())
        {
            beforeSessions = await before.UploadSessions.AsNoTracking().CountAsync();
            beforeFiles = await before.Files.AsNoTracking().CountAsync();
        }

        foreach (var sizeBytes in new[] { (long)int.MaxValue + 1, 8L * 1024 * 1024 * 1024 })
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/uploads")
            {
                Content = JsonContent.Create(new
                {
                    purpose = "SURVEY_VIDEO",
                    projectId,
                    targetId = (Guid?)null,
                    fileName = "boundary.mp4",
                    mediaType = "video/mp4",
                    sizeBytes,
                    checksumSha256 = new string('a', 64)
                })
            };
            request.Headers.Add("Idempotency-Key", $"upload-limit-{Guid.NewGuid():N}");
            var response = await client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString()
                .Should().Be("internal_error");
            response.Headers.Contains("X-Correlation-ID").Should().BeTrue();
        }

        await using var verification = _sql.CreateDbContext();
        (await verification.FileScopes.AsNoTracking().CountAsync(scope => scope.ProjectId == projectId)).Should().Be(0);
        (await verification.UploadSessions.AsNoTracking().CountAsync()).Should().Be(beforeSessions);
        (await verification.Files.AsNoTracking().CountAsync()).Should().Be(beforeFiles);
    }

    [Fact]
    public async Task UploadEndpoints_EnforceScopeIdempotencyAndIfMatchBeforeVerifiedDownload()
    {
        var supervisor = await _sql.CreateUserAsync($"upload-supervisor-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var manager = await _sql.CreateUserAsync($"upload-manager-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var outsider = await _sql.CreateUserAsync($"upload-outsider-{Guid.NewGuid():N}", "Current1!", UserRoleCode.DroneOperator);
        var content = Encoding.UTF8.GetBytes("%PDF-1.7 upload endpoint fixture");
        var checksum = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        var storage = new EndpointUploadStorage(content, checksum);
        await using var factory = new AuthenticationWebApplicationFactory(
            _sql.ConnectionString,
            configureTestServices: services =>
            {
                services.RemoveAll<IUploadObjectStorage>();
                services.AddSingleton<IUploadObjectStorage>(storage);
            });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        await AuthenticateAsync(client, supervisor.UserName!);
        var projectId = await CreateProjectAsync(client, manager.Id);
        await AuthenticateAsync(client, manager.UserName!);

        var createPayload = new
        {
            purpose = "DOCUMENT",
            projectId,
            targetId = (Guid?)null,
            fileName = "evidence.pdf",
            mediaType = "application/pdf",
            sizeBytes = content.Length,
            checksumSha256 = checksum
        };
        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/uploads")
        {
            Content = JsonContent.Create(createPayload)
        };
        createRequest.Headers.Add("Idempotency-Key", $"upload-create-{Guid.NewGuid():N}");
        var created = await client.SendAsync(createRequest);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdBody = await created.Content.ReadFromJsonAsync<JsonElement>();
        var uploadId = createdBody.GetProperty("id").GetGuid();
        var fileId = createdBody.GetProperty("fileId").GetGuid();
        created.Headers.ETag.Should().NotBeNull();

        using var partRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/uploads/{uploadId}/part-urls")
        {
            Content = JsonContent.Create(new { partNumbers = new[] { 1 } })
        };
        partRequest.Headers.Add("Idempotency-Key", $"upload-parts-{Guid.NewGuid():N}");
        var partUrls = await client.SendAsync(partRequest);
        partUrls.StatusCode.Should().Be(HttpStatusCode.OK);

        var session = await client.GetAsync($"/api/v1/uploads/{uploadId}");
        session.StatusCode.Should().Be(HttpStatusCode.OK);
        var currentVersion = (await session.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetString()!;

        var stale = await CompleteAsync(client, uploadId, checksum, "//////////8=", $"upload-stale-{Guid.NewGuid():N}");
        stale.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        await using (var verification = _sql.CreateDbContext())
        {
            (await verification.UploadSessions.AsNoTracking().SingleAsync(item => item.Id == uploadId))
                .Status.Should().Be(UploadSessionStatus.Uploading);
        }

        var completed = await CompleteAsync(client, uploadId, checksum, currentVersion, $"upload-complete-{Guid.NewGuid():N}");
        completed.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await completed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("VERIFYING");

        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Files.IUploadService>().ProcessOneVerificationAsync();
        }

        var metadata = await client.GetAsync($"/api/v1/files/{fileId}");
        metadata.StatusCode.Should().Be(HttpStatusCode.OK);
        (await metadata.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("VERIFIED");
        var download = await client.GetAsync($"/api/v1/files/{fileId}/content");
        download.StatusCode.Should().Be(HttpStatusCode.OK);
        (await download.Content.ReadAsByteArrayAsync()).Should().Equal(content);

        await AuthenticateAsync(client, outsider.UserName!);
        var denied = await client.GetAsync($"/api/v1/uploads/{uploadId}");
        denied.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await using (var verification = _sql.CreateDbContext())
        {
            (await verification.UploadSessions.AsNoTracking().SingleAsync(item => item.Id == uploadId))
                .Status.Should().Be(UploadSessionStatus.Verified);
            (await verification.FileScopes.AsNoTracking().SingleAsync(item => item.FileId == fileId))
                .ProjectId.Should().Be(projectId);
        }
    }

    [MinioSmokeFact]
    [Trait("Requires", "Minio")]
    public async Task UploadEndpoints_CompleteMultipartUploadAgainstConfiguredMinio()
    {
        var supervisor = await _sql.CreateUserAsync($"minio-supervisor-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var manager = await _sql.CreateUserAsync($"minio-manager-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var content = Encoding.UTF8.GetBytes("%PDF-1.7 MinIO multipart upload smoke fixture");
        var checksum = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString, allowExternalStorage: true);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        await AuthenticateAsync(client, supervisor.UserName!);
        var projectId = await CreateProjectAsync(client, manager.Id);
        await AuthenticateAsync(client, manager.UserName!);

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/uploads")
        {
            Content = JsonContent.Create(new
            {
                purpose = "DOCUMENT",
                projectId,
                targetId = (Guid?)null,
                fileName = "minio-smoke.pdf",
                mediaType = "application/pdf",
                sizeBytes = content.Length,
                checksumSha256 = checksum
            })
        };
        createRequest.Headers.Add("Idempotency-Key", $"minio-create-{Guid.NewGuid():N}");
        var created = await client.SendAsync(createRequest);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdBody = await created.Content.ReadFromJsonAsync<JsonElement>();
        var uploadId = createdBody.GetProperty("id").GetGuid();
        var fileId = createdBody.GetProperty("fileId").GetGuid();

        using var partRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/uploads/{uploadId}/part-urls")
        {
            Content = JsonContent.Create(new { partNumbers = new[] { 1 } })
        };
        partRequest.Headers.Add("Idempotency-Key", $"minio-parts-{Guid.NewGuid():N}");
        var partUrlResponse = await client.SendAsync(partRequest);
        partUrlResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var partUrlBody = await partUrlResponse.Content.ReadFromJsonAsync<JsonElement>();
        var partUrl = partUrlBody.GetProperty("parts")[0].GetProperty("url").GetString();
        partUrl.Should().NotBeNullOrWhiteSpace();

        using var storageClient = new HttpClient();
        using var partContent = new ByteArrayContent(content);
        partContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        var putPart = await storageClient.PutAsync(partUrl, partContent);
        putPart.IsSuccessStatusCode.Should().BeTrue();
        var eTag = putPart.Headers.ETag?.Tag;
        eTag.Should().NotBeNullOrWhiteSpace();

        var session = await client.GetAsync($"/api/v1/uploads/{uploadId}");
        session.StatusCode.Should().Be(HttpStatusCode.OK);
        var version = (await session.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetString()!;
        var completed = await CompleteAsync(client, uploadId, checksum, version, $"minio-complete-{Guid.NewGuid():N}", eTag!);
        completed.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await completed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("VERIFYING");

        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Files.IUploadService>().ProcessOneVerificationAsync();
        }

        var metadata = await client.GetAsync($"/api/v1/files/{fileId}");
        metadata.StatusCode.Should().Be(HttpStatusCode.OK);
        (await metadata.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("VERIFIED");
        var download = await client.GetAsync($"/api/v1/files/{fileId}/content");
        download.StatusCode.Should().Be(HttpStatusCode.OK);
        (await download.Content.ReadAsByteArrayAsync()).Should().Equal(content);
    }

    private static async Task<HttpResponseMessage> CompleteAsync(
        HttpClient client,
        Guid uploadId,
        string checksum,
        string version,
        string idempotencyKey,
        string eTag = "etag-1")
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/uploads/{uploadId}/complete")
        {
            Content = JsonContent.Create(new { parts = new[] { new { partNumber = 1, eTag } }, checksumSha256 = checksum })
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");
        return await client.SendAsync(request);
    }

    private static async Task<Guid> CreateProjectAsync(HttpClient client, Guid managerId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            projectCode = $"UPLOAD-{Guid.NewGuid():N}",
            name = "Upload endpoint project",
            engineeringUtmSrid = 32648,
            startDate = "2026-09-01",
            endDate = "2027-09-01",
            primaryProjectManagerUserId = managerId,
            handover = new { documentNo = $"HD-{Guid.NewGuid():N}", handoverDate = "2026-08-31" },
            operationId = Guid.NewGuid()
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("projectId").GetGuid();
    }

    private static async Task AuthenticateAsync(HttpClient client, string username)
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = AuthenticationSqlServerFixture.EmailFor(username),
            password = "Current1!"
        });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
    }

    private sealed class EndpointUploadStorage : IUploadObjectStorage
    {
        private readonly byte[] _content;
        private readonly string _checksum;

        public EndpointUploadStorage(byte[] content, string checksum)
        {
            _content = content;
            _checksum = checksum;
        }

        public Task<string> InitiateAsync(string objectKey, string mediaType, CancellationToken cancellationToken = default)
            => Task.FromResult($"upload-{objectKey}");

        public Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(
            string objectKey,
            string uploadId,
            IReadOnlyList<int> partNumbers,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PresignedUploadPart>>(
                partNumbers.Select(partNumber => new PresignedUploadPart(partNumber, $"https://storage.test/{uploadId}/{partNumber}", expiresAt)).ToArray());

        public Task<UploadObjectVerification> CompleteAndVerifyAsync(
            string objectKey,
            string uploadId,
            IReadOnlyList<CompletedStoragePart> parts,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new UploadObjectVerification(_content.Length, _checksum, "application/pdf"));

        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default)
            => Task.FromResult<Stream>(new MemoryStream(_content, writable: false));
    }
}
