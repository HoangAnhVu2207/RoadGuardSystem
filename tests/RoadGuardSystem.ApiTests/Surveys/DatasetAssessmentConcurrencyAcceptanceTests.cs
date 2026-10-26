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
public sealed class DatasetAssessmentConcurrencyAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task GenuineAssessmentConcurrencyAndAssignmentRevocation(bool sameKey, bool changedCommand)
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


        var path = $"/api/v1/datasets/{dataset}/assessments";
        var version = submitted.Headers.ETag!.Tag;
        var item = new RoadGuardSystem.DTOs.Surveys.AssessmentItemDto(route, set, segment, "SURFACE", "PASS", "PASS", "PASS", "Concurrent review", [new(video, 0, 1000)]);
        var request = new RoadGuardSystem.DTOs.Surveys.CreateAssessmentDto("pm-evidence-review.v1", [item]);
        var leftKey = Guid.NewGuid().ToString("N"); var rightKey = sameKey ? leftKey : Guid.NewGuid().ToString("N");
        var changed = request with { Items = [item with { Reason = "Different concurrent review" }] };
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<HttpResponseMessage> Send(string key, bool different) { await start.Task; return await Command(client, path, different ? changed : request, version, key); }
        var left = Send(leftKey, false); var right = Send(rightKey, changedCommand); start.SetResult();
        var responses = await Task.WhenAll(left, right);
        try
        {
            var successes = responses.Where(r => r.StatusCode == HttpStatusCode.Created).ToArray();
            var diagnostics = string.Join(";", await Task.WhenAll(responses.Select(async r => ((int)r.StatusCode) + ":" + await r.Content.ReadAsStringAsync())));
            successes.Length.Should().Be(changedCommand ? 1 : 2, "actual responses {0}", diagnostics);
            var ids = new List<Guid>();
            foreach (var response in responses)
            {
                Guid.TryParse(response.Headers.GetValues("X-Correlation-ID").Single(), out _).Should().BeTrue();
                var value = await Body(response);
                if (response.StatusCode == HttpStatusCode.Created)
                {
                    var id = value.GetProperty("id").GetGuid(); ids.Add(id);
                    response.Headers.ETag!.Tag.Should().Be('"' + value.GetProperty("version").GetString() + '"'); response.Headers.Location!.OriginalString.Should().Be(path + "/" + id);
                }
                else
                {
                    response.StatusCode.Should().Be(HttpStatusCode.Conflict); value.GetProperty("code").GetString().Should().Be("duplicate_request"); value.GetProperty("status").GetInt32().Should().Be(409);
                    response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json"); response.Headers.ETag.Should().BeNull(); response.Headers.Location.Should().BeNull();
                }
            }
            ids.Distinct().Count().Should().Be(sameKey ? 1 : 2);
            var winnerIndex = responses[0].StatusCode == HttpStatusCode.Created ? 0 : 1;
            var winnerPayload = winnerIndex == 1 && changedCommand ? changed : request; var winnerKey = winnerIndex == 0 ? leftKey : rightKey;
            using var replay = await Command(client, path, winnerPayload, version, winnerKey); replay.StatusCode.Should().Be(HttpStatusCode.Created);
            (await Body(replay)).GetRawText().Should().Be((await Body(responses[winnerIndex])).GetRawText());
            async Task<string> Snapshot()
            {
                await using var db = sql.CreateDbContext();
                return JsonSerializer.Serialize(new
                {
                    assessments = await db.Set<DatasetAssessment>().Where(a => a.DatasetId == dataset).OrderBy(a => a.Id).Select(a => new { a.Id, a.RowVersion }).ToArrayAsync(),
                    receipts = await db.IdempotencyRecords.Where(r => r.ProjectId == project && r.Operation == "DatasetAssessmentCreated").OrderBy(r => r.Id).Select(r => r.Id).ToArrayAsync(),
                    audits = await db.AuditLogs.Where(a => a.ActorUserId == manager.Id && a.EventType == "dataset_assessment_created").OrderBy(a => a.Id).Select(a => a.Id).ToArrayAsync()
                });
            }
            await using (var db = sql.CreateDbContext())
            {
                var count = sameKey ? 1 : 2;
                (await db.Set<DatasetAssessment>().CountAsync(a => a.DatasetId == dataset)).Should().Be(count);
                (await db.IdempotencyRecords.CountAsync(r => r.ProjectId == project && r.Operation == "DatasetAssessmentCreated")).Should().Be(count);
                (await db.AuditLogs.CountAsync(a => a.ActorUserId == manager.Id && a.EventType == "dataset_assessment_created")).Should().Be(count);
                foreach (var id in ids.Distinct())
                {
                    (await db.Set<DatasetAssessmentItem>().CountAsync(i => i.AssessmentId == id)).Should().Be(1);
                    var receipt = await db.IdempotencyRecords.SingleAsync(r => r.ProjectId == project && r.OperationId == id && r.Operation == "DatasetAssessmentCreated"); receipt.ActorUserId.Should().Be(manager.Id);
                    (await db.AuditLogs.SingleAsync(a => a.EntityId == id && a.EventType == "dataset_assessment_created")).ActorUserId.Should().Be(manager.Id);
                }
                var source = await db.SurveyDataVersions.SingleAsync(d => d.Id == dataset); ('"' + Convert.ToBase64String(source.RowVersion) + '"').Should().Be(version);
                await db.SurveyAssignments.Where(a => a.SurveyRequestId == task && a.EndedAt == null).ExecuteUpdateAsync(u => u.SetProperty(a => a.EndedAt, DateTimeOffset.UtcNow));
            }
            var before = await Snapshot();
            foreach (var key in new[] { winnerKey, Guid.NewGuid().ToString("N") })
            {
                using var denied = await Command(client, path, winnerPayload, version, key); denied.StatusCode.Should().Be(HttpStatusCode.NotFound);
                var error = await Body(denied); error.GetProperty("code").GetString().Should().Be("not_found"); error.GetProperty("status").GetInt32().Should().Be(404);
                denied.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json"); denied.Headers.ETag.Should().BeNull(); denied.Headers.Location.Should().BeNull();
                Guid.TryParse(denied.Headers.GetValues("X-Correlation-ID").Single(), out _).Should().BeTrue(); (await Snapshot()).Should().Be(before);
            }
        }
        finally { foreach (var response in responses) response.Dispose(); }
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
