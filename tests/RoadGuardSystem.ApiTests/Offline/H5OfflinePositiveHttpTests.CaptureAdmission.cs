using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Offline;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.DTOs.Offline;
using RoadGuardSystem.Services.Files;
using RoadGuardSystem.Services.Offline;
using Xunit;

namespace RoadGuardSystem.ApiTests.Offline;

public sealed partial class H5OfflinePositiveHttpTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SignedCaptureStagesUploadsToRealMinioAndResumesOriginalBatchExactlyOnce(bool handover)
    {
        await using var minio = await OwnedWebMinio.StartAsync(createBucket: true);
        var suffix = Guid.NewGuid().ToString("N");
        var pm = await fixture.CreateUserAsync("capture-pm-" + suffix, "Current1!", UserRoleCode.ProjectManager);
        var crew = await fixture.CreateUserAsync("capture-crew-" + suffix, "Current1!", UserRoleCode.RepairCrew);
        var sup = await fixture.CreateUserAsync("capture-sup-" + suffix, "Current1!", UserRoleCode.Supervisor);
        var reporter = await fixture.CreateUserAsync("capture-reporter-" + suffix, "Current1!", UserRoleCode.Reporter);
        var scope = await SeedAsync(pm.Id, crew.Id, sup.Id, reporter.Id);
        using var logs = new InMemoryLogProvider();
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString,
            loggerProvider: logs, allowExternalStorage: true, configureTestServices: minio.Configure);
        using var client = factory.CreateClient(new() { BaseAddress = new("https://localhost") });
        var field = $"/api/v1/projects/{scope.Project}/field-inspection-tasks";
        var offline = $"/api/v1/projects/{scope.Project}/offline";
        var task = await CreateAcceptedTaskAsync(client, field, scope, pm.UserName!, crew.UserName!, crew.Id);
        var device = Guid.NewGuid();
        using var keys = OfflineDeviceKeys.Generate(crew.Id, device.ToString("D"));
        var registration = (await Body(await PostAsync(client, offline + "/devices",
            new OfflineDeviceRegisterInput(device, keys.PublicKeys.EncryptionPublicKey, keys.PublicKeys.SigningPublicKey)), 201)).GetProperty("id").GetGuid();
        var snapshot = await SnapshotAsync(client, offline, registration, task);
        var start = Operation(crew.Id, device, snapshot, task);
        byte[] photo = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+j1ioAAAAASUVORK5CYII=");
        var hash = Convert.ToHexString(SHA256.HashData(photo)).ToLowerInvariant();
        var capture = Guid.NewGuid(); var origin = Guid.NewGuid();
        var submission = new FieldSubmissionInput(origin, start.EffectId, null, [],
            [new(capture, null, "MEASUREMENT", hash, "image/png", DateTimeOffset.UtcNow, null, photo.Length)],
            new("POSITION_CHECKLIST", "observed", null, null, ObservedRouteVersionId: scope.Route,
                ObservedChainageMeters: 10), "MEASUREMENT", null, null, device);
        var operation = new OfflineOperationInput(1, origin, origin, "FIELD_SUBMISSION", crew.Id, device,
            snapshot.SnapshotId, task.TaskId, task.AssignmentId, snapshot.Version, new string('0', 64),
            [start.OriginId], null, FieldSubmission: submission);
        operation = operation with { CorePayloadHash = OfflineWorkflowEngine.FieldCoreHash(operation) };
        var batchId = Guid.NewGuid();
        var signature = OfflinePackageAuthentication.SignClaim(OfflineWorkflowEngine.CanonicalManifest(scope.Project,
            batchId, registration, [OfflineWorkflowEngine.Describe(start), OfflineWorkflowEngine.Describe(operation)]), keys);
        var input = new OfflineSignedBatchInput(batchId, registration, [start, operation], signature);
        var original = JsonSerializer.Serialize(input, Json);
        OfflinePackageImportInput? imported = null;
        var currentUser = crew.UserName!;
        if (handover)
        {
            var recipientDevice = Guid.NewGuid();
            using var recipientKeys = OfflineDeviceKeys.Generate(pm.Id, recipientDevice.ToString("D"));
            await LoginAsync(client, pm.UserName!);
            var recipientRegistration = (await Body(await PostAsync(client, offline + "/devices",
                new OfflineDeviceRegisterInput(recipientDevice, recipientKeys.PublicKeys.EncryptionPublicKey,
                    recipientKeys.PublicKeys.SigningPublicKey)), 201)).GetProperty("id").GetGuid();
            await LoginAsync(client, crew.UserName!);
            var attached = OfflineWorkflowEngine.CanonicalAttachedPayload(batchId, registration, input.Operations, signature);
            var payloadHash = Convert.ToHexString(SHA256.HashData(attached)).ToLowerInvariant();
            var packageId = Guid.NewGuid();
            var header = OfflinePackageHeader.Create(packageId, scope.Project, crew.Id, device.ToString("D"),
                [start.OriginId, operation.OriginId], payloadHash);
            var encrypted = OfflineHandoverCrypto.Seal(header, attached, keys, [recipientKeys.PublicKeys]);
            var descriptors = input.Operations.Select(OfflineWorkflowEngine.Describe).ToArray();
            await Body(await PostAsync(client, offline + "/packages/export", new OfflinePackageExportInput(
                H5EncryptedPackageDto.FromDomain(encrypted), new(batchId, registration, descriptors, signature))), 201);
            await LoginAsync(client, sup.UserName!);
            var granted = await Body(await PostAsync(client, offline + "/grants", new OfflineHandoverGrantInput(packageId,
                recipientRegistration, input.Operations.Select(o => new OfflineGrantItemScope(o.OriginId, o.Kind,
                    o.CorePayloadHash, OfflineWorkflowEngine.EnvelopeHash(o), o.TaskId, o.AssignmentId, o.SnapshotId)).ToArray(),
                "Recover signed captures without impersonating original Crew")), 201);
            var grantId = granted.GetProperty("id").GetGuid(); var importBatch = Guid.NewGuid();
            var endorsement = OfflinePackageAuthentication.SignClaim(OfflineRecipientEndorsement.CanonicalClaim(new(
                scope.Project, packageId, grantId, importBatch, recipientRegistration, payloadHash)), recipientKeys);
            imported = new(importBatch, packageId, grantId, recipientRegistration, input.Operations, signature, endorsement);
            currentUser = pm.UserName!; await LoginAsync(client, currentUser);
        }
        Task<HttpResponseMessage> SendSigned() => imported is null
            ? PostAsync(client, offline + "/sync", input) : PostAsync(client, offline + "/import", imported);
        var reconnects = await Task.WhenAll(SendSigned(), SendSigned());
        var staged = await Body(reconnects[0], 200);
        var repeatedStage = await Body(reconnects[1], 200);
        Assert.Equal(staged.GetProperty("items")[1].GetProperty("admission").GetProperty("id").GetGuid(),
            repeatedStage.GetProperty("items")[1].GetProperty("admission").GetProperty("id").GetGuid());
        Assert.False(repeatedStage.GetProperty("items")[1].GetProperty("durableAcknowledgment").GetBoolean());
        var pending = staged.GetProperty("items")[1];
        Assert.False(pending.GetProperty("durableAcknowledgment").GetBoolean());
        Assert.Equal("PENDING_DEPENDENCY", pending.GetProperty("state").GetString());
        var admission = pending.GetProperty("admission");
        Assert.Equal("WAITING_UPLOAD", admission.GetProperty("status").GetString());
        Assert.Equal(crew.Id, admission.GetProperty("originalActorId").GetGuid());
        Assert.Equal(handover ? pm.Id : crew.Id, admission.GetProperty("currentImporterId").GetGuid());
        Assert.Equal(handover, admission.GetProperty("grantId").ValueKind == JsonValueKind.String);
        Assert.Equal(capture, admission.GetProperty("captures")[0].GetProperty("captureOriginId").GetGuid());
        var admissionId = admission.GetProperty("id").GetGuid();
        var serverBatch = staged.GetProperty("batchId").GetGuid();
        // Response loss/restart: discover the durable admission instead of creating a different signed batch.
        await LoginAsync(client, currentUser);
        var read = await Body(await client.GetAsync(offline + $"/batches/{serverBatch}"), 200);
        Assert.Equal(admissionId, read.GetProperty("items")[1].GetProperty("admission").GetProperty("id").GetGuid());
        var uploadInput = new
        {
            admissionId,
            captureOriginId = capture,
            upload = new
            {
                purpose = "MEASUREMENT",
                projectId = scope.Project,
                targetId = task.TaskId,
                fileName = "offline.png",
                mediaType = "image/png",
                sizeBytes = photo.Length,
                checksumSha256 = hash
            }
        };
        await Body(await PostAsync(client, "/api/v1/offline/uploads", uploadInput with
        { admissionId = Guid.NewGuid() }), 404);
        await Body(await PostAsync(client, "/api/v1/offline/uploads", uploadInput with
        { captureOriginId = Guid.NewGuid() }), 404);
        foreach (var invalid in new[]
        {
            uploadInput.upload with { sizeBytes = photo.Length + 1 },
            uploadInput.upload with { checksumSha256 = new string('a', 64) },
            uploadInput.upload with { mediaType = "image/jpeg" },
            uploadInput.upload with { purpose = "AFTER" }
        })
            Assert.Equal("upload_session_not_found", (await Body(await PostAsync(client, "/api/v1/offline/uploads",
                uploadInput with { upload = invalid }), 404)).GetProperty("code").GetString());
        var createKey = Guid.NewGuid().ToString("N");
        async Task<HttpResponseMessage> CreateUpload()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/offline/uploads") { Content = JsonContent.Create(uploadInput) };
            request.Headers.Add("Idempotency-Key", createKey); return await client.SendAsync(request);
        }
        var uploaded = await Body(await CreateUpload(), 201);
        Assert.Equal(uploaded.GetProperty("id").GetGuid(), (await Body(await CreateUpload(), 201)).GetProperty("id").GetGuid());
        var uploadId = uploaded.GetProperty("id").GetGuid(); var fileId = uploaded.GetProperty("fileId").GetGuid();
        var resumed = await Body(await client.GetAsync(offline + $"/batches/{serverBatch}"), 200);
        var mapped = resumed.GetProperty("items")[1].GetProperty("admission").GetProperty("captures")[0];
        Assert.Equal(uploadId, mapped.GetProperty("uploadId").GetGuid());
        Assert.Equal(fileId, mapped.GetProperty("fileId").GetGuid());
        Assert.False(resumed.GetProperty("items")[1].GetProperty("durableAcknowledgment").GetBoolean());
        await LoginAsync(client, handover ? crew.UserName! : pm.UserName!);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(offline + $"/batches/{serverBatch}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(offline + $"/origins/{origin}")).StatusCode);
        await LoginAsync(client, currentUser);
        var urls = await Body(await PostAsync(client, $"/api/v1/uploads/{uploadId}/part-urls", new { partNumbers = new[] { 1 } }), 200);
        using var storage = new HttpClient(); using var bytes = new ByteArrayContent(photo);
        using var put = await storage.PutAsync(urls.GetProperty("parts")[0].GetProperty("url").GetString(), bytes);
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var session = await Body(await client.GetAsync($"/api/v1/uploads/{uploadId}"), 200);
        await Body(await PostAsync(client, $"/api/v1/uploads/{uploadId}/complete", new
        { parts = new[] { new { partNumber = 1, eTag = put.Headers.ETag!.Tag } }, checksumSha256 = hash },
            session.GetProperty("version").GetString()), 202);
        await using (var services = factory.Services.CreateAsyncScope())
            await services.ServiceProvider.GetRequiredService<IUploadService>().ProcessOneVerificationAsync();
        var verified = await Body(await client.GetAsync($"/api/v1/files/{fileId}"), 200);
        Assert.Equal("VERIFIED", verified.GetProperty("status").GetString());
        Assert.Equal(original, JsonSerializer.Serialize(input, Json));
        var responses = await Task.WhenAll(SendSigned(), SendSigned());
        foreach (var response in responses)
        {
            var committed = await Body(response, 200);
            var result = committed.GetProperty("items")[1];
            Assert.True(result.GetProperty("durableAcknowledgment").GetBoolean(), result.GetRawText());
            Assert.Equal("COMMITTED", result.GetProperty("admission").GetProperty("status").GetString());
            Assert.Equal(fileId, result.GetProperty("admission").GetProperty("captures")[0].GetProperty("fileId").GetGuid());
        }
        await LoginAsync(client, currentUser);
        var receipt = await Body(await client.GetAsync(offline + $"/origins/{origin}"), 200);
        Assert.True(receipt.GetProperty("durableAcknowledgment").GetBoolean());
        if (Environment.GetEnvironmentVariable("RG_ADMISSION_EVIDENCE") is { } directory)
        {
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(Path.Combine(directory, handover ? "offline-import-wire.json" : "offline-wire.json"), JsonSerializer.Serialize(new
            { staged, uploaded, verified, receipt, corePayloadHash = operation.CorePayloadHash }, Json));
        }
        await using var db = fixture.CreateDbContext();
        Assert.Single(await db.Set<OfflineOperationAdmission>().Where(a => a.Id == admissionId).ToArrayAsync());
        Assert.Single(await db.Set<OfflineOperationResult>().Where(r => r.AdmissionId == admissionId && r.DurableAck).ToArrayAsync());
        Assert.Single(await db.Set<FieldInspectionSubmission>().Where(s => s.TaskId == task.TaskId).ToArrayAsync());
        Assert.Single(await db.Set<FieldInspectionEvidenceLink>().Where(e => e.TaskId == task.TaskId && e.FileId == fileId).ToArrayAsync());
        var transport = await db.Set<OfflineEvidenceCaptureReference>().SingleAsync(e => e.FileId == fileId);
        Assert.Equal(crew.Id, transport.OriginalActorId);
        Assert.Equal(handover ? pm.Id : crew.Id, transport.ActualUploaderId);
        var expiredAssignment = await db.FieldInspectionAssignments.SingleAsync(a => a.Id == task.AssignmentId);
        expiredAssignment.End(DateTimeOffset.UtcNow, "Revoked after commit"); await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync(offline + $"/batches/{serverBatch}")).StatusCode);
        var deniedReplay = await Body(await SendSigned(), 200);
        Assert.False(deniedReplay.GetProperty("items")[1].GetProperty("durableAcknowledgment").GetBoolean());
        async Task<JsonElement> Body(HttpResponseMessage response, int expected)
        {
            var raw = await response.Content.ReadAsStringAsync();
            Assert.True((int)response.StatusCode == expected, $"Expected {expected}, got {(int)response.StatusCode}: {raw} {(expected == (int)response.StatusCode ? "" : logs.CombinedEntries)}");
            return JsonDocument.Parse(raw).RootElement.Clone();
        }
    }
}
