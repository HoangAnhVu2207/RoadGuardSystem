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
using Amazon.S3;
using Amazon.S3.Model;
using DotNet.Testcontainers.Builders;
using RoadGuardSystem.Repositories.Options;

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
    public async Task UploadCreate_DocumentRetainsIntLimitWithoutWriting()
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
                    purpose = "DOCUMENT",
                    projectId,
                    targetId = (Guid?)null,
                    fileName = "boundary.pdf",
                    mediaType = "application/pdf",
                    sizeBytes,
                    checksumSha256 = new string('a', 64)
                })
            };
            request.Headers.Add("Idempotency-Key", $"upload-limit-{Guid.NewGuid():N}");
            var response = await client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString()
                .Should().Be("upload_validation_failed");
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
        var malformed = await CompleteAsync(client, uploadId, checksum, "not-base64", $"upload-malformed-{Guid.NewGuid():N}");
        malformed.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
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
        => await VerifyMinioMultipartAsync();

    [OwnedMinioSmokeFact]
    public async Task UploadEndpoints_OwnedMinioPreservesBytesAndRejectsPrivateFileOutsider()
    {
        _sql.GetOwnedSeedDatabaseIdentity();
        var access = "sbx" + Guid.NewGuid().ToString("N");
        var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var approvedImage = Environment.GetEnvironmentVariable("RG_MINIO_IMAGE")
            ?? throw new InvalidOperationException("RG_MINIO_IMAGE must identify the approved sandbox image.");
        await using var minio = new ContainerBuilder(approvedImage)
            .WithEnvironment("MINIO_ROOT_USER", access).WithEnvironment("MINIO_ROOT_PASSWORD", secret)
            .WithCommand("server", "/data").WithPortBinding(9000, true)
            // The approved image can be shell-less; probe its public readiness endpoint.
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(
                request => request.ForPort(9000).ForPath("/minio/health/ready"),
                strategy => strategy.WithTimeout(TimeSpan.FromSeconds(30)))).Build();
        await minio.StartAsync();
        var endpoint = $"http://{minio.Hostname}:{minio.GetMappedPublicPort(9000)}";
        var bucket = "sbx-" + Guid.NewGuid().ToString("N");
        using var s3 = new AmazonS3Client(access, secret, new AmazonS3Config
        { ServiceURL = endpoint, ForcePathStyle = true, AuthenticationRegion = "us-east-1" });
        await s3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });
        await VerifyMinioMultipartAsync(services => services.PostConfigure<MinioStorageOptions>(options =>
        {
            options.Endpoint = endpoint; options.UseSsl = false; options.BucketName = bucket;
            options.AccessKey = access; options.SecretKey = secret;
        }));
        // Reporter-private bytes use the same real adapter, separate from project scope.
        var reporter = await _sql.CreateUserAsync($"minio-reporter-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var outsider = await _sql.CreateUserAsync($"minio-private-other-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString,
            configureTestServices: services => services.PostConfigure<MinioStorageOptions>(options =>
            { options.Endpoint = endpoint; options.UseSsl = false; options.BucketName = bucket; options.AccessKey = access; options.SecretKey = secret; }),
            allowExternalStorage: true);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await AuthenticateAsync(client, reporter.UserName!);
        var bytes = new byte[] { 255, 216, 255, 0 };
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        using var create = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reporter-evidence/uploads")
        { Content = JsonContent.Create(new { fileName = "private.jpg", mediaType = "image/jpeg", sizeBytes = bytes.Length, checksumSha256 = hash }) };
        create.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var created = await client.SendAsync(create); created.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        var upload = body.GetProperty("id").GetGuid(); var file = body.GetProperty("fileId").GetGuid();
        using var parts = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reporter-evidence/uploads/{upload}/part-urls")
        { Content = JsonContent.Create(new { partNumbers = new[] { 1 } }) };
        parts.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var signingStartedAt = DateTimeOffset.UtcNow;
        var signed = await client.SendAsync(parts); signed.StatusCode.Should().Be(HttpStatusCode.OK);
        var signingCompletedAt = DateTimeOffset.UtcNow;
        var signedPart = (await signed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("parts")[0];
        var partUrl = signedPart.GetProperty("url").GetString();
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(partUrl!).Query);
        // AWS SDK uses the storage endpoint's corrected clock for this duration.
        // A rounded Date header can make it 301 while absolute expiry stays unchanged.
        // Check the approved 300-second lifetime against the request clock instead.
        var receiptExpiry = signedPart.GetProperty("expiresAt").GetDateTimeOffset();
        receiptExpiry.Should().BeOnOrAfter(signingStartedAt.AddSeconds(300));
        receiptExpiry.Should().BeOnOrBefore(signingCompletedAt.AddSeconds(300));
        int.Parse(query["X-Amz-Expires"].ToString(), System.Globalization.CultureInfo.InvariantCulture).Should().BePositive();
        var signedAt = DateTimeOffset.ParseExact(query["X-Amz-Date"].ToString(), "yyyyMMdd'T'HHmmss'Z'", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal);
        var providerExpiry = signedAt.AddSeconds(int.Parse(query["X-Amz-Expires"].ToString(), System.Globalization.CultureInfo.InvariantCulture));
        // The SDK signs whole seconds and can round either side of the receipt's
        // fractional second. This verifies provider precision, not strict TTL equality.
        Math.Abs((signedPart.GetProperty("expiresAt").GetDateTimeOffset() - providerExpiry).TotalSeconds)
            .Should().BeLessThan(2);
        // Owner-approved: a previously issued capability remains usable until original expiry.
        await using (var revoked = _sql.CreateDbContext())
        { var actor = await revoked.Users.SingleAsync(x => x.Id == reporter.Id); actor.Status = UserStatus.Suspended; await revoked.SaveChangesAsync(); }
        using var deniedParts = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reporter-evidence/uploads/{upload}/part-urls")
        { Content = JsonContent.Create(new { partNumbers = new[] { 1 } }) };
        deniedParts.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        (await client.SendAsync(deniedParts)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var storage = new HttpClient();
        using var payload = new ByteArrayContent(bytes); payload.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        var put = await storage.PutAsync(partUrl, payload); put.EnsureSuccessStatusCode();
        await using (var restored = _sql.CreateDbContext())
        { var actor = await restored.Users.SingleAsync(x => x.Id == reporter.Id); actor.Status = UserStatus.Active; await restored.SaveChangesAsync(); }
        var session = await client.GetAsync($"/api/v1/reporter-evidence/uploads/{upload}");
        using var complete = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reporter-evidence/uploads/{upload}/complete")
        { Content = JsonContent.Create(new { checksumSha256 = hash, parts = new[] { new { partNumber = 1, eTag = put.Headers.ETag!.Tag } } }) };
        complete.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        complete.Headers.TryAddWithoutValidation("If-Match", session.Headers.ETag!.ToString());
        (await client.SendAsync(complete)).StatusCode.Should().Be(HttpStatusCode.Accepted);
        using (var scope = factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Files.IUploadService>().ProcessOneVerificationAsync();
        var path = $"/api/v1/reporter-evidence/files/{file}/content";
        var downloaded = await client.GetAsync(path); downloaded.StatusCode.Should().Be(HttpStatusCode.OK);
        (await downloaded.Content.ReadAsByteArrayAsync()).Should().Equal(bytes);
        var capability = new Uri(partUrl!);
        (await storage.GetAsync(capability.GetLeftPart(UriPartial.Authority) + capability.AbsolutePath)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await AuthenticateAsync(client, outsider.UserName!);
        (await client.GetAsync(path)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        await AuthenticateAsync(client, reporter.UserName!);
        await using (var db = _sql.CreateDbContext())
        { var actor = await db.Users.SingleAsync(x => x.Id == reporter.Id); actor.Status = UserStatus.Suspended; await db.SaveChangesAsync(); }
        (await client.GetAsync(path)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await using var post = _sql.CreateDbContext();
        var stored = await post.UploadSessions.AsNoTracking().SingleAsync(x => x.Id == upload);
        stored.Status.Should().Be(UploadSessionStatus.Verified);
        (await post.FileScopes.AsNoTracking().SingleAsync(x => x.FileId == file)).ProjectId.Should().BeNull();
        stored.OwnerUserId.Should().Be(reporter.Id);
        // A short-expiry provider probe is separate from the production 300-second policy.
        var shortAdapter = new MinioUploadObjectStorage(Microsoft.Extensions.Options.Options.Create(new MinioStorageOptions
        { Endpoint = endpoint, UseSsl = false, BucketName = bucket, AccessKey = access, SecretKey = secret }));
        var shortKey = "expiry-probe-" + Guid.NewGuid().ToString("N");
        var shortUpload = await shortAdapter.InitiateAsync(shortKey, "image/jpeg");
        var shortPart = (await shortAdapter.PresignPartsAsync(shortKey, shortUpload, [1], DateTimeOffset.UtcNow.AddSeconds(3))).Single();
        using (var beforeExpiry = await storage.PutAsync(shortPart.Url, new ByteArrayContent(bytes)))
            beforeExpiry.StatusCode.Should().Be(HttpStatusCode.OK);
        var shortQuery = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(shortPart.Url).Query);
        var shortSignedAt = DateTimeOffset.ParseExact(shortQuery["X-Amz-Date"].ToString(), "yyyyMMdd'T'HHmmss'Z'", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal);
        var shortExpires = shortSignedAt.AddSeconds(int.Parse(shortQuery["X-Amz-Expires"].ToString(), System.Globalization.CultureInfo.InvariantCulture));
        await Task.Delay(shortExpires - DateTimeOffset.UtcNow + TimeSpan.FromSeconds(1));
        using (var expired = await storage.PutAsync(shortPart.Url, new ByteArrayContent(new byte[] { 1, 2 })))
            expired.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using (var malformed = await storage.PutAsync(shortPart.Url.Replace("partNumber=1", "partNumber=2", StringComparison.Ordinal), new ByteArrayContent(bytes)))
            malformed.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var shortParts = await s3.ListPartsAsync(new ListPartsRequest { BucketName = bucket, Key = shortKey, UploadId = shortUpload });
        shortParts.Parts.Should().ContainSingle().Which.Size.Should().Be(bytes.Length);
        await shortAdapter.AbortMultipartAsync(shortKey, shortUpload);
        // Suspended reporter still cannot renew after the provider expiry probe.
        using var renewal = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reporter-evidence/uploads/{upload}/part-urls")
        { Content = JsonContent.Create(new { partNumbers = new[] { 1 } }) };
        renewal.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        (await client.SendAsync(renewal)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        // Byte preservation is checked through authenticated storage; never emit key or credentials.
        using var actual = await s3.GetObjectAsync(bucket, stored.ObjectKey);
        using var buffer = new MemoryStream(); await actual.ResponseStream.CopyToAsync(buffer);
        buffer.ToArray().Should().Equal(bytes);
    }

    private async Task VerifyMinioMultipartAsync(Action<IServiceCollection>? configureStorage = null)
    {
        var supervisor = await _sql.CreateUserAsync($"minio-supervisor-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var manager = await _sql.CreateUserAsync($"minio-manager-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var content = Encoding.UTF8.GetBytes("%PDF-1.7 MinIO multipart upload smoke fixture");
        var checksum = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString,
            configureTestServices: configureStorage, allowExternalStorage: true);
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
        // Signed capability is bound to its method/query. Never print the URL or credentials.
        if (configureStorage is not null)
        {
            using var malformed = await storageClient.PutAsync(partUrl + "&partNumber=999", new ByteArrayContent(content));
            malformed.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
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

        if (configureStorage is not null)
        {
            await using var ownDb = _sql.CreateDbContext();
            var objectKey = (await ownDb.UploadSessions.AsNoTracking().SingleAsync(x => x.Id == uploadId)).ObjectKey;
            var signed = new Uri(partUrl!);
            var objectUri = new Uri(signed.GetLeftPart(UriPartial.Authority) + signed.AbsolutePath);
            using var anonymous = await storageClient.GetAsync(objectUri);
            anonymous.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            objectKey.Should().NotBeNullOrWhiteSpace();
        }
        var outsider = await _sql.CreateUserAsync($"minio-outsider-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        await AuthenticateAsync(client, outsider.UserName!);
        (await client.GetAsync($"/api/v1/files/{fileId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync($"/api/v1/files/{fileId}/content")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync($"/api/v1/uploads/{uploadId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await using var verification = _sql.CreateDbContext();
        (await verification.UploadSessions.AsNoTracking().SingleAsync(x => x.Id == uploadId)).Status.Should().Be(UploadSessionStatus.Verified);
        (await verification.FileScopes.AsNoTracking().SingleAsync(x => x.FileId == fileId)).ProjectId.Should().Be(projectId);
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

// An absent approved image is a missing provider prerequisite, not an API result.
public sealed class OwnedMinioSmokeFactAttribute : FactAttribute
{
    public OwnedMinioSmokeFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RG_MINIO_IMAGE")))
        {
            Skip = "Set RG_MINIO_IMAGE to the approved sandbox MinIO image to run the owned provider test.";
        }
    }
}
