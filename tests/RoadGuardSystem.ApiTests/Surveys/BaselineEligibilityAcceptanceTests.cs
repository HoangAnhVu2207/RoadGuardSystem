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
public sealed class BaselineEligibilityAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task GenuineBaselineEligibilityRequiresEveryAssessmentStatusAndScope()
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


        var path = $"/api/v1/projects/{project}/baseline-selections";
        async Task<string> Snapshot()
        {
            await using var db = sql.CreateDbContext();
            return JsonSerializer.Serialize(new
            {
                selections = await db.Set<BaselineSelection>().Where(x => x.ProjectId == project).OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync(),
                entries = await (from i in db.Set<BaselineSelectionItem>() join b in db.Set<BaselineSelection>() on i.BaselineSelectionId equals b.Id where b.ProjectId == project orderby i.Id select i.Id).ToArrayAsync(),
                pointers = await db.Set<BaselineCurrentPointer>().Where(x => x.ProjectId == project).OrderBy(x => x.Id).Select(x => new { x.Id, x.SelectionId, x.RowVersion }).ToArrayAsync(),
                receipts = await db.IdempotencyRecords.Where(x => x.ProjectId == project && x.Operation == "BaselineSelected").OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync(),
                audits = await db.AuditLogs.Where(x => x.ActorUserId == manager.Id && x.EventType == "baseline_selected").OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync()
            });
        }
        async Task<Guid> Assess(string position, string quality, string coverage, string band = "SURFACE")
        {
            using var response = await Command(client, $"/api/v1/datasets/{dataset}/assessments", new
            {
                methodVersion = "pm-evidence-review.v1",
                items = new[] { new { routeVersionId = route, segmentSetId = set, segmentId = segment, targetBand = band, positionStatus = position, qualityStatus = quality, coverageStatus = coverage, reason = "Eligibility acceptance", evidence = new[] { new { fileId = video, fromMilliseconds = 0, toMilliseconds = 1000 } } } }
            }, submitted.Headers.ETag!.Tag);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            return (await Body(response)).GetProperty("id").GetGuid();
        }
        async Task Reject(object payload, HttpStatusCode status, string code)
        {
            var before = await Snapshot();
            using var response = await Command(client, path, payload);
            response.StatusCode.Should().Be(status); response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
            var error = await Body(response); error.GetProperty("status").GetInt32().Should().Be((int)status); error.GetProperty("code").GetString().Should().Be(code);
            Guid.TryParse(response.Headers.GetValues("X-Correlation-ID").Single(), out _).Should().BeTrue(); response.Headers.ETag.Should().BeNull(); response.Headers.Location.Should().BeNull();
            (await Snapshot()).Should().Be(before);
        }
        object Item(Guid assessment, string band = "SURFACE") => new { routeVersionId = route, segmentSetId = set, segmentId = segment, targetBand = band, datasetId = dataset, assessmentId = assessment, expectedBaselineSelectionId = (Guid?)null };
        var passing = await Assess("PASS", "PASS", "PASS");
        foreach (var dimension in new[] { 0, 1, 2 })
            foreach (var nonPassing in new[] { "FAIL", "UNKNOWN" })
            {
                var statuses = new[] { "PASS", "PASS", "PASS" }; statuses[dimension] = nonPassing;
                var ineligible = await Assess(statuses[0], statuses[1], statuses[2]);
                await Reject(new { items = new[] { Item(ineligible) }, reason = "Reject non-PASS" }, HttpStatusCode.UnprocessableEntity, "baseline_not_eligible");
                // A valid earlier item must not survive failure of a later item in the same transaction.
                await Reject(new { items = new[] { Item(passing), Item(ineligible, "RIGHT_EDGE") }, reason = "Reject partial commit" }, HttpStatusCode.UnprocessableEntity, "baseline_not_eligible");
            }
        var edge = await Assess("PASS", "PASS", "PASS", "RIGHT_EDGE");
        await Reject(new { items = new[] { Item(edge) }, reason = "Assessment is for another band" }, HttpStatusCode.UnprocessableEntity, "baseline_not_eligible");
        using var selected = await Command(client, path, new { items = new[] { Item(passing), Item(edge, "RIGHT_EDGE") }, reason = "All selected items eligible" });
        selected.StatusCode.Should().Be(HttpStatusCode.Created);
        var value = await Body(selected); var id = value.GetProperty("id").GetGuid(); value.GetProperty("items").GetArrayLength().Should().Be(2);
        await using (var db = sql.CreateDbContext())
        {
            (await db.Set<BaselineSelection>().CountAsync(x => x.ProjectId == project)).Should().Be(1);
            (await db.Set<BaselineSelectionItem>().CountAsync(x => x.BaselineSelectionId == id)).Should().Be(2);
            (await db.Set<BaselineCurrentPointer>().CountAsync(x => x.ProjectId == project)).Should().Be(2);
            (await db.IdempotencyRecords.CountAsync(x => x.ProjectId == project && x.Operation == "BaselineSelected")).Should().Be(1);
            (await db.AuditLogs.CountAsync(x => x.EntityId == id && x.EventType == "baseline_selected")).Should().Be(1);
        }
        using var history = await client.GetAsync(selected.Headers.Location); history.StatusCode.Should().Be(HttpStatusCode.OK);
        // No cross-route item ordering is specified; preserve every field and duplicate.
        var historical = await Body(history);
        historical.EnumerateObject().Where(p => p.Name != "items").ToDictionary(p => p.Name, p => p.Value.GetRawText())
            .Should().BeEquivalentTo(value.EnumerateObject().Where(p => p.Name != "items").ToDictionary(p => p.Name, p => p.Value.GetRawText()));
        historical.GetProperty("items").EnumerateArray().Select(p => p.GetRawText()).OrderBy(p => p, StringComparer.Ordinal)
            .Should().Equal(value.GetProperty("items").EnumerateArray().Select(p => p.GetRawText()).OrderBy(p => p, StringComparer.Ordinal));
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
