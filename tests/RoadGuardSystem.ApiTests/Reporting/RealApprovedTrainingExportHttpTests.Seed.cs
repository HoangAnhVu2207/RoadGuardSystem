using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Processing.Worker;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Services.Integration;
using RoadGuardSystem.Services.Processing.Worker;
using Xunit;

namespace RoadGuardSystem.ApiTests.Reporting;

public sealed partial class RealApprovedTrainingExportHttpTests
{
    private async Task<RealSource> Seed(AuthenticationWebApplicationFactory factory)
    {
        await using var db = sql.CreateDbContext(); var s = await GenuineProtectedSource.Seed(db, sql);
        var now = DateTimeOffset.UtcNow;
        // Complete the controlled geometry fixture; exercise the real resolver without weakening its metadata guard.
        var geometryInput = new GeometryDraftInputFact("COORDINATES", 32648, 0, "Controlled REAL export geometry fixture",
            [new(0, 0), new(20, 0)], null, null, null, [new(0, 2, 2)], 4);
        var inputJson = JsonSerializer.Serialize(geometryInput, Json);
        var draft = RoadGeometryDraft.Create(s.Project, s.Road, "REAL-EXPORT", null, inputJson, "[]", null, s.Pm, now);
        db.AddRange(draft, RoadGeometryMetadata.Create(s.Route, draft.Id, inputJson, Hash(Encoding.UTF8.GetBytes(inputJson)), s.Supervisor, now: now));
        var segmentSet = await db.RoadSegmentSets.SingleAsync(x => x.Id == s.Set);
        db.Entry(segmentSet).Property(x => x.DefinitionJson).CurrentValue = JsonSerializer.Serialize(new SegmentDefinitionFact(20), Json);
        db.Entry(segmentSet).Property(x => x.GeometryHash).CurrentValue = Hash(Encoding.UTF8.GetBytes(inputJson));
        await db.SaveChangesAsync();
        var op = await sql.CreateUserAsync("real-export-op-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.DroneOperator);
        var task = SurveyRequest.Create(Guid.NewGuid(), s.Project, s.Road, null, s.Pm, SurveyType.Periodic, SurveyRequestStatus.Submitted, now);
        var survey = Survey.Create(Guid.NewGuid(), task.Id, s.Project, s.Route, SurveyType.Periodic, SurveyStatus.InProgress, false, null, null);
        var video = StoredFile.Create(Guid.NewGuid(), "test/source-video-" + Guid.NewGuid().ToString("N"), "video.mp4", "video/mp4", 10, new string('a', 64), op.Id, now, null);
        var upload = UploadSession.Create(Guid.NewGuid(), video.Id, op.Id, video.StorageUri, "SURVEY_VIDEO", video.MimeType, video.SizeBytes, video.Checksum, 8388608, now.AddHours(1));
        upload.StartUploading("fixture-only-source", now);
        db.AddRange(task, survey, video, upload, FileScope.Create(Guid.NewGuid(), video.Id, s.Project, task.Id, op.Id, "SURVEY_VIDEO", now),
            new ProjectMember { Id = Guid.NewGuid(), ProjectId = s.Project, UserId = op.Id, RoleCode = UserRoleCode.DroneOperator, Status = ProjectMemberStatus.Active, ValidFrom = new(2000, 1, 1) },
            SurveyAssignment.Create(Guid.NewGuid(), task.Id, op.Id, s.Pm, now, now, null, null, null, null));
        await db.SaveChangesAsync(); upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now);
        await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync();
        var dataset = SurveyDataVersion.Create(Guid.NewGuid(), survey.Id, 1, SurveyDataVersionStatus.ServerConfirmed, SurveyDataIntegrityStatus.Passed,
            now, SurveyDataConfirmationActor.Backend, JsonSerializer.Serialize(new[] { new { fileId = video.Id, purpose = "SURVEY_VIDEO", checksumSha256 = video.Checksum, sizeBytes = video.SizeBytes } }, Json));
        dataset.SetSubmissionProvenance(op.Id, "[]");
        var model = AIModelVersion.Create(Guid.NewGuid(), "REAL export schema fixture, no inference", Guid.NewGuid().ToString("N"), "fixture/model", null, null, AIModelVersionStatus.Released, now, s.Supervisor);
        var block = ProcessingBlock.Create(Guid.NewGuid(), dataset.Id, 1, "{}"); db.AddRange(dataset, model, block); await db.SaveChangesAsync();
        var map = new Dictionary<string, string> { ["alligator crack"] = "ALLIGATOR_CRACK", ["longitudinal crack"] = "LONGITUDINAL_CRACK", ["oblique crack"] = "OBLIQUE_CRACK", ["pothole"] = "POTHOLE", ["repair"] = "REPAIR_PATCH", ["transverse crack"] = "TRANSVERSE_CRACK" };
        foreach (var code in map.Values) if (!await db.DefectTypes.AnyAsync(x => x.Code == code)) db.Add(DefectType.Create(code, code));
        await db.SaveChangesAsync();
        ProjectGeometryContext geometry;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var resolved = await scope.ServiceProvider.GetRequiredService<IAnhHuyProducerService>().ResolveGeometryAsync(s.Pm, UserRoleCode.ProjectManager, s.Project, s.Route, s.Set);
            Assert.Equal(AnhHuyProducerStatus.Ready, resolved.Status); geometry = resolved.Facts!;
        }
        var jobId = Guid.NewGuid(); var attempt = Guid.NewGuid(); var workId = Guid.NewGuid(); var result = Guid.NewGuid();
        var manifest = JsonSerializer.Serialize(new
        {
            contractVersion = "rg-ai-worker-v1",
            datasetId = dataset.Id,
            projectId = s.Project,
            sourceVideoFileId = video.Id,
            inputSha256 = video.Checksum,
            inputBytes = video.SizeBytes,
            modelVersionId = model.Id,
            sourceVideoVersion = Convert.ToBase64String(upload.RowVersion),
            checkpointSha256 = new string('b', 64),
            preprocessingVersion = "ffmpeg-pts-v1",
            configVersion = "ft95-cpu-v1",
            inputWidth = 1,
            inputHeight = 1,
            inputDurationMs = 10000,
            sampleFps = 1,
            maxFrames = 10,
            logicalConfigurationIdentity = "fixture-real-export",
            classMap = map,
            geometry = new AiVideoGeometrySnapshot(s.Route, s.Set, geometry.Version, geometry.Segments.Select(x => x.Id).ToArray())
        }, Json);
        var admission = new AiWorkerAdmission("rg-ai-worker-v1", s.Project, jobId, attempt, dataset.Id, video.Id, Hash(Encoding.UTF8.GetBytes(manifest)), model.Id,
            new string('b', 64), "ffmpeg-pts-v1", "ft95-cpu-v1", video.Checksum, video.SizeBytes, 1, 1, 10000, 1, 10);
        var job = ProcessingJob.CreateQueued(jobId, block.Id, model.Id, s.Project, Hash(Encoding.UTF8.GetBytes(manifest)), manifest, "REAL");
        var work = AiVideoWorkItem.Create(workId, dataset.Id, video.Id, s.Project, jobId, attempt, "fixture-real-export", JsonSerializer.Serialize(admission, AiWorkerWireContract.JsonOptions), now);
        work.AcquireLease("fixture", now, now.AddMinutes(1)); work.Transition("fixture", attempt, AiVideoWorkState.Admitted, now);
        work.Transition("fixture", attempt, AiVideoWorkState.ArtifactsReady, now);
        db.AddRange(job, ProcessingAttempt.Create(attempt, jobId, 1, now, null, null, "fixture"), work);
        await db.SaveChangesAsync();
        async Task<AiRegisteredArtifact> Artifact(string kind, string media, byte[] bytes)
        {
            var artifactId = Guid.NewGuid(); var key = $"anh02/real/{attempt:D}/{artifactId:D}";
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                using var content = new MemoryStream(bytes);
                var written = await scope.ServiceProvider.GetRequiredService<IAnh02ArtifactStore>().WriteAsync(key, content, bytes.Length, media);
                Assert.Equal(Hash(bytes), written.Sha256);
            }
            return new(artifactId, Hash(bytes), bytes.Length, media, kind, key, kind == "FRAME" ? 1 : (int?)null, kind == "FRAME" ? 1 : (int?)null);
        }
        var frame = await Artifact("FRAME", "image/jpeg", FrameBytes);
        var detectionId = Guid.NewGuid();
        var detections = new[] { new { detectionId, frameArtifactId = frame.ArtifactId, timestampMs = 1500,
            sourceClass = "pothole", confidence = .9, bbox = new[] { .1, .2, .3, .4 } } };
        var raw = await Artifact("RAW_RESULT", "application/json", JsonSerializer.SerializeToUtf8Bytes(new
        { contractVersion = admission.ContractVersion, resultId = result, jobId, attemptId = attempt, detections }, Json));
        var wire = JsonSerializer.SerializeToNode(admission, AiWorkerWireContract.JsonOptions)!.AsObject();
        wire["resultId"] = JsonSerializer.SerializeToNode(result);
        wire["rawArtifact"] = JsonSerializer.SerializeToNode(new { artifactId = raw.ArtifactId, sha256 = raw.Sha256, bytes = raw.Bytes, mediaType = raw.MediaType });
        wire["frameArtifacts"] = JsonSerializer.SerializeToNode(new[] { new { artifactId = frame.ArtifactId, sha256 = frame.Sha256, bytes = frame.Bytes, mediaType = frame.MediaType, width = 1, height = 1 } });
        wire["detections"] = JsonSerializer.SerializeToNode(detections);
        wire["telemetry"] = JsonSerializer.SerializeToNode(new { source = "NONE", outcome = "NO_SUBTITLE_STREAM", sourceVideoSha256 = video.Checksum, streamIndex = (int?)null, extractorVersion = "fixture", srtArtifact = (object?)null });
        wire["roadMaskStatus"] = "NOT_AVAILABLE"; wire["limitations"] = JsonSerializer.SerializeToNode(new[] { "TEST_FIXTURE_NO_INFERENCE" });
        using var parsedWire = JsonDocument.Parse(wire.ToJsonString());
        var parsed = AiWorkerWireContract.ParseResult(parsedWire.RootElement, admission);
        var payload = JsonSerializer.Serialize(new
        {
            contractVersion = admission.ContractVersion,
            resultId = result,
            frameArtifactId = frame.ArtifactId,
            timestampMs = 1500,
            sourceVideoSha256 = video.Checksum,
            bbox = new[] { .1m, .2m, .3m, .4m },
            confidence = .9m,
            proposalOnly = true
        }, Json);
        db.ChangeTracker.Clear();
        await using (var scope = factory.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<IAiVideoWorkRepository>().CompleteAsync(work.Id, "fixture", attempt,
                [frame, raw], [new(parsed.Detections.Single().DetectionId, frame.ArtifactId, "pothole", "POTHOLE", .9m, payload)], map, now, default);
        var frameFile = await db.AiWorkerArtifacts.Where(x => x.WorkItemId == work.Id && x.ArtifactId == frame.ArtifactId.ToString("D")).Select(x => x.FileId!.Value).SingleAsync();
        var approvedType = await db.Defects.Where(x => x.Id == s.Defect).Select(x => x.DefectTypeCode!).SingleAsync();
        return new(s.Pm, s.Project, await db.Users.Where(x => x.Id == s.Pm).Select(x => x.Email!).SingleAsync(), detectionId, frameFile, approvedType);
    }
    private sealed record RealSource(Guid Pm, Guid Project, string Email, Guid Detection, Guid Frame, string ApprovedType);
}
