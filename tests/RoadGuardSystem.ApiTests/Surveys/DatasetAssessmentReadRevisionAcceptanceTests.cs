using Microsoft.Extensions.Options;
using RoadGuardSystem.Services.Authentication;
using RoadGuardSystem.Services.Factories;
using RoadGuardSystem.Services.Options;
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
public sealed class DatasetAssessmentReadRevisionAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task NewAssessmentUpdatesCoverageButPreservesHistoricalReadVersion()
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

        static void Fields(JsonElement value, params string[] expected) =>
            value.EnumerateObject().Select(p => p.Name).Order().Should().Equal(expected.Order());
        async Task<JsonElement> Read(string path, bool etag)
        {
            using var response = await client.GetAsync(path);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
            Guid.TryParse(response.Headers.GetValues("X-Correlation-ID").Single(), out _).Should().BeTrue();
            response.Headers.Location.Should().BeNull();
            var value = await Body(response);
            if (etag) response.Headers.ETag!.Tag.Should().Be('"' + value.GetProperty("version").GetString() + '"');
            else response.Headers.ETag.Should().BeNull();
            foreach (var denied in new[] { "storageUri", "storageKey", "objectKey", "password", "accessToken", "email", "phoneNumber", "https://storage.test", "test/" })
                value.GetRawText().Should().NotContain(denied);
            return value;
        }
        var datasetPath = $"/api/v1/datasets/{dataset}";
        var coveragePath = datasetPath + "/coverage";
        var projection = await Read(datasetPath, true);
        Fields(projection, "id", "surveyTaskId", "projectId", "dataVersionId", "submittedBy", "submittedAt", "recordedAt", "deviceId", "scope", "integrityStatus", "telemetryStatus", "sourceFiles", "pairs", "version");
        projection.GetProperty("id").GetGuid().Should().Be(dataset);
        projection.GetProperty("dataVersionId").GetGuid().Should().Be(dataset);
        projection.GetProperty("surveyTaskId").GetGuid().Should().Be(task);
        projection.GetProperty("projectId").GetGuid().Should().Be(project);
        projection.GetProperty("deviceId").GetGuid().Should().Be(device);
        projection.GetProperty("submittedBy").GetGuid().Should().Be(first.Id);
        projection.GetProperty("integrityStatus").GetString().Should().Be("PASSED");
        projection.GetProperty("telemetryStatus").GetString().Should().Be("MISSING");
        projection.GetProperty("recordedAt").GetDateTimeOffset().Should().Be(DateTimeOffset.Parse("2026-10-02T08:00:00Z"));
        projection.GetProperty("submittedAt").GetDateTimeOffset().Should().BeBefore(DateTimeOffset.UtcNow);
        var file = projection.GetProperty("sourceFiles").EnumerateArray().Single();
        Fields(file, "fileId", "checksumSha256", "sizeBytes", "mediaType", "purpose");
        file.GetProperty("fileId").GetGuid().Should().Be(video);
        file.GetProperty("checksumSha256").GetString().Should().Be(checksum);
        file.GetProperty("sizeBytes").GetInt64().Should().Be(media.Length);
        file.GetProperty("mediaType").GetString().Should().Be("video/mp4");
        file.GetProperty("purpose").GetString().Should().Be("SURVEY_VIDEO");
        foreach (var scope in projection.GetProperty("scope").EnumerateArray())
        {
            Fields(scope, "routeVersionId", "segmentSetId", "segmentIds", "targetBand");
            scope.GetProperty("routeVersionId").GetGuid().Should().Be(route);
            scope.GetProperty("segmentSetId").GetGuid().Should().Be(set);
            scope.GetProperty("segmentIds").EnumerateArray().Single().GetGuid().Should().Be(segment);
        }
        var pair = projection.GetProperty("pairs").EnumerateArray().Single();
        Fields(pair, "videoFileId", "telemetryFileId", "timeOffsetMilliseconds");
        pair.GetProperty("videoFileId").GetGuid().Should().Be(video);
        pair.GetProperty("telemetryFileId").ValueKind.Should().Be(JsonValueKind.Null);
        pair.GetProperty("timeOffsetMilliseconds").GetInt64().Should().Be(0);
        var unknown = await Read(coveragePath, false);
        Fields(unknown, "datasetId", "items", "methodVersion", "assessmentId", "version");
        unknown.GetProperty("datasetId").GetGuid().Should().Be(dataset);
        unknown.GetProperty("methodVersion").GetString().Should().Be("coverage-not-evaluated.v1");
        unknown.GetProperty("assessmentId").ValueKind.Should().Be(JsonValueKind.Null);
        unknown.GetProperty("version").ValueKind.Should().Be(JsonValueKind.Null);
        foreach (var item in unknown.GetProperty("items").EnumerateArray())
        {
            Fields(item, "scope", "positionCoverage", "qualityCoverage", "overallCoverage", "reasons", "coverageStatus");
            item.GetProperty("overallCoverage").GetString().Should().Be("UNKNOWN");
            item.GetProperty("positionCoverage").GetString().Should().Be("UNKNOWN");
            item.GetProperty("qualityCoverage").GetString().Should().Be("UNKNOWN");
        }
        var assessed = await Command(client, datasetPath + "/assessments", new
        {
            methodVersion = "pm-evidence-review.v1",
            items = new[] { new { routeVersionId = route, segmentSetId = set, segmentId = segment, targetBand = "SURFACE", positionStatus = "PASS", qualityStatus = "PASS", coverageStatus = "PASS", reason = "Verified fixture video", evidence = new[] { new { fileId = video, fromMilliseconds = 0, toMilliseconds = 1000 } } } }
        }, submitted.Headers.ETag!.Tag);
        assessed.StatusCode.Should().Be(HttpStatusCode.Created);
        var assessment = (await Body(assessed)).GetProperty("id").GetGuid();
        var assessmentPath = datasetPath + $"/assessments/{assessment}";
        var review = await Read(assessmentPath, true);
        Fields(review, "id", "datasetId", "methodVersion", "reviewedBy", "reviewedAt", "items", "version");
        review.GetProperty("id").GetGuid().Should().Be(assessment);
        review.GetProperty("datasetId").GetGuid().Should().Be(dataset);
        review.GetProperty("reviewedBy").GetGuid().Should().Be(manager.Id);
        review.GetProperty("reviewedAt").GetDateTimeOffset().Should().BeBefore(DateTimeOffset.UtcNow);
        review.GetProperty("methodVersion").GetString().Should().Be("pm-evidence-review.v1");
        var reviewedItem = review.GetProperty("items").EnumerateArray().Single();
        Fields(reviewedItem, "routeVersionId", "segmentSetId", "segmentId", "targetBand", "positionStatus", "qualityStatus", "coverageStatus", "reason", "evidence");
        reviewedItem.GetProperty("segmentId").GetGuid().Should().Be(segment);
        reviewedItem.GetProperty("routeVersionId").GetGuid().Should().Be(route);
        reviewedItem.GetProperty("segmentSetId").GetGuid().Should().Be(set);
        reviewedItem.GetProperty("positionStatus").GetString().Should().Be("PASS");
        var assessedCoverage = await Read(coveragePath, false);
        assessedCoverage.GetProperty("assessmentId").GetGuid().Should().Be(assessment);
        assessedCoverage.GetProperty("version").GetString().Should().Be(review.GetProperty("version").GetString());
        var surface = assessedCoverage.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("scope").GetProperty("targetBand").GetString() == "SURFACE");
        surface.GetProperty("overallCoverage").GetString().Should().Be("PASS");
        var edge = assessedCoverage.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("scope").GetProperty("targetBand").GetString() == "RIGHT_EDGE");
        edge.GetProperty("overallCoverage").GetString().Should().Be("UNKNOWN");
        var original = review.GetRawText();
        var originalVersion = review.GetProperty("version").GetString();
        await using (var db = sql.CreateDbContext())
        {
            var persisted = await db.Set<DatasetAssessment>().AsNoTracking().SingleAsync(a => a.Id == assessment);
            Convert.ToBase64String(persisted.RowVersion).Should().Be(originalVersion);
        }
        var replacement = await Command(client, datasetPath + "/assessments", new
        {
            methodVersion = "pm-evidence-review.v1",
            items = new[] { new { routeVersionId = route, segmentSetId = set, segmentId = segment, targetBand = "SURFACE", positionStatus = "FAIL", qualityStatus = "UNKNOWN", coverageStatus = "FAIL", reason = "Second review finds insufficient coverage", evidence = Array.Empty<object>() } }
        }, submitted.Headers.ETag!.Tag);
        replacement.StatusCode.Should().Be(HttpStatusCode.Created);
        var replacementId = (await Body(replacement)).GetProperty("id").GetGuid();
        replacementId.Should().NotBe(assessment);
        var replacementPath = datasetPath + $"/assessments/{replacementId}";
        var current = await Read(replacementPath, true);
        current.GetProperty("id").GetGuid().Should().Be(replacementId);
        current.GetProperty("items")[0].GetProperty("positionStatus").GetString().Should().Be("FAIL");
        current.GetProperty("items")[0].GetProperty("qualityStatus").GetString().Should().Be("UNKNOWN");
        var latest = await Read(coveragePath, false);
        latest.GetProperty("assessmentId").GetGuid().Should().Be(replacementId);
        latest.GetProperty("version").GetString().Should().Be(current.GetProperty("version").GetString());
        latest.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("scope").GetProperty("targetBand").GetString() == "SURFACE").GetProperty("overallCoverage").GetString().Should().Be("FAIL");
        (await Read(assessmentPath, true)).GetRawText().Should().Be(original);
        (await Read(datasetPath, true)).GetRawText().Should().Be(projection.GetRawText());
        await using (var db = sql.CreateDbContext())
        {
            var rows = await db.Set<DatasetAssessment>().AsNoTracking().Where(a => a.DatasetId == dataset).ToListAsync();
            rows.Should().HaveCount(2);
            Convert.ToBase64String(rows.Single(a => a.Id == assessment).RowVersion).Should().Be(originalVersion);
            Convert.ToBase64String(rows.Single(a => a.Id == replacementId).RowVersion).Should().Be(current.GetProperty("version").GetString());
            rows.Single(a => a.Id == replacementId).ReviewedAt.Should().BeOnOrAfter(rows.Single(a => a.Id == assessment).ReviewedAt);
        }
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
