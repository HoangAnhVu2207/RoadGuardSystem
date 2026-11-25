using System.Text;
using FluentAssertions;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Processing.Worker;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Services.Processing.Worker;
using Xunit;

namespace RoadGuardSystem.UnitTests.Processing;

public sealed class AiVideoTelemetryServiceTests
{
    private const string Cue = "1\n00:00:00,000 --> 00:00:01,000\nGPS(106.5, 10.5, 0)\n";
    [Fact]
    public async Task MalformedSupplementNeverWritesArtifactOrRevision()
    {
        var repo = new Repo(); var store = new Store();
        var call = () => new AiVideoTelemetryService(repo, store).AppendAsync(Guid.NewGuid(), UserRoleCode.DroneOperator, repo.Scope.ProjectId, repo.Scope.WorkId,
            new("EXTERNAL_SRT", "not SRT", 0, null, null, null), "test-key", Guid.Empty, default);
        (await call.Should().ThrowAsync<AiRequestException>()).Which.Status.Should().Be(422);
        store.Writes.Should().Be(0); repo.Appends.Should().Be(0);
    }
    [Fact]
    public async Task ValidEmbeddedTelemetryRejectsInitialFallbackBeforeWrite()
    {
        var bytes = Encoding.UTF8.GetBytes(Cue); var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
        var repo = new Repo(); var store = new Store { Bytes = bytes };
        repo.Scope = repo.Scope with { EmbeddedSrt = new(Guid.NewGuid(), Guid.NewGuid(), "private/embedded.srt", hash, bytes.Length, "application/x-subrip") };
        var call = () => new AiVideoTelemetryService(repo, store).AppendAsync(Guid.NewGuid(), UserRoleCode.DroneOperator, repo.Scope.ProjectId, repo.Scope.WorkId,
            new("EXTERNAL_SRT", Cue, 0, null, null, null), "test-key", Guid.Empty, default);
        (await call.Should().ThrowAsync<AiRequestException>()).Which.Status.Should().Be(409);
        store.Writes.Should().Be(0); repo.Appends.Should().Be(0);
    }
    [Fact]
    public async Task VerifiedPairedSrtWinsOverEmbeddedAndRetainsItsOffset()
    {
        var bytes = Encoding.UTF8.GetBytes(Cue); var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
        var repo = new Repo(); var store = new Store { Bytes = bytes };
        repo.Scope = repo.Scope with
        {
            PairedSrt = new(new(Guid.Empty, Guid.NewGuid(), "paired.srt", hash, bytes.Length, "application/x-subrip"), 500),
            EmbeddedSrt = new(Guid.NewGuid(), Guid.NewGuid(), "embedded.srt", hash, bytes.Length, "application/x-subrip")
        };
        var uploads = new Uploaded(bytes);
        var view = await new AiVideoTelemetryService(repo, store, uploads).GetAsync(Guid.NewGuid(), UserRoleCode.ProjectManager, repo.Scope.ProjectId, repo.Scope.WorkId, default);
        view.Status.Should().Be("PAIRED_SRT"); view.MeasuredTelemetryStatus.Should().Be("PAIRED_SRT");
        view.Intervals.Should().ContainSingle().Which.StartMs.Should().Be(500);
        uploads.Keys.Should().Equal("paired.srt"); store.Keys.Should().BeEmpty();
    }
    [Fact]
    public async Task EstimateRetainsExplicitMissingMeasuredTelemetryStatus()
    {
        var repo = new Repo(); var id = Guid.NewGuid(); var actor = Guid.NewGuid();
        repo.Scope = repo.Scope with { HeadRevisionId = id };
        repo.Revisions = [AiVideoTelemetryRevision.Create(id, repo.Scope.WorkId, 1, null, actor, DateTimeOffset.UtcNow,
            repo.Scope.SourceVideoHash, "OPERATOR_ESTIMATE", "[{\"startMs\":0,\"endMs\":1000,\"latitude\":10,\"longitude\":106}]", 2000,
            null, null, null, 0, "operator estimate", new string('b',64), new string('c',64))];
        var view = await new AiVideoTelemetryService(repo, new Store()).GetAsync(actor, UserRoleCode.ProjectManager, repo.Scope.ProjectId, repo.Scope.WorkId, default);
        view.Status.Should().Be("OPERATOR_ESTIMATE"); view.MeasuredTelemetryStatus.Should().Be("MISSING_TELEMETRY");
        view.PositionMeaning.Should().Be("DRONE_REFERENCE_NOT_GROUND_LOCATION");
    }
    [Fact]
    public async Task ManagersCannotAppendEvenWithAnInjectedPermissiveRepository()
    {
        var repo = new Repo(); var store = new Store();
        var call = () => new AiVideoTelemetryService(repo, store).AppendAsync(Guid.NewGuid(), UserRoleCode.ProjectManager, Guid.NewGuid(), Guid.NewGuid(),
            new("EXTERNAL_SRT", Cue, 0, null, null, null), "test", Guid.Empty, default);
        (await call.Should().ThrowAsync<AiRequestException>()).Which.Status.Should().Be(403);
        repo.Reads.Should().Be(0);
    }
    private sealed class Repo : IAiVideoTelemetryRepository
    {
        public AiVideoTelemetryScope Scope { get; set; } = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new string('a', 64), 2000, "{}", Guid.Empty, null);
        public int Reads { get; private set; }
        public int Appends { get; private set; }
        public IReadOnlyList<AiVideoTelemetryRevision> Revisions { get; set; } = [];
        public Task<AiVideoWorkPage> DiscoverAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid datasetId, int offset, int limit, CancellationToken ct) => throw new NotSupportedException();
        public Task<AiVideoTelemetryScope> ReadScopeAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid workId, CancellationToken ct) { Reads++; return Task.FromResult(Scope); }
        public Task<IReadOnlyList<AiVideoTelemetryRevision>> ListAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid workId, CancellationToken ct) => Task.FromResult(Revisions);
        public Task<AiVideoTelemetryRevision> AppendAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid workId, AiVideoTelemetryAppend request, CancellationToken ct) { Appends++; throw new InvalidOperationException("Unexpected append"); }
    }
    private sealed class Store : IAnh02ArtifactStore
    {
        public byte[] Bytes { get; init; } = [];
        public int Writes { get; private set; }
        public List<string> Keys { get; } = [];
        public Task<Anh02ArtifactMetadata> WriteAsync(string key, Stream content, long? sizeBytes, string mediaType, CancellationToken cancellationToken = default) { Writes++; throw new InvalidOperationException("Unexpected write"); }
        public Task<Anh02ArtifactRead> OpenReadAsync(string key, CancellationToken cancellationToken = default)
        { Keys.Add(key); return Task.FromResult(new Anh02ArtifactRead(new MemoryStream(Bytes), new(key, Bytes.Length, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Bytes)).ToLowerInvariant(), "application/x-subrip"))); }
    }
    private sealed class Uploaded(byte[] bytes) : IUploadObjectStorage
    {
        public List<string> Keys { get; } = [];
        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default) { Keys.Add(objectKey); return Task.FromResult<Stream>(new MemoryStream(bytes)); }
        public Task<string> InitiateAsync(string key, string mediaType, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(string key, string uploadId, IReadOnlyList<int> numbers, DateTimeOffset expires, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<UploadObjectVerification> CompleteAndVerifyAsync(string key, string uploadId, IReadOnlyList<CompletedStoragePart> parts, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
