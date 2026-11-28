using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Processing.Worker;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.DTOs.Exports;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.Services.Integration;
using RoadGuardSystem.Services.Exports;
using RoadGuardSystem.Services.Processing.Worker;
using Xunit;

namespace RoadGuardSystem.ApiTests.Reporting;

// REAL schema/lineage fixture; no inference engine is replaced or invoked. SQL and MinIO are owned by this test.
public sealed partial class RealApprovedTrainingExportHttpTests(AuthenticationSqlServerFixture sql) : IClassFixture<AuthenticationSqlServerFixture>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly byte[] FrameBytes = Convert.FromBase64String("/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAABAAEDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDx+iiigD//2Q==");
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RealApprovedSnapshotExportsEditedAnnotationAndFrozenProvenanceWithCurrentAuthority(bool originals)
    {
        await using var minio = await OwnedWebMinio.StartAsync(createBucket: true);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, allowExternalStorage: true, configureTestServices: minio.Configure);
        var source = await Seed(factory);
        using var pm = factory.CreateClient();
        using var login = await pm.PostAsJsonAsync("/api/v1/auth/login", new { email = source.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        pm.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        var root = $"/api/v1/projects/{source.Project}";
        AiCandidateFactsV1 proof;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var reader = scope.ServiceProvider.GetRequiredService<IAiCandidateFactsReader>();
            Assert.IsType<CompositeAiCandidateFactsReader>(reader);
            var resolved = await reader.ResolveAsync(source.Pm, UserRoleCode.ProjectManager, source.Project, source.Detection);
            Assert.Equal(AnhHuyProducerStatus.Ready, resolved.Status); proof = resolved.Facts!; Assert.Equal("REAL", proof.Mode);
        }
        var created = await Post(pm, root + "/labels", new
        {
            sourceKind = "AI_DETECTION",
            sourceId = source.Detection,
            sourceVersion = proof.SourceVersion,
            fileId = source.Frame,
            annotation = new { kind = "BBOX", coordinateSpace = "NORMALIZED", x = .2m, y = .3m, width = .4m, height = .5m },
            defectTypeCode = source.ApprovedType,
            reason = "Edited approved class/box differs from the detection"
        });
        var label = await Body(created, 201); var labelId = label.GetProperty("id").GetGuid();
        var exportInput = new { kind = "TRAINING", format = "ZIP", includeOriginalFiles = originals };
        Assert.Equal("no_eligible_labels", (await Body(await Post(pm, root + "/exports", exportInput), 422)).GetProperty("code").GetString());
        await Body(await Post(pm, $"/api/v1/labels/{labelId}/review", new { decision = "APPROVE", reason = "Approved revision for training" }, version: label.GetProperty("version").GetString()), 200);
        var key = Guid.NewGuid().ToString();
        var admitted = await Body(await Post(pm, root + "/exports", exportInput, key), 202); var job = admitted.GetProperty("id").GetGuid();
        Assert.Equal(job, (await Body(await Post(pm, root + "/exports", exportInput, key), 202)).GetProperty("id").GetGuid());
        var path = root + "/exports/" + job;
        var captured = await pm.GetFromJsonAsync<ExportManifestV1Dto>(path + "/manifest");
        var exported = Assert.Single(captured!.Labels!);
        Assert.Equal("REAL", exported.Mode); Assert.Equal(source.ApprovedType, exported.TypeCode); Assert.NotEqual(proof.TypeCode, exported.TypeCode);
        Assert.Equal(.2m, exported.X); Assert.Equal(.3m, exported.Y); Assert.Equal(.4m, exported.Width); Assert.Equal(.5m, exported.Height);
        Assert.Equal(source.Frame, exported.FileId); Assert.Equal(Hash(FrameBytes), exported.Sha256);
        Assert.Equal(proof.AttemptId, exported.AiSource!.AttemptId); Assert.Equal(1, exported.AiSource.FrameWidth); Assert.Equal(1, exported.AiSource.FrameHeight);
        Assert.Equal(proof.ResultId, exported.AiSource.ResultId); Assert.Equal(proof.ManifestHash, exported.AiSource.ManifestHash);
        Assert.Equal(proof.SourceVideoFileId, exported.AiSource.SourceVideoFileId);
        Assert.Equal(source.Frame, Assert.Single(captured.Files).FileId); Assert.Equal(originals, captured.Files[0].Included);
        Assert.DoesNotContain(captured.Files, x => x.FileId == proof.SourceVideoFileId);
        await using (var scope = factory.Services.CreateAsyncScope()) Assert.True(await scope.ServiceProvider.GetRequiredService<IExportService>().ProcessNextAsync(default));
        var state = await Body(await pm.GetAsync(path)); Assert.Equal("SUCCEEDED", state.GetProperty("status").GetString());
        var bytes = await pm.GetByteArrayAsync(path + "/content");
        using (var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read))
        {
            using var text = new StreamReader(archive.GetEntry("labels.jsonl")!.Open());
            var row = JsonSerializer.Deserialize<ExportLabelDto>((await text.ReadToEndAsync()).Trim(), Json);
            Assert.Equal(exported, row);
            if (originals)
            {
                using var original = archive.GetEntry(captured.Files[0].ArchivePath!)!.Open(); using var memory = new MemoryStream();
                await original.CopyToAsync(memory); Assert.Equal(FrameBytes, memory.ToArray());
            }
            else Assert.DoesNotContain(archive.Entries, e => e.Name.EndsWith(".jpg", StringComparison.Ordinal));
        }
        Assert.Equal("export_ai_segment_scope_unresolved", (await Body(await Post(pm, root + "/exports",
            new { kind = "TRAINING", format = "ZIP", segmentIds = new[] { Guid.NewGuid() } }), 422)).GetProperty("code").GetString());
        // A physically missing frame fails rendering; never mutate immutable file metadata to arrange the test.
        var missingJob = await Body(await Post(pm, root + "/exports", new { kind = "TRAINING", format = "ZIP", includeOriginalFiles = true }), 202);
        await using (var readDb = sql.CreateDbContext())
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var frameKey = await readDb.Files.Where(x => x.Id == source.Frame).Select(x => x.StorageUri).SingleAsync();
            var options = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RoadGuardSystem.Repositories.Options.MinioStorageOptions>>().Value;
            using var storage = new Amazon.S3.AmazonS3Client(options.AccessKey, options.SecretKey,
                new Amazon.S3.AmazonS3Config { ServiceURL = options.Endpoint, ForcePathStyle = true, AuthenticationRegion = "us-east-1" });
            await storage.DeleteObjectAsync(options.BucketName, frameKey);
            Assert.True(await scope.ServiceProvider.GetRequiredService<IExportService>().ProcessNextAsync(default));
            var failed = await Body(await pm.GetAsync(root + "/exports/" + missingJob.GetProperty("id").GetGuid()));
            Assert.Equal("FAILED", failed.GetProperty("status").GetString());
            Assert.Equal("export_source_unavailable", failed.GetProperty("errorCode").GetString());
            using var frameContent = new MemoryStream(FrameBytes);
            await scope.ServiceProvider.GetRequiredService<IAnh02ArtifactStore>().WriteAsync(frameKey, frameContent, FrameBytes.Length, "image/jpeg");
        }
        Assert.Equal(bytes, await pm.GetByteArrayAsync(path + "/content"));
        var pending = await Body(await Post(pm, root + "/exports", exportInput), 202);
        var pendingId = pending.GetProperty("id").GetGuid();
        var currentLabel = await pm.GetFromJsonAsync<JsonElement>(root + "/labels/" + labelId);
        await Body(await Post(pm, root + $"/labels/{labelId}/revisions", new
        {
            fileId = source.Frame,
            annotation = new { kind = "BBOX", coordinateSpace = "NORMALIZED", x = .1m, y = .1m, width = .1m, height = .1m },
            defectTypeCode = source.ApprovedType,
            reason = "New unapproved revision after frozen export"
        }, version: currentLabel.GetProperty("version").GetString()), 201);
        Assert.Equal("no_eligible_labels", (await Body(await Post(pm, root + "/exports", exportInput), 422)).GetProperty("code").GetString());
        var unapproved = await pm.GetFromJsonAsync<JsonElement>(root + "/labels/" + labelId);
        await Body(await Post(pm, $"/api/v1/labels/{labelId}/review", new { decision = "REJECT", reason = "Reject new revision, preserve old archive" }, version: unapproved.GetProperty("version").GetString()), 200);
        Assert.Equal("no_eligible_labels", (await Body(await Post(pm, root + "/exports", exportInput), 422)).GetProperty("code").GetString());
        Assert.Equal(bytes, await pm.GetByteArrayAsync(path + "/content"));
        Assert.Equal(exported, Assert.Single((await pm.GetFromJsonAsync<ExportManifestV1Dto>(path + "/manifest"))!.Labels!));
        var timeline = await Body(await pm.GetAsync(root + $"/reports/timeline?aggregateType=Project&aggregateId={source.Project}"));
        var terminal = Assert.Single(timeline.GetProperty("items").EnumerateArray().Where(x => x.GetProperty("action").GetString() == "AI_ATTEMPT.Ingested"));
        Assert.Equal("SYSTEM", terminal.GetProperty("actorKind").GetString()); Assert.Equal(JsonValueKind.Null, terminal.GetProperty("actorId").ValueKind);
        await using var db = sql.CreateDbContext();
        Assert.Equal(1, await db.Set<RoadGuardSystem.BusinessObjects.Exports.ExportJob>().CountAsync(x => x.Id == job));
        var member = await db.ProjectMembers.SingleAsync(x => x.ProjectId == source.Project && x.UserId == source.Pm);
        member.Status = ProjectMemberStatus.Ended; await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await pm.GetAsync(path + "/content")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await pm.GetAsync(path + "/manifest")).StatusCode);
        await using (var scope = factory.Services.CreateAsyncScope()) Assert.True(await scope.ServiceProvider.GetRequiredService<IExportService>().ProcessNextAsync(default));
        var rejectedJob = await db.Set<RoadGuardSystem.BusinessObjects.Exports.ExportJob>().AsNoTracking().SingleAsync(x => x.Id == pendingId);
        Assert.Equal("FAILED", rejectedJob.Status); Assert.Equal("export_authority_revoked", rejectedJob.ErrorCode);
        if (Environment.GetEnvironmentVariable("RG_SCOPE_HANDOFF_EVIDENCE") is { } directory)
        {
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(Path.Combine(directory, $"real-training-{originals}.json"), JsonSerializer.Serialize(new { admitted, manifest = captured, state, timeline }, Json));
        }
    }
    private static Task<HttpResponseMessage> Post(HttpClient client, string path, object body, string? key = null, string? version = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString()); if (version is not null) request.Headers.Add("If-Match", '"' + version + '"'); return client.SendAsync(request);
    }
    private static async Task<JsonElement> Body(HttpResponseMessage response, int status = 200)
    {
        var raw = await response.Content.ReadAsStringAsync(); Assert.True((int)response.StatusCode == status, $"Expected {status}, got {(int)response.StatusCode}: {raw}"); return JsonDocument.Parse(raw).RootElement.Clone();
    }
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
