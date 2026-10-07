using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.Repositories.Storage;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Devices;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Surveys;

[Collection(AuthenticationApiFixture.Name)]
public sealed class Anh01SurveyFlowTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task ManualAssessmentPartialBaselineSupplementAndRevocationPersistThroughHttp()
    {
        var supervisor = await sql.CreateUserAsync($"anh-super-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var manager = await sql.CreateUserAsync($"anh-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var first = await sql.CreateUserAsync($"anh-op-{Guid.NewGuid():N}", "Current1!", UserRoleCode.DroneOperator);
        var second = await sql.CreateUserAsync($"anh-op2-{Guid.NewGuid():N}", "Current1!", UserRoleCode.DroneOperator);
        var media = new byte[100];
        var checksum = Convert.ToHexString(SHA256.HashData(media)).ToLowerInvariant();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        { services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(new SurveyVideoStorage(media, checksum)); });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await Login(client, supervisor.UserName!);
        var created = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            projectCode = $"ANH-{Guid.NewGuid():N}",
            name = "DEMO survey evidence",
            engineeringUtmSrid = 32648,
            startDate = "2026-09-01",
            endDate = "2027-09-01",
            primaryProjectManagerUserId = manager.Id,
            handover = new { documentNo = "DEMO", handoverDate = "2026-08-31" },
            operationId = Guid.NewGuid()
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var project = (await Body(created)).GetProperty("projectId").GetGuid();
        await Login(client, manager.UserName!);
        var draft = await Command(client, $"/api/v1/projects/{project}/road-geometry-drafts", new
        {
            sourceKind = "COORDINATES",
            sourceCrs = 32648,
            stationOriginMeters = 0d,
            changeReason = "DEMO alignment",
            coordinates = new[] { new { x = 500000d, y = 1100000d }, new { x = 500200d, y = 1100000d } },
            widthProfile = new[] { new { fromOffsetMeters = 0d, toOffsetMeters = 200d, widthMeters = 8d } },
            surveyWidthMeters = 12d,
            roadCode = "DEMO"
        });
        draft.StatusCode.Should().Be(HttpStatusCode.Created);
        await Login(client, supervisor.UserName!);
        var road = await Command(client, draft.Headers.Location!.OriginalString + "/confirm", new
        {
            expectedCurrentVersionId = (Guid?)null,
            effectiveFrom = "2026-10-02T00:00:00Z",
            reason = "Supervisor DEMO approval"
        }, draft.Headers.ETag!.Tag);
        road.StatusCode.Should().Be(HttpStatusCode.Created);
        var route = (await Body(road)).GetProperty("routeVersionId").GetGuid();
        var section = (await Body(road)).GetProperty("roadSectionId").GetGuid();
        await Login(client, manager.UserName!);
        var setsPath = $"/api/v1/projects/{project}/road-sections/{section}/versions/{route}/segment-sets";
        var segmentSet = await Command(client, setsPath, new { targetLengthMeters = 100d, remainderMode = "KEEP" });
        segmentSet.StatusCode.Should().Be(HttpStatusCode.Created);
        var set = (await Body(segmentSet)).GetProperty("id").GetGuid();
        var segment = (await Body(segmentSet)).GetProperty("segments")[0].GetProperty("id").GetGuid();
        var published = await Command(client, setsPath + $"/{set}/publish", new { expectedPublishedSetId = (Guid?)null, reason = "PM DEMO publication" }, segmentSet.Headers.ETag!.Tag);
        published.StatusCode.Should().Be(HttpStatusCode.OK);
        var device = Guid.NewGuid();
        // Registry and project membership provisioning are fixture setup; there is no assigned device allocation API.
        await using (var db = sql.CreateDbContext())
        {
            db.DroneDevices.Add(DroneDevice.Create(device, $"DEMO-{device:N}", DroneDeviceStatus.Active));
            foreach (var actor in new[] { first.Id, second.Id })
                db.ProjectMembers.Add(new ProjectMember
                {
                    Id = Guid.NewGuid(),
                    ProjectId = project,
                    UserId = actor,
                    RoleCode = UserRoleCode.DroneOperator,
                    ValidFrom = new DateOnly(2026, 1, 1),
                    Status = ProjectMemberStatus.Active
                });
            await db.SaveChangesAsync();
        }
        object Scope(string band) => new { routeVersionId = route, segmentSetId = set, segmentIds = new[] { segment }, targetBand = band };
        var scopes = new[] { Scope("SURFACE"), Scope("RIGHT_EDGE") };
        await Login(client, manager.UserName!);
        var plan = await Command(client, $"/api/v1/projects/{project}/survey-plans", new { scope = scopes, plannedAt = "2026-10-02T08:00:00Z", surveyType = "BASELINE" });
        plan.StatusCode.Should().Be(HttpStatusCode.Created);
        var planId = (await Body(plan)).GetProperty("id").GetGuid();
        (await client.GetAsync(plan.Headers.Location)).StatusCode.Should().Be(HttpStatusCode.OK);
        var taskResponse = await Command(client, $"/api/v1/projects/{project}/survey-tasks", new
        {
            scope = scopes,
            surveyType = "BASELINE",
            operatorId = first.Id,
            dueAt = "2026-10-03T08:00:00Z",
            accessPoint = (object?)null,
            planId
        });
        taskResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var task = (await Body(taskResponse)).GetProperty("id").GetGuid();
        await Login(client, first.UserName!);
        var work = await client.GetAsync($"/api/v1/survey-tasks/{task}/work-package");
        work.StatusCode.Should().Be(HttpStatusCode.OK);
        var workBody = await Body(work);
        workBody.GetProperty("schemaVersion").GetString().Should().Be("anh01.survey-work.v1");
        workBody.GetProperty("geometryRefs").GetArrayLength().Should().Be(1);
        workBody.GetProperty("geometryRefs")[0].TryGetProperty("targetBand", out _).Should().BeFalse();
        var accepted = await Command(client, $"/api/v1/survey-tasks/{task}/accept", null, taskResponse.Headers.ETag!.Tag);
        accepted.StatusCode.Should().Be(HttpStatusCode.OK);
        var uploadCreated = await Command(client, "/api/v1/uploads", new
        {
            purpose = "SURVEY_VIDEO",
            projectId = project,
            targetId = task,
            fileName = "synthetic.mp4",
            mediaType = "video/mp4",
            sizeBytes = media.Length,
            checksumSha256 = checksum
        });
        uploadCreated.StatusCode.Should().Be(HttpStatusCode.Created);
        var upload = (await Body(uploadCreated)).GetProperty("id").GetGuid();
        var video = (await Body(uploadCreated)).GetProperty("fileId").GetGuid();
        (await Command(client, $"/api/v1/uploads/{upload}/part-urls", new { partNumbers = new[] { 1 } })).StatusCode.Should().Be(HttpStatusCode.OK);
        var uploadRead = await client.GetAsync($"/api/v1/uploads/{upload}");
        (await Command(client, $"/api/v1/uploads/{upload}/complete", new { parts = new[] { new { partNumber = 1, eTag = "fixture-1" } }, checksumSha256 = checksum }, uploadRead.Headers.ETag!.Tag))
            .StatusCode.Should().Be(HttpStatusCode.Accepted);
        using (var verification = factory.Services.CreateScope())
            await verification.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Files.IUploadService>().ProcessOneVerificationAsync();
        var fileMetadata = await client.GetAsync($"/api/v1/files/{video}");
        (await Body(fileMetadata)).GetProperty("status").GetString().Should().Be("VERIFIED");
        var submit = new
        {
            videoFileIds = new[] { video },
            telemetryFileIds = Array.Empty<Guid>(),
            recordedAt = "2026-10-02T08:00:00Z",
            deviceId = device,
            scope = scopes,
            pairs = new[] { new { videoFileId = video, telemetryFileId = (Guid?)null, timeOffsetMilliseconds = 0L } }
        };
        var submitted = await Command(client, $"/api/v1/survey-tasks/{task}/datasets", submit, accepted.Headers.ETag!.Tag);
        submitted.StatusCode.Should().Be(HttpStatusCode.Created);
        var dataset = (await Body(submitted)).GetProperty("id").GetGuid();
        var detail = await client.GetAsync(submitted.Headers.Location);
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Body(detail)).GetProperty("submittedBy").GetGuid().Should().Be(first.Id);
        await Login(client, manager.UserName!);
        object Item(string band, string status) => new
        {
            routeVersionId = route,
            segmentSetId = set,
            segmentId = segment,
            targetBand = band,
            positionStatus = status,
            qualityStatus = status,
            coverageStatus = status,
            reason = status == "PASS" ? "PM inspected position, clear image and full band" : "Band not visible",
            evidence = status == "PASS" ? new[] { new { fileId = video, fromMilliseconds = 0, toMilliseconds = 1000 } } : Array.Empty<object>()
        };
        var assessmentBody = new { methodVersion = "pm-evidence-review.v1", items = new[] { Item("SURFACE", "PASS"), Item("RIGHT_EDGE", "UNKNOWN") } };
        (await Command(client, $"/api/v1/datasets/{dataset}/assessments", new { methodVersion = "unapproved.v1", items = assessmentBody.items }, submitted.Headers.ETag!.Tag))
            .StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Command(client, $"/api/v1/datasets/{dataset}/assessments", assessmentBody, "\"//////////8=\""))
            .StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        (await Command(client, $"/api/v1/datasets/{dataset}/assessments", assessmentBody, "\"malformed\""))
            .StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        var assessmentKey = Guid.NewGuid().ToString("N");
        var assessed = await Command(client, $"/api/v1/datasets/{dataset}/assessments", assessmentBody, submitted.Headers.ETag!.Tag, assessmentKey);
        assessed.StatusCode.Should().Be(HttpStatusCode.Created);
        var assessment = (await Body(assessed)).GetProperty("id").GetGuid();
        var replay = await Command(client, $"/api/v1/datasets/{dataset}/assessments", assessmentBody, submitted.Headers.ETag!.Tag, assessmentKey);
        replay.StatusCode.Should().Be(HttpStatusCode.Created);
        (await Body(replay)).GetProperty("id").GetGuid().Should().Be(assessment);
        (await client.GetAsync(assessed.Headers.Location)).StatusCode.Should().Be(HttpStatusCode.OK);
        var coverage = await Body(await client.GetAsync($"/api/v1/datasets/{dataset}/coverage"));
        coverage.GetProperty("assessmentId").GetGuid().Should().Be(assessment);
        object Selection(string band, Guid? expected = null) => new
        {
            routeVersionId = route,
            segmentSetId = set,
            segmentId = segment,
            targetBand = band,
            datasetId = dataset,
            assessmentId = assessment,
            expectedBaselineSelectionId = expected
        };
        var denied = await Command(client, $"/api/v1/projects/{project}/baseline-selections", new { items = new[] { Selection("SURFACE"), Selection("RIGHT_EDGE") }, reason = "Partial baseline" });
        denied.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await using (var db = sql.CreateDbContext()) { (await db.Set<BaselineSelection>().CountAsync(x => x.ProjectId == project)).Should().Be(0); }
        var selectionKey = Guid.NewGuid().ToString("N");
        var selectBody = new { items = new[] { Selection("SURFACE") }, reason = "Surface evidence accepted" };
        var selected = await Command(client, $"/api/v1/projects/{project}/baseline-selections", selectBody, key: selectionKey);
        selected.StatusCode.Should().Be(HttpStatusCode.Created);
        (await client.GetAsync(selected.Headers.Location)).StatusCode.Should().Be(HttpStatusCode.OK);
        var selectedReplay = await Command(client, $"/api/v1/projects/{project}/baseline-selections", selectBody, key: selectionKey);
        (await Body(selectedReplay)).GetProperty("id").GetGuid().Should().Be((await Body(selected)).GetProperty("id").GetGuid());
        var stale = await Command(client, $"/api/v1/projects/{project}/baseline-selections", selectBody);
        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var currentSelectionId = (await Body(selected)).GetProperty("items")[0].GetProperty("selectionId").GetGuid();
        var compareBody = new { items = new[] { Selection("SURFACE", currentSelectionId) }, reason = "PM reconfirmation" };
        var selectionRace = await Task.WhenAll(Command(client, $"/api/v1/projects/{project}/baseline-selections", compareBody),
            Command(client, $"/api/v1/projects/{project}/baseline-selections", compareBody));
        selectionRace.Count(r => r.StatusCode == HttpStatusCode.Created).Should().Be(1);
        selectionRace.Single(r => r.StatusCode != HttpStatusCode.Created).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await client.GetAsync(selected.Headers.Location)).StatusCode.Should().Be(HttpStatusCode.OK);
        var failReview = await Command(client, $"/api/v1/datasets/{dataset}/assessments", new { methodVersion = "pm-evidence-review.v1", items = new[] { Item("SURFACE", "FAIL"), Item("RIGHT_EDGE", "UNKNOWN") } }, submitted.Headers.ETag!.Tag);
        failReview.StatusCode.Should().Be(HttpStatusCode.Created);
        (await client.GetAsync(selected.Headers.Location)).StatusCode.Should().Be(HttpStatusCode.OK);
        // A15: a new current route must not invalidate already assigned scopes or their supplement child.
        var replacementDraft = await Command(client, $"/api/v1/projects/{project}/road-sections/{section}/geometry-drafts", new
        {
            sourceKind = "COORDINATES",
            sourceCrs = 32648,
            stationOriginMeters = 0d,
            changeReason = "Replace alignment after survey",
            coordinates = new[] { new { x = 500000d, y = 1100000d }, new { x = 500200d, y = 1100000d } },
            widthProfile = new[] { new { fromOffsetMeters = 0d, toOffsetMeters = 200d, widthMeters = 8d } },
            surveyWidthMeters = 12d
        });
        replacementDraft.StatusCode.Should().Be(HttpStatusCode.Created);
        await Login(client, supervisor.UserName!);
        var replacementRoute = await Command(client, replacementDraft.Headers.Location!.OriginalString + "/confirm", new
        {
            expectedCurrentVersionId = route,
            effectiveFrom = "2026-10-03T00:00:00Z",
            reason = "Supersede route after assigned task"
        }, replacementDraft.Headers.ETag!.Tag);
        replacementRoute.StatusCode.Should().Be(HttpStatusCode.Created);
        await Login(client, manager.UserName!);
        var replacementRouteId = (await Body(replacementRoute)).GetProperty("routeVersionId").GetGuid();
        var replacementSetsPath = $"/api/v1/projects/{project}/road-sections/{section}/versions/{replacementRouteId}/segment-sets";
        var replacementSet = await Command(client, replacementSetsPath, new { targetLengthMeters = 100d, remainderMode = "KEEP" });
        replacementSet.StatusCode.Should().Be(HttpStatusCode.Created);
        var replacementSetId = (await Body(replacementSet)).GetProperty("id").GetGuid();
        (await Command(client, replacementSetsPath + $"/{replacementSetId}/publish", new { expectedPublishedSetId = (Guid?)null, reason = "Publish replacement route" }, replacementSet.Headers.ETag!.Tag))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        var parent = await client.GetAsync($"/api/v1/survey-tasks/{task}");
        var supplementKey = Guid.NewGuid().ToString("N");
        var supplementBody = new { scope = new[] { Scope("RIGHT_EDGE") }, operatorId = second.Id, reason = "Missing right edge" };
        var supplemented = await Command(client, $"/api/v1/survey-tasks/{task}/supplements", supplementBody, parent.Headers.ETag!.Tag, supplementKey);
        supplemented.StatusCode.Should().Be(HttpStatusCode.Created);
        var child = (await Body(supplemented)).GetProperty("supplementTaskId").GetGuid();
        (await Body(supplemented)).GetProperty("id").GetGuid().Should().Be(task);
        var supplementReplay = await Command(client, $"/api/v1/survey-tasks/{task}/supplements", supplementBody, parent.Headers.ETag!.Tag, supplementKey);
        (await Body(supplementReplay)).GetProperty("supplementTaskId").GetGuid().Should().Be(child);
        var parentAfterSupplement = await client.GetAsync($"/api/v1/survey-tasks/{task}");
        var reopenParent = await Command(client, $"/api/v1/survey-tasks/{task}/reassign",
            new { operatorId = second.Id, reason = "Attempt to bypass supplement child" }, parentAfterSupplement.Headers.ETag!.Tag);
        reopenParent.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await Login(client, first.UserName!);
        (await client.GetAsync($"/api/v1/survey-tasks/{child}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await Login(client, second.UserName!);
        var childRead = await client.GetAsync(supplemented.Headers.Location);
        childRead.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Command(client, $"/api/v1/survey-tasks/{child}/accept", null, childRead.Headers.ETag!.Tag)).StatusCode.Should().Be(HttpStatusCode.OK);
        var childAccepted = await client.GetAsync($"/api/v1/survey-tasks/{child}");
        var largeVideos = new List<Guid>();
        for (var index = 0; index < 4; index++) largeVideos.Add(await VerifiedSource(project, child, second.Id, "SURVEY_VIDEO", 8_589_934_592L));
        var extraByte = await VerifiedSource(project, child, second.Id, "SURVEY_VIDEO", 1);
        object LargeSubmission(IEnumerable<Guid> videos, Guid registryDevice) => new
        {
            videoFileIds = videos.ToArray(),
            telemetryFileIds = Array.Empty<Guid>(),
            recordedAt = "2026-10-02T08:00:00Z",
            deviceId = registryDevice,
            scope = new[] { Scope("RIGHT_EDGE") }
        };
        var excess = await Command(client, $"/api/v1/survey-tasks/{child}/datasets", LargeSubmission(largeVideos.Append(extraByte), device), childAccepted.Headers.ETag!.Tag);
        excess.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var unknownDevice = await Command(client, $"/api/v1/survey-tasks/{child}/datasets", LargeSubmission(largeVideos, Guid.NewGuid()), childAccepted.Headers.ETag!.Tag);
        unknownDevice.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await using (var rejected = sql.CreateDbContext())
        {
            (await rejected.Surveys.CountAsync(s => s.SurveyRequestId == child)).Should().Be(0);
            (await rejected.SurveyRequests.SingleAsync(t => t.Id == child)).Status.Should().Be(SurveyRequestStatus.Accepted);
        }
        var exact = LargeSubmission(largeVideos, device);
        var concurrent = await Task.WhenAll(Command(client, $"/api/v1/survey-tasks/{child}/datasets", exact, childAccepted.Headers.ETag!.Tag),
            Command(client, $"/api/v1/survey-tasks/{child}/datasets", exact, childAccepted.Headers.ETag!.Tag));
        concurrent.Count(r => r.StatusCode == HttpStatusCode.Created).Should().Be(1);
        concurrent.Single(r => r.StatusCode != HttpStatusCode.Created).StatusCode.Should().BeOneOf(HttpStatusCode.Conflict, HttpStatusCode.PreconditionFailed);
        var childDataset = (await Body(concurrent.Single(r => r.StatusCode == HttpStatusCode.Created))).GetProperty("id").GetGuid();
        var childDetail = await Body(await client.GetAsync($"/api/v1/datasets/{childDataset}"));
        childDetail.GetProperty("sourceFiles").EnumerateArray().Sum(f => f.GetProperty("sizeBytes").GetInt64()).Should().Be(34_359_738_368L);
        await using (var db = sql.CreateDbContext())
        {
            (await db.Set<DatasetAssessment>().CountAsync(x => x.DatasetId == dataset)).Should().Be(2);
            (await db.Set<BaselineSelection>().CountAsync(x => x.ProjectId == project)).Should().Be(2);
            (await db.SurveyRequests.CountAsync(x => x.ParentTaskId == task)).Should().Be(1);
            (await db.Surveys.CountAsync(x => x.SurveyRequestId == child)).Should().Be(1);
            var membership = await db.ProjectMembers.SingleAsync(x => x.UserId == second.Id && x.ProjectId == project);
            membership.Status = ProjectMemberStatus.Ended; await db.SaveChangesAsync();
        }
        (await client.GetAsync($"/api/v1/survey-tasks/{child}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
    private async Task<Guid> VerifiedSource(Guid project, Guid task, Guid actor, string purpose, long size)
    {
        var id = Guid.NewGuid(); var now = DateTimeOffset.UtcNow; var hash = new string('a', 64);
        await using var db = sql.CreateDbContext();
        db.Files.Add(StoredFile.Create(id, $"test/{id:N}", "test.mp4", "video/mp4", size, hash, actor, now, null));
        db.FileScopes.Add(FileScope.Create(Guid.NewGuid(), id, project, task, actor, purpose, now));
        var upload = UploadSession.Create(Guid.NewGuid(), id, actor, $"test/{id:N}", purpose, "video/mp4", size, hash, 8 * 1024 * 1024, now.AddHours(1));
        db.UploadSessions.Add(upload); upload.StartUploading("fixture", now); await db.SaveChangesAsync();
        upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now); upload.MarkVerified(); await db.SaveChangesAsync();
        return id;
    }
    private static async Task Login(HttpClient client, string username)
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(username), password = "Current1!" });
        login.EnsureSuccessStatusCode(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Body(login)).GetProperty("accessToken").GetString());
    }
    private static async Task<HttpResponseMessage> Command(HttpClient client, string url, object? body, string? version = null, string? key = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = body is null ? null : JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString("N"));
        if (version is not null) request.Headers.TryAddWithoutValidation("If-Match", version);
        return await client.SendAsync(request);
    }
    private static async Task<JsonElement> Body(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }
    private sealed class SurveyVideoStorage(byte[] media, string checksum) : IUploadObjectStorage
    {
        public Task<string> InitiateAsync(string objectKey, string mediaType, CancellationToken cancellationToken = default) => Task.FromResult("fixture-upload");
        public Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(string objectKey, string uploadId, IReadOnlyList<int> partNumbers,
            DateTimeOffset expiresAt, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PresignedUploadPart>>(partNumbers.Select(n => new PresignedUploadPart(n, $"https://storage.test/{n}", expiresAt)).ToArray());
        public Task<UploadObjectVerification> CompleteAndVerifyAsync(string objectKey, string uploadId, IReadOnlyList<CompletedStoragePart> parts,
            CancellationToken cancellationToken = default) => Task.FromResult(new UploadObjectVerification(media.Length, checksum, "video/mp4"));
        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(new MemoryStream(media, false));
    }
}
