using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Processing.Worker;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.Repositories.Storage;
using Xunit;

namespace RoadGuardSystem.ApiTests.Processing;

/// <summary>Real HTTP routing/authentication/service proof; persistence and artifact storage are explicit fakes.</summary>
public sealed class AiVideoTelemetryHttpTests(AuthenticationSqlServerFixture sql) : IClassFixture<AuthenticationSqlServerFixture>
{
    [Fact]
    public async Task Disabled_worker_starts_without_token_and_anonymous_cannot_read_or_append()
    {
        var repository = new TelemetryRepository();
        await using var factory = Host(repository, new ArtifactStore());
        using var client = Client(factory);
        using var get = await client.GetAsync(repository.Path + "/telemetry");
        using var post = await Send(client, repository, Estimate(), "anonymous", Quote(Guid.Empty));
        Assert.Equal(HttpStatusCode.Unauthorized, get.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, post.StatusCode);
        Assert.Equal(0, repository.ReadCalls);
        Assert.Equal(0, repository.AppendCalls);
    }

    [Theory]
    [InlineData(UserRoleCode.DroneOperator)]
    [InlineData(UserRoleCode.ProjectManager)]
    [InlineData(UserRoleCode.Supervisor)]
    public async Task Authorized_reader_gets_missing_telemetry_body_initial_etag_and_discovery(UserRoleCode role)
    {
        var repository = new TelemetryRepository();
        await using var factory = Host(repository, new ArtifactStore());
        using var client = Client(factory);
        await Login(client, role);
        using var response = await client.GetAsync(repository.Path + "/telemetry");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(Quote(Guid.Empty), response.Headers.ETag?.Tag);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(repository.WorkId, body.GetProperty("workId").GetGuid());
        Assert.Equal(repository.AttemptId, body.GetProperty("attemptId").GetGuid());
        Assert.Equal(repository.FileId, body.GetProperty("sourceVideoFileId").GetGuid());
        Assert.Equal("MISSING_TELEMETRY", body.GetProperty("status").GetString());
        Assert.Equal("DRONE_REFERENCE_NOT_GROUND_LOCATION", body.GetProperty("positionMeaning").GetString());
        Assert.Equal(0, body.GetProperty("intervals").GetArrayLength());
        using var discovery = await client.GetAsync($"/api/v1/projects/{repository.ProjectId}/ai-videos?datasetId={repository.DatasetId}&offset=2&limit=3");
        Assert.Equal(HttpStatusCode.OK, discovery.StatusCode);
        var page = await discovery.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, page.GetProperty("offset").GetInt32());
        Assert.Equal(3, page.GetProperty("limit").GetInt32());
        Assert.Equal(repository.WorkId, page.GetProperty("items")[0].GetProperty("workId").GetGuid());
        Assert.Equal(role, repository.LastRole);
        Assert.Equal(repository.DatasetId, repository.LastDatasetId);
    }

    [Theory]
    [InlineData(null, null, 428, "required_precondition")]
    [InlineData("key", null, 428, "required_precondition")]
    [InlineData(null, "\"00000000-0000-0000-0000-000000000000\"", 428, "required_precondition")]
    [InlineData("key", "*", 422, "invalid_telemetry_version")]
    [InlineData("key", "00000000-0000-0000-0000-000000000000", 422, "invalid_telemetry_version")]
    [InlineData("key", "W/\"00000000-0000-0000-0000-000000000000\"", 422, "invalid_telemetry_version")]
    public async Task Append_requires_exact_preconditions(string? key, string? match, int status, string code)
    {
        var repository = new TelemetryRepository();
        await using var factory = Host(repository, new ArtifactStore());
        using var client = Client(factory);
        await Login(client, UserRoleCode.DroneOperator);
        using var response = await Send(client, repository, Estimate(), key, match);
        await Problem(response, status, code);
        Assert.Equal(0, repository.ReadCalls);
        Assert.Equal(0, repository.AppendCalls);
    }

    [Theory]
    [InlineData(UserRoleCode.ProjectManager)]
    [InlineData(UserRoleCode.Supervisor)]
    public async Task Reader_roles_cannot_append(UserRoleCode role)
    {
        var repository = new TelemetryRepository();
        await using var factory = Host(repository, new ArtifactStore());
        using var client = Client(factory);
        await Login(client, role);
        using var response = await Send(client, repository, Estimate(), "key", Quote(Guid.Empty));
        await Problem(response, 403, "access_forbidden");
        Assert.Equal(0, repository.AppendCalls);
        Assert.Equal(0, repository.ReadCalls);
    }

    [Fact]
    public async Task Operator_append_replay_conflict_and_stale_head_are_mapped_over_http()
    {
        var repository = new TelemetryRepository();
        var artifacts = new ArtifactStore();
        await using var factory = Host(repository, artifacts);
        using var client = Client(factory);
        await Login(client, UserRoleCode.DroneOperator);
        using var first = await Send(client, repository, Estimate(), "same-key", Quote(Guid.Empty));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var id = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("revisionId").GetGuid();
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal(Quote(id), first.Headers.ETag?.Tag);
        using var replay = await Send(client, repository, Estimate(), "same-key", Quote(Guid.Empty));
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(id, (await replay.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("revisionId").GetGuid());
        Assert.Equal(first.Headers.ETag, replay.Headers.ETag);
        using var conflict = await Send(client, repository, Estimate("Changed rationale"), "same-key", Quote(Guid.Empty));
        await Problem(conflict, 409, "idempotency_conflict");
        using var stale = await Send(client, repository, Estimate(), "new-key", Quote(Guid.Empty));
        await Problem(stale, 412, "telemetry_revision_stale");
        Assert.Single(repository.Revisions);
        Assert.Equal(Hash("same-key"), repository.Revisions[0].IdempotencyKeyHash);
        Assert.Equal(0, artifacts.WriteCalls);
        using var read = await client.GetAsync(repository.Path + "/telemetry");
        Assert.Equal(Quote(id), read.Headers.ETag?.Tag);
        var view = await read.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("OPERATOR_ESTIMATE", view.GetProperty("status").GetString());
        Assert.Equal("MISSING_TELEMETRY", view.GetProperty("measuredTelemetryStatus").GetString());
        Assert.Equal(1, view.GetProperty("revisions").GetArrayLength());
        Assert.Equal(10m, view.GetProperty("intervals")[0].GetProperty("latitude").GetDecimal());
    }

    [Fact]
    public async Task External_srt_writes_verified_private_bytes_and_reads_normalized_interval()
    {
        var repository = new TelemetryRepository();
        var artifacts = new ArtifactStore();
        await using var factory = Host(repository, artifacts);
        using var client = Client(factory);
        await Login(client, UserRoleCode.DroneOperator);
        const string srt = "1\n00:00:00,000 --> 00:00:01,000\nGPS (106.25, 10.5)\n";
        using var response = await Send(client, repository, new { kind = "EXTERNAL_SRT", srtText = srt, offsetMs = 500 }, "external", Quote(Guid.Empty));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var revision = Assert.Single(repository.Revisions);
        Assert.Equal(Encoding.UTF8.GetBytes(srt), artifacts.Bytes);
        Assert.Equal(Hash(srt), revision.OriginalSrtSha256);
        Assert.StartsWith($"anh02/real/telemetry/{repository.WorkId:D}/", revision.OriginalSrtObjectKey);
        using var read = await client.GetAsync(repository.Path + "/telemetry");
        var view = await read.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("EXTERNAL_SRT", view.GetProperty("status").GetString());
        var interval = view.GetProperty("intervals")[0];
        Assert.Equal(500, interval.GetProperty("startMs").GetInt64());
        Assert.Equal(1500, interval.GetProperty("endMs").GetInt64());
        Assert.Equal(10.5m, interval.GetProperty("latitude").GetDecimal());
        Assert.Equal(106.25m, interval.GetProperty("longitude").GetDecimal());
    }

    [Fact]
    public async Task Invalid_interval_returns_safe_problem_without_storage_or_append()
    {
        var repository = new TelemetryRepository();
        var artifacts = new ArtifactStore();
        await using var factory = Host(repository, artifacts);
        using var client = Client(factory);
        await Login(client, UserRoleCode.DroneOperator);
        using var response = await Send(client, repository,
            new { kind = "OPERATOR_ESTIMATE", estimate = new { startMs = 0, endMs = 61000, latitude = 10, longitude = 106 }, reason = "Estimate" },
            "invalid", Quote(Guid.Empty));
        await Problem(response, 422, "invalid_video_telemetry");
        Assert.Equal(0, repository.AppendCalls);
        Assert.Equal(0, artifacts.WriteCalls);
    }

    [Theory]
    [InlineData(403, "access_forbidden")]
    [InlineData(404, "not_found")]
    public async Task Repository_scope_errors_are_safe_http_problems(int status, string code)
    {
        var repository = new TelemetryRepository { ReadError = new AiRequestException(status, code) };
        await using var factory = Host(repository, new ArtifactStore());
        using var client = Client(factory);
        await Login(client, UserRoleCode.DroneOperator);
        using var response = await client.GetAsync(repository.Path + "/telemetry");
        await Problem(response, status, code);
    }

    private WebApplicationFactory<Program> Host(TelemetryRepository repository, ArtifactStore artifacts)
    {
        var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IHostedService>();
            services.RemoveAll<IAiVideoTelemetryRepository>();
            services.RemoveAll<IAnh02ArtifactStore>();
            services.AddSingleton<IAiVideoTelemetryRepository>(repository);
            services.AddSingleton<IAnh02ArtifactStore>(artifacts);
        });
        return factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("AiRealWorker:Enabled", "false");
            builder.UseSetting("AiRealWorker:ServiceTokenFile", "");
        });
    }
    private static HttpClient Client(WebApplicationFactory<Program> factory) => factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
    private async Task Login(HttpClient client, UserRoleCode role)
    {
        var user = await sql.CreateUserAsync("telemetry-http-" + Guid.NewGuid().ToString("N"), "Current1!", role);
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = user.Email, password = "Current1!" });
        response.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
    }
    private static object Estimate(string reason = "Operator reference estimate") => new
    { kind = "OPERATOR_ESTIMATE", estimate = new { startMs = 0, endMs = 1000, latitude = 10, longitude = 106 }, reason };
    private static string Quote(Guid id) => $"\"{id:D}\"";
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static async Task<HttpResponseMessage> Send(HttpClient client, TelemetryRepository repository, object body, string? key, string? match)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, repository.Path + "/telemetry-revisions") { Content = JsonContent.Create(body) };
        if (key is not null) request.Headers.TryAddWithoutValidation("Idempotency-Key", key);
        if (match is not null) request.Headers.TryAddWithoutValidation("If-Match", match);
        return await client.SendAsync(request);
    }
    private static async Task Problem(HttpResponseMessage response, int status, string code)
    {
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Null(response.Headers.ETag);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(status, body.GetProperty("status").GetInt32());
        Assert.Equal(code, body.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("correlationId").GetString()));
        Assert.DoesNotContain("exception", body.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    // Models repository outcomes only; locking, membership and immutable SQL evidence are tested in the SQL suite.
    private sealed class TelemetryRepository : IAiVideoTelemetryRepository
    {
        public Guid WorkId { get; } = Guid.NewGuid();
        public Guid ProjectId { get; } = Guid.NewGuid();
        public Guid DatasetId { get; } = Guid.NewGuid();
        public Guid AttemptId { get; } = Guid.NewGuid();
        public Guid FileId { get; } = Guid.NewGuid();
        public string Path => $"/api/v1/projects/{ProjectId}/ai-videos/{WorkId}";
        public List<AiVideoTelemetryRevision> Revisions { get; } = [];
        public int ReadCalls { get; private set; }
        public int AppendCalls { get; private set; }
        public UserRoleCode LastRole { get; private set; }
        public Guid LastDatasetId { get; private set; }
        public AiRequestException? ReadError { get; init; }
        public Task<AiVideoWorkPage> DiscoverAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid datasetId, int offset, int limit, CancellationToken ct)
        {
            Assert.Equal(ProjectId, projectId);
            LastRole = role; LastDatasetId = datasetId;
            return Task.FromResult(new AiVideoWorkPage([new(WorkId, FileId, Guid.NewGuid(), AttemptId, "INGESTED", null)], offset, limit, false));
        }
        public Task<AiVideoTelemetryScope> ReadScopeAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid workId, CancellationToken ct)
        {
            ReadCalls++; LastRole = role;
            Assert.NotEqual(Guid.Empty, actorId); Assert.Equal(ProjectId, projectId); Assert.Equal(WorkId, workId);
            if (ReadError is { } error) throw error;
            return Task.FromResult(new AiVideoTelemetryScope(WorkId, ProjectId, AttemptId, FileId, new string('a', 64), 60000, "{}", Revisions.LastOrDefault()?.Id ?? Guid.Empty, null));
        }
        public Task<IReadOnlyList<AiVideoTelemetryRevision>> ListAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid workId, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<AiVideoTelemetryRevision>>(Revisions.ToArray());
        public Task<AiVideoTelemetryRevision> AppendAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid workId, AiVideoTelemetryAppend request, CancellationToken ct)
        {
            AppendCalls++;
            Assert.Equal(UserRoleCode.DroneOperator, role); Assert.Equal(AttemptId, request.ExpectedAttemptId);
            var replay = Revisions.SingleOrDefault(x => x.ActorId == actorId && x.IdempotencyKeyHash == request.IdempotencyKeyHash);
            if (replay is not null)
            {
                if (replay.PayloadFingerprint != request.PayloadFingerprint) throw new AiRequestException(409, "idempotency_conflict");
                return Task.FromResult(replay);
            }
            var head = Revisions.LastOrDefault()?.Id;
            if ((head ?? Guid.Empty) != request.ExpectedHeadId) throw new AiRequestException(412, "telemetry_revision_stale");
            var revision = AiVideoTelemetryRevision.Create(request.RevisionId, WorkId, Revisions.Count + 1, head, actorId,
                DateTimeOffset.UtcNow, request.SourceVideoHash, request.Kind, request.IntervalsJson, 60000,
                request.OriginalSrtObjectKey, request.OriginalSrtSha256, request.OriginalSrtBytes, request.OffsetMs,
                request.Note, request.IdempotencyKeyHash, request.PayloadFingerprint);
            Revisions.Add(revision);
            return Task.FromResult(revision);
        }
    }
    private sealed class ArtifactStore : IAnh02ArtifactStore
    {
        public int WriteCalls { get; private set; }
        public byte[]? Bytes { get; private set; }
        public async Task<Anh02ArtifactMetadata> WriteAsync(string key, Stream content, long? sizeBytes, string mediaType, CancellationToken cancellationToken = default)
        {
            WriteCalls++;
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            Bytes = buffer.ToArray();
            Assert.Equal(sizeBytes, Bytes.LongLength);
            return new(key, Bytes.LongLength, Convert.ToHexString(SHA256.HashData(Bytes)).ToLowerInvariant(), mediaType);
        }
        public Task<Anh02ArtifactRead> OpenReadAsync(string key, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("These HTTP fixtures contain no embedded or paired subtitle artifact.");
    }
}
