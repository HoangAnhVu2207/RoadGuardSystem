using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Security.Cryptography;
using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Exports;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Services.Integration;
using RoadGuardSystem.Services.Exports;
using Xunit;

namespace RoadGuardSystem.ApiTests.Reports;

public sealed partial class Huy01ReporterReportsApiTests
{
    [Theory]
    [InlineData("unavailable", 503)] [InlineData("empty", 422)]
    [InlineData("foreign", 422)] [InlineData("duplicate-head", 422)]
    [InlineData("null-annotation", 422)] [InlineData("denied", 403)]
    [InlineData("approved-fixture", 202)]
    public async Task Anh02_training_consumer_observes_typed_contract_without_claiming_Huy_approval(string shape, int expected)
    {
        var reporter = await sql.CreateUserAsync($"label-source-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var manager = await sql.CreateUserAsync($"label-reader-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var project = Guid.NewGuid();
        await using (var db = sql.CreateDbContext())
        {
            db.Projects.Add(Project.Create(project, project.ToString(), "Typed label fixture", null, null, null, null, DateTimeOffset.UtcNow));
            db.ProjectMembers.Add(ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project, manager.Id, new DateOnly(2000, 1, 1)));
            await db.SaveChangesAsync();
        }
        var state = new ApprovedReaderFixtureState { Actor = manager.Id, Project = project, Allowed = shape != "denied" };
        var artifacts = new ConsumerArtifactFixture();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(new VerifiedPhotoStorage());
            services.RemoveAll<IAnh02ArtifactStore>(); services.AddSingleton<IAnh02ArtifactStore>(artifacts);
            // Explicit contract fixtures, NEVER registered in production composition root.
            services.AddScoped<IApprovedTrainingLabelReader>(sp => new ApprovedReaderContractFixture(state, sp.GetRequiredService<RoadGuardDbContext>()));
            services.AddScoped<ITrainingSourceAccessReader>(sp => new ApprovedSourceAccessContractFixture(state));
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginAsync(client, reporter.UserName!); var file = await UploadVerifiedAsync(client, factory); state.File = file.FileId;
        var reportResponse = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", new { description = "Label source fixture", evidence = new[] { new { fileId = file.FileId, fileVersion = file.Version, locationSource = "UNKNOWN" } } }, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Created, reportResponse.StatusCode);
        var report = (await reportResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var label = new ApprovedTrainingLabelV1(Guid.NewGuid(), 2, Guid.NewGuid(), project, "POTHOLE", new(.1m,.2m,.3m,.4m), file.FileId, file.Version,
            Convert.ToHexString(SHA256.HashData([1,2,3,4])).ToLowerInvariant(), 4, "image/jpeg", "REPORT", report, "fixture-source-v1", Guid.NewGuid(), manager.Id, DateTimeOffset.UtcNow, null, null, null, "REAL", null);
        var labels = shape switch
        {
            "empty" => Array.Empty<ApprovedTrainingLabelV1>(),
            "foreign" => [label with { ProjectId = Guid.NewGuid() }],
            "duplicate-head" => [label, label with { Revision = 3, RevisionId = Guid.NewGuid() }],
            "null-annotation" => [label with { Annotation = null! }],
            _ => new[] { label }
        };
        state.Snapshot = shape == "unavailable" ? null : new("anh02.label-contract-fixture.v1", Guid.NewGuid(), new string('a',64), DateTimeOffset.UtcNow, labels);
        await LoginAsync(client, manager.UserName!);
        var path = $"/api/v1/projects/{project}/exports"; var key = Guid.NewGuid().ToString();
        var request = new { kind = "TRAINING", format = "ZIP", includeOriginalFiles = true };
        var response = await SendAsync(client, HttpMethod.Post, path, request, key);
        Assert.Equal(expected, (int)response.StatusCode); Assert.Equal(1, state.Captures);
        if (expected != 202)
        {
            await using var db = sql.CreateDbContext(); Assert.Empty(await db.Set<ExportJob>().Where(j => j.ProjectId == project).ToArrayAsync());
            Assert.Equal(shape switch { "unavailable" => "producer_unavailable", "empty" => "no_eligible_labels", "denied" => "forbidden", _ => "producer_invalid" }, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
            return;
        }
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        // A later fixture head is not allowed to rewrite an admitted historical snapshot.
        state.Snapshot = state.Snapshot! with { Labels = [label with { Revision = 3, RevisionId = Guid.NewGuid() }] };
        var replay = await SendAsync(client, HttpMethod.Post, path, request, key);
        Assert.Equal(HttpStatusCode.Accepted, replay.StatusCode); Assert.Equal(id, (await replay.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid()); Assert.Equal(1, state.Captures);
        for (var attempt = 0; attempt < 20; attempt++)
        {
            using var scope = factory.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IExportService>();
            if ((await service.GetAsync(manager.Id, project, id, default)).Value!.Status is "SUCCEEDED" or "FAILED") break;
            Assert.True(await service.ProcessNextAsync(default));
        }
        var content = await client.GetAsync(path + $"/{id}/content"); Assert.Equal(HttpStatusCode.OK, content.StatusCode);
        using var zip = new ZipArchive(new MemoryStream(await content.Content.ReadAsByteArrayAsync()));
        using var reader = new StreamReader(zip.GetEntry("labels.jsonl")!.Open());
        var storedLabel = JsonSerializer.Deserialize<JsonElement>((await reader.ReadToEndAsync()).Trim()); Assert.Equal(2, storedLabel.GetProperty("revision").GetInt32());
        using var photo = zip.GetEntry("images/" + file.FileId.ToString("D") + ".jpg")?.Open();
        Assert.NotNull(photo);
        await using (var db = sql.CreateDbContext()) await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Users SET MustChangePassword=1 WHERE Id={manager.Id}");
        using (var scope = factory.Services.CreateScope())
        {
            var direct = await scope.ServiceProvider.GetRequiredService<IExportService>().ContentAsync(manager.Id, project, id, default);
            Assert.Equal("forbidden", direct.Code); // Internal/worker callers cannot rely on JWT preflight.
        }
        await using (var db = sql.CreateDbContext()) await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Users SET MustChangePassword=0 WHERE Id={manager.Id}");
        state.Allowed = false;
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(path + $"/{id}/content")).StatusCode);
        Assert.True(state.AccessChecks >= 4); Assert.Equal(1, artifacts.Writes);
    }

    private sealed class ApprovedReaderFixtureState
    {
        public Guid Actor { get; init; } public Guid Project { get; init; } public Guid File { get; set; }
        public bool Allowed { get; set; } public int Captures { get; set; } public int AccessChecks { get; set; }
        public ApprovedLabelSnapshotV1? Snapshot { get; set; }
    }
    private sealed class ApprovedReaderContractFixture(ApprovedReaderFixtureState state, RoadGuardDbContext db) : IApprovedTrainingLabelReader
    {
        public Task<ApprovedLabelSnapshotV1?> CaptureApprovedAsync(Guid actorId, UserRoleCode role, Guid projectId, TrainingLabelFilterV1 filters, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested(); Assert.Equal(state.Actor, actorId); Assert.Equal(state.Project, projectId); Assert.Equal(UserRoleCode.ProjectManager, role);
            Assert.NotNull(db.Database.CurrentTransaction); Assert.Equal(System.Data.IsolationLevel.Serializable, db.Database.CurrentTransaction.GetDbTransaction().IsolationLevel);
            state.Captures++; return Task.FromResult(state.Snapshot);
        }
    }
    private sealed class ApprovedSourceAccessContractFixture(ApprovedReaderFixtureState state) : ITrainingSourceAccessReader
    {
        public Task<bool> CanReadAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid[] fileIds, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested(); state.AccessChecks++;
            return Task.FromResult(state.Allowed && actorId == state.Actor && projectId == state.Project && fileIds.All(id => id == state.File));
        }
    }
    private sealed class ConsumerArtifactFixture : IAnh02ArtifactStore
    {
        private readonly Dictionary<string,(byte[] Bytes,string Media)> _objects = new(); public int Writes { get; private set; }
        public async Task<Anh02ArtifactMetadata> WriteAsync(string key, Stream content, long? sizeBytes, string mediaType, CancellationToken cancellationToken = default)
        {
            using var bytes = new MemoryStream(); await content.CopyToAsync(bytes, cancellationToken); var data = bytes.ToArray(); _objects[key] = (data, mediaType); Writes++;
            return Metadata(key, data, mediaType);
        }
        public Task<Anh02ArtifactRead> OpenReadAsync(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested(); if (!_objects.TryGetValue(key, out var obj)) throw new FileNotFoundException();
            return Task.FromResult(new Anh02ArtifactRead(new MemoryStream(obj.Bytes), Metadata(key, obj.Bytes, obj.Media)));
        }
        private static Anh02ArtifactMetadata Metadata(string key, byte[] bytes, string media) => new(key, bytes.LongLength, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), media);
    }
}
