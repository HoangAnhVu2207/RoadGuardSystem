using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Processing;
using RoadGuardSystem.aBusinessObjects.Commons;
using System.Text;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.Services.Processing.Anh02;
using Xunit;

namespace RoadGuardSystem.UnitTests.Processing;

public sealed class AiFailureDiagnosticTests
{
    [Theory]
    [InlineData(false, "fixture_missing")]
    [InlineData(true, "fixture_hash_mismatch")]
    public void MissingOrUnapprovedFrameIsRejectedWithoutLeakingItsPath(bool writeInvalid, string reason)
    {
        var directory = Path.Combine(Path.GetTempPath(), "rg-ai-diagnostic-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            if (writeInvalid) File.WriteAllBytes(Path.Combine(directory, "synthetic-road-v1-frame-1.png"), [1, 2, 3]);
            var fixture = new SyntheticAiFixture(Options.Create(new Anh02AiOptions { FixtureDirectory = directory }));
            var error = Assert.Throws<AiRequestException>(() => fixture.Frame());
            Assert.Equal(422, error.Status);
            Assert.Equal("ai_contract_invalid", error.Code);
            var correlation = Guid.NewGuid();
            var detail = AiFailureDiagnostics.Describe(error, correlation);
            Assert.Equal(reason, detail.ReasonCode);
            Assert.Equal("mock_materialization", detail.Step);
            Assert.Equal("fixtureVersion", detail.Field);
            Assert.Equal(correlation, detail.OriginCorrelationId);
            Assert.DoesNotContain(directory, detail.Recovery, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(directory, error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData("fixture_version_unsupported")]
    [InlineData("source_lineage_invalid")]
    [InlineData("source_hash_mismatch")]
    [InlineData("manifest_hash_mismatch")]
    [InlineData("result_schema_invalid")]
    [InlineData("result_hash_mismatch")]
    [InlineData("artifact_integrity_mismatch")]
    public void ClientRecoveryDoesNotSerializeTheUnderlyingException(string reason)
    {
        var error = new AiRequestException(422, "ai_contract_invalid", reason, "result_validation", "datasetId",
            new IOException("private/path/fixture with sensitive raw data"));
        var diagnostic = AiFailureDiagnostics.Describe(error);
        Assert.Equal(reason, diagnostic.ReasonCode);
        Assert.Equal("datasetId", diagnostic.Field);
        Assert.Null(diagnostic.OriginCorrelationId);
        Assert.DoesNotContain("private/path", diagnostic.Recovery, StringComparison.Ordinal);
        Assert.NotEmpty(diagnostic.Recovery);
    }
    [Theory]
    [InlineData("{}", false, "manifest_schema_invalid")]
    [InlineData("{", false, "manifest_schema_invalid")]
    [InlineData("{}", true, "manifest_hash_mismatch")]
    public async Task InvalidManifestTerminatesWithSafeDurableDiagnosticInsteadOfContinuing(string manifest, bool badHash, string reason)
    {
        var repository = new ClaimedManifestRepository(new AiMockRun
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            CreatedBy = Guid.NewGuid(),
            CanonicalManifest = manifest,
            ManifestHash = badHash ? "incorrect" : AiManifestCanonicalizer.Hash(Encoding.UTF8.GetBytes(manifest))
        });
        var service = new Anh02AiService(repository, null!, null!, null!, null!, null!,
            Options.Create(new Anh02AiOptions { MockEnabled = true }), new TestEnvironment(), TimeProvider.System);
        Assert.True(await service.ProcessOneAsync(default));
        var diagnostic = Assert.IsType<AiFailureDiagnosticFact>(repository.Failure);
        Assert.Equal(reason, diagnostic.ReasonCode);
        Assert.Equal("manifest_validation", diagnostic.Step);
        Assert.Equal("ai_contract_invalid", repository.Code);
        Assert.False(repository.ReadSource);
    }
    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "fixture";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
    private sealed class ClaimedManifestRepository(AiMockRun run) : IAnh02AiRepository
    {
        public AiFailureDiagnosticFact? Failure { get; private set; }
        public string? Code { get; private set; }
        public bool ReadSource { get; private set; }
        public Task AuthorizeAsync(Guid actorId, UserRoleCode role, Guid projectId, bool managerOnly, CancellationToken ct) => Task.CompletedTask;
        public Task<AiMockRun?> ClaimAsync(Guid owner, DateTimeOffset now, CancellationToken ct) => Task.FromResult<AiMockRun?>(run);
        public Task FailAsync(Guid runId, Guid owner, string code, CancellationToken ct) => throw new NotSupportedException();
        public Task FailAsync(Guid runId, Guid owner, string code, AiFailureDiagnosticFact diagnostic, CancellationToken ct)
        { Assert.Equal(run.Id, runId); Code = code; Failure = diagnostic; return Task.CompletedTask; }
        public Task<AiDatasetSourceFacts> ReadSourcesAsync(Guid projectId, CreateAiMockRunRequestFact request, CancellationToken ct)
        { ReadSource = true; throw new NotSupportedException(); }
        public Task<AiMockRunViewFact> AdmitAsync(Guid actorId, UserRoleCode role, Guid projectId, CreateAiMockRunRequestFact request, string key, string fingerprint, Func<Guid, Guid, Guid, AiDatasetSourceFacts, CancellationToken, Task<AiAdmissionManifest>> createManifest, CancellationToken ct) => throw new NotSupportedException();
        public Task<AiMockRunViewFact> ReadViewAsync(Guid projectId, Guid runId, CancellationToken ct) => throw new NotSupportedException();
        public Task<AiMockRun?> GetAsync(Guid projectId, Guid runId, CancellationToken ct) => throw new NotSupportedException();
        public Task<AiResultProvenance?> GetResultAsync(Guid runId, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> CompleteAsync(Guid runId, Guid owner, AiCompletion completion, Func<CancellationToken, Task<bool>> recheckGeometry, CancellationToken ct) => throw new NotSupportedException();
        public Task<AiCandidateReadFacts?> ReadCandidateAsync(Guid projectId, Guid detectionId, CancellationToken ct) => throw new NotSupportedException();
        public Task<Guid?> DetectionProjectAsync(Guid detectionId, CancellationToken ct) => throw new NotSupportedException();
    }

}
