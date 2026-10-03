using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Repositories.Files;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Services.Files;
using Xunit;

namespace RoadGuardSystem.ApiTests.Files;

[Collection(AuthenticationApiFixture.Name)]
[Trait("Package", "HUY-01")]
public sealed class MultipartRecoveryHttpTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task LostAcknowledgement_NewHostRecoversSameKey_ConflictsAndAuthority_CompleteDownload()
    {
        var user = await sql.CreateUserAsync($"recover-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var other = await sql.CreateUserAsync($"wrong-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var remote = new RemotePhoto();
        Guid upload, file;
        var partKey = Guid.NewGuid().ToString();
        await using (var initial = Factory(remote, loseAck: true))
        {
            using var client = initial.CreateClient(new() { BaseAddress = new Uri("https://localhost") }); await LoginAsync(client, user.UserName!);
            using var create = await CreateAsync(client); create.StatusCode.Should().Be(HttpStatusCode.Created);
            var body = await create.Content.ReadFromJsonAsync<JsonElement>(); upload = body.GetProperty("id").GetGuid(); file = body.GetProperty("fileId").GetGuid();
            using var failed = await PartsAsync(client, upload, partKey); failed.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            (await failed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().Should().Be("upload_storage_unavailable");
        }
        await using var db = sql.CreateDbContext();
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadSessions SET MultipartNextCheckAt={DateTimeOffset.UtcNow.AddMinutes(-1)} WHERE Id={upload}");
        await using var restarted = Factory(remote, loseAck: false);
        using var current = restarted.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginAsync(current, other.UserName!);
        (await PartsAsync(current, upload, partKey)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        await LoginAsync(current, user.UserName!);
        (await PartsAsync(current, upload, partKey)).StatusCode.Should().Be(HttpStatusCode.OK);
        using var secondCreate = await CreateAsync(current);
        var secondUpload = (await secondCreate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await PartsAsync(current, secondUpload, partKey)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PartsAsync(current, upload, partKey)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await PartsAsync(current, upload, partKey, [1, 2])).StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity); // invalid shape, never rewrites receipt
        var session = await current.GetAsync($"/api/v1/reporter-evidence/uploads/{upload}");
        using var complete = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reporter-evidence/uploads/{upload}/complete")
        { Content = JsonContent.Create(new { checksumSha256 = RemotePhoto.Hash, parts = new[] { new { partNumber = 1, eTag = "fixture" } } }) };
        complete.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString()); complete.Headers.TryAddWithoutValidation("If-Match", session.Headers.ETag!.ToString());
        (await current.SendAsync(complete)).StatusCode.Should().Be(HttpStatusCode.Accepted);
        using (var scope = restarted.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<IUploadService>().ProcessOneVerificationAsync();
        using var content = await current.GetAsync($"/api/v1/reporter-evidence/files/{file}/content"); content.StatusCode.Should().Be(HttpStatusCode.OK);
        Convert.ToHexString(SHA256.HashData(await content.Content.ReadAsByteArrayAsync())).ToLowerInvariant().Should().Be(RemotePhoto.Hash);
        remote.Initiations.Should().Be(1);
        (await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == user.Id && r.Operation == "UploadPartUrlsIssued")).Should().Be(1);
        (await db.Files.CountAsync(f => f.Id == file)).Should().Be(1);
        (await db.UploadSessions.AsNoTracking().SingleAsync(s => s.Id == upload)).Status.Should().Be(UploadSessionStatus.Verified);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Users SET Status={(byte)UserStatus.Suspended} WHERE Id={user.Id}");
        (await PartsAsync(current, upload, partKey)).StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LegacyTerminalViaRealWorker_ReturnsConflictAndPermitsNewCreate()
    {
        var user = await sql.CreateUserAsync($"legacy-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var remote = new RemotePhoto(); await using var factory = Factory(remote, false);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") }); await LoginAsync(client, user.UserName!);
        var response = await CreateAsync(client); var body = await response.Content.ReadFromJsonAsync<JsonElement>(); var upload = body.GetProperty("id").GetGuid();
        await using (var db = sql.CreateDbContext())
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadSessions SET FailureCode='multipart_initiating:legacy' WHERE Id={upload}");
        using (var scope = factory.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<IUploadRepository>().RecoverMultipartsAsync();
        (await PartsAsync(client, upload, Guid.NewGuid().ToString())).StatusCode.Should().Be(HttpStatusCode.Conflict);
        var restarted = await CreateAsync(client); restarted.StatusCode.Should().Be(HttpStatusCode.Created);
        var newUpload = (await restarted.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        newUpload.Should().NotBe(upload);
        (await PartsAsync(client, newUpload, Guid.NewGuid().ToString())).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private AuthenticationWebApplicationFactory Factory(RemotePhoto remote, bool loseAck)
        => new(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(new PhotoAdapter(remote, loseAck));
            services.Configure<UploadSessionOptions>(o => o.RecoveryRetrySeconds = 1);
        });
    private static async Task LoginAsync(HttpClient client, string name)
    { var r = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(name), password = "Current1!" }); r.EnsureSuccessStatusCode(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()); }
    private static Task<HttpResponseMessage> CreateAsync(HttpClient client)
    { var r = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reporter-evidence/uploads") { Content = JsonContent.Create(new { fileName = "synthetic.jpg", mediaType = "image/jpeg", sizeBytes = 4, checksumSha256 = RemotePhoto.Hash }) }; r.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString()); return client.SendAsync(r); }
    private static Task<HttpResponseMessage> PartsAsync(HttpClient client, Guid upload, string key, int[]? parts = null)
    { var r = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reporter-evidence/uploads/{upload}/part-urls") { Content = JsonContent.Create(new { partNumbers = parts ?? [1] }) }; r.Headers.Add("Idempotency-Key", key); return client.SendAsync(r); }
    private sealed class RemotePhoto
    {
        public static byte[] Bytes => [255, 216, 255, 0];
        public static string Hash => Convert.ToHexString(SHA256.HashData(Bytes)).ToLowerInvariant();
        public int Initiations { get; set; }
    }
    private sealed class PhotoAdapter(RemotePhoto remote, bool loseAck) : IUploadObjectStorage, IMultipartRecoveryStorage
    {
        public Task<string> InitiateAsync(string objectKey, string mediaType, CancellationToken cancellationToken = default)
        { remote.Initiations++; if (loseAck) throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable, "Synthetic lost remote acknowledgement"); return Task.FromResult("fixture-id"); }
        public Task<IReadOnlyList<string>> ListMultipartIdsAsync(string exactObjectKey, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<string>>(remote.Initiations == 0 ? [] : ["fixture-id"]);
        public Task<bool> HasPartsAsync(string objectKey, string uploadId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task AbortMultipartAsync(string objectKey, string uploadId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(string objectKey, string uploadId, IReadOnlyList<int> partNumbers, DateTimeOffset expiresAt, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PresignedUploadPart>>(partNumbers.Select(n => new PresignedUploadPart(n, "https://storage.invalid/part", expiresAt)).ToArray());
        public Task<UploadObjectVerification> CompleteAndVerifyAsync(string objectKey, string uploadId, IReadOnlyList<CompletedStoragePart> parts, CancellationToken cancellationToken = default) => Task.FromResult(new UploadObjectVerification(4, RemotePhoto.Hash, "image/jpeg"));
        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(new MemoryStream(RemotePhoto.Bytes));
    }
}
