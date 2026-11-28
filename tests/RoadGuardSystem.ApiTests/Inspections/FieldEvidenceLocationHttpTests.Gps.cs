using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.Services.Files;
using Xunit;

namespace RoadGuardSystem.ApiTests.Inspections;

public sealed partial class FieldEvidenceLocationHttpTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task IndependentCaptureGpsSurvivesRealUploadReadbackReplayAndValidation()
    {
        var pm = await sql.CreateUserAsync($"photo-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var crew = await sql.CreateUserAsync($"photo-crew-{Guid.NewGuid():N}", "Current1!", UserRoleCode.RepairCrew);
        var other = await sql.CreateUserAsync($"photo-other-{Guid.NewGuid():N}", "Current1!", UserRoleCode.RepairCrew);
        var reporter = await sql.CreateUserAsync($"photo-reporter-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var supervisor = await sql.CreateUserAsync($"photo-sup-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var source = await H3FieldInspectionWorkflowHttpTests.Seed(sql, pm.Id, crew.Id, other.Id, reporter.Id);
        await using var minio = await OwnedWebMinio.StartAsync(createBucket: true);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, allowExternalStorage: true, configureTestServices: minio.Configure);
        using var client = factory.CreateClient();
        await Login(client, pm.UserName!);
        var root = $"/api/v1/projects/{source.Project}/field-inspection-tasks";
        using var created = await Post(client, root, new
        {
            defectId = source.Defect,
            defectVersion = source.Version,
            sourceKind = "REPORTER",
            routeVersionId = source.Route,
            segmentSetId = source.Set,
            purpose = "PRE_MEASUREMENT",
            requiredMeasurementType = 4,
            measurementScope = "{}",
            assignedToUserId = crew.Id,
            dueAt = DateTimeOffset.UtcNow.AddDays(1)
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var task = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var path = root + "/" + task;
        await Login(client, crew.UserName!);
        using var accepted = await Post(client, path + "/accept", new { reason = "Accept" }, version: await Version(client, path));
        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        using var started = await Post(client, path + "/start", new { originId = Guid.NewGuid(), claimedAt = DateTimeOffset.UtcNow }, version: await Version(client, path));
        Assert.Equal(HttpStatusCode.Created, started.StatusCode);
        var start = (await started.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        byte[] photo = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+j1ioAAAAASUVORK5CYII=");
        var hash = Convert.ToHexString(SHA256.HashData(photo)).ToLowerInvariant();
        var declarations = new List<FieldEvidenceDeclaration>();
        var captured = DateTimeOffset.UtcNow;
        var points = new FieldCaptureCoordinates?[] { new(106.1, 10.1, 0, "CAPTURE", captured), new(107.2, 11.2, null, "EXIF", captured), null };
        foreach (var point in points)
        {
            using var upload = await Post(client, "/api/v1/uploads", new { projectId = source.Project, targetId = task, purpose = "MEASUREMENT", fileName = "gps.png", mediaType = "image/png", sizeBytes = photo.Length, checksumSha256 = hash });
            Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
            var uploaded = await upload.Content.ReadFromJsonAsync<JsonElement>();
            var uploadId = uploaded.GetProperty("id").GetGuid(); var file = uploaded.GetProperty("fileId").GetGuid();
            using var urls = await Post(client, $"/api/v1/uploads/{uploadId}/part-urls", new { partNumbers = new[] { 1 } });
            Assert.Equal(HttpStatusCode.OK, urls.StatusCode);
            var url = (await urls.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("parts")[0].GetProperty("url").GetString();
            using var http = new HttpClient(); using var bytes = new ByteArrayContent(photo);
            using var put = await http.PutAsync(url, bytes); Assert.Equal(HttpStatusCode.OK, put.StatusCode);
            using var state = await client.GetAsync($"/api/v1/uploads/{uploadId}");
            using var complete = await Post(client, $"/api/v1/uploads/{uploadId}/complete", new { checksumSha256 = hash, parts = new[] { new { partNumber = 1, eTag = put.Headers.ETag!.Tag } } }, version: state.Headers.ETag!.Tag.Trim('"'));
            Assert.Equal(HttpStatusCode.Accepted, complete.StatusCode);
            await using (var services = factory.Services.CreateAsyncScope())
                await services.ServiceProvider.GetRequiredService<IUploadService>().ProcessOneVerificationAsync();
            var verified = await client.GetFromJsonAsync<JsonElement>($"/api/v1/files/{file}");
            Assert.Equal("VERIFIED", verified.GetProperty("status").GetString());
            declarations.Add(new(Guid.NewGuid(), file, "MEASUREMENT", hash, "image/png", captured, null, photo.Length, point));
        }
        var input = new FieldSubmissionInput(Guid.NewGuid(), start, null, [], declarations.ToArray(),
            new("GPS_CAPTURE", null, 108.3, 12.3), "MEASUREMENT", null, null, Guid.NewGuid());
        var version = await Version(client, path); var key = Guid.NewGuid().ToString();
        using var submitted = await Post(client, path + "/submissions", input, key, version);
        Assert.Equal(HttpStatusCode.Created, submitted.StatusCode);
        var body = await submitted.Content.ReadFromJsonAsync<JsonElement>(); var id = body.GetProperty("id").GetGuid();
        Assert.Contains(body.GetProperty("missingReasons").EnumerateArray(), x => x.GetString() == "GPS_SOURCE_BINDING_UNAVAILABLE");
        using var fresh = factory.CreateClient(); await Login(fresh, crew.UserName!);
        using var readResponse = await fresh.GetAsync(path + "/submissions/" + id);
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        var raw = await readResponse.Content.ReadAsStringAsync();
        var read = JsonSerializer.Deserialize<FieldSubmissionView>(raw, Json);
        if (Environment.GetEnvironmentVariable("RG_FIELD_GPS_RESPONSE") is { } responseFile)
            await File.WriteAllTextAsync(responseFile, raw);
        Assert.NotNull(read); Assert.Equal(108.3, read.LocationProof!.Longitude);
        Assert.Equal(points, read.Evidence.Select(x => x.Coordinates));
        Assert.Equal(3, read.EvidenceReferences!.Length);
        for (var i = 0; i < 3; i++)
        {
            var reference = read.EvidenceReferences[i];
            Assert.Equal(declarations[i].CaptureOriginId, reference.CaptureOriginId);
            Assert.Equal(declarations[i].FileId, reference.FileId); Assert.Equal(hash, reference.ChecksumSha256);
            Assert.Equal(crew.Id, reference.OriginalActorId); Assert.Equal(crew.Id, reference.ActualUploaderId);
            Assert.Equal(input.DeviceId, reference.SourceDeviceId); Assert.False(string.IsNullOrEmpty(reference.FileVersion));
        }
        foreach (var reader in new[] { pm.UserName!, supervisor.UserName! })
        {
            await Login(fresh, reader);
            var authorized = await fresh.GetFromJsonAsync<FieldSubmissionView>(path + "/submissions/" + id);
            Assert.Equal(id, authorized!.Id); Assert.Equal(points, authorized.Evidence.Select(x => x.Coordinates));
        }
        await Login(fresh, crew.UserName!);
        using var replay = await Post(client, path + "/submissions", input, key, version);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(id, (await replay.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());
        var changed = input with { Evidence = [declarations[0] with { Coordinates = new(109, 13) }] };
        using var conflict = await Post(client, path + "/submissions", changed, key, version);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        using var originConflict = await Post(client, path + "/submissions", changed, version: version);
        Assert.Equal(HttpStatusCode.Conflict, originConflict.StatusCode);
        Assert.Equal("origin_content_conflict", (await originConflict.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await Login(client, pm.UserName!);
        using var reviewed = await Post(client, path + "/reviews", new { submissionId = id, decision = "SUPPLEMENT", reason = "Need position checklist" }, version: await Version(client, path));
        Assert.Equal(HttpStatusCode.Created, reviewed.StatusCode);
        await Login(client, crew.UserName!);
        var invalidPoints = new[] { new FieldCaptureCoordinates(null, 10), new(106, null), new(181, 10), new(106, 91), new(106, 10, -1) };
        foreach (var point in invalidPoints)
        {
            var invalid = input with { OriginId = Guid.NewGuid(), ParentSubmissionId = id, Evidence = [declarations[0] with { Coordinates = point }] };
            using var denied = await Post(client, path + "/submissions", invalid, version: await Version(client, path));
            Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        }
        foreach (var number in new[] { "NaN", "Infinity", "-Infinity", "1e999" })
        {
            var invalid = JsonSerializer.SerializeToNode(input with { OriginId = Guid.NewGuid(), ParentSubmissionId = id }, Json)!;
            invalid["evidence"]![0]!["coordinates"]!["longitude"] = number == "1e999" ? System.Text.Json.Nodes.JsonNode.Parse(number) : System.Text.Json.Nodes.JsonValue.Create(number);
            using var denied = await Post(client, path + "/submissions", invalid, version: await Version(client, path));
            Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        }
        using var supplement = await Post(client, path + "/submissions", input with { OriginId = Guid.NewGuid(), ParentSubmissionId = id, Evidence = [declarations[0] with { CaptureOriginId = Guid.NewGuid(), Coordinates = new(110, 14) }] }, version: await Version(client, path));
        Assert.Equal(HttpStatusCode.Created, supplement.StatusCode);
        var previous = await fresh.GetFromJsonAsync<FieldSubmissionView>(path + "/submissions/" + id);
        Assert.Equal(points, previous!.Evidence.Select(x => x.Coordinates));
        await using var db = sql.CreateDbContext();
        var persisted = await db.Set<FieldInspectionSubmission>().SingleAsync(x => x.Id == id);
        var payload = JsonSerializer.Deserialize<FieldSubmissionInput>(persisted.PayloadJson, Json)!;
        Assert.Equal(points, payload.Evidence!.Select(x => x.Coordinates));
        var links = await db.Set<FieldInspectionEvidenceLink>().Where(x => x.SubmissionId == id).ToArrayAsync();
        Assert.Equal(3, links.Length);
        foreach (var link in links)
        {
            using var snapshot = JsonDocument.Parse(link.CaptureFactsJson);
            var declaration = snapshot.RootElement.GetProperty("declared").Deserialize<FieldEvidenceDeclaration>(Json);
            Assert.Equal(declarations.Single(x => x.CaptureOriginId == link.CaptureOriginId), declaration);
        }
    }
}
