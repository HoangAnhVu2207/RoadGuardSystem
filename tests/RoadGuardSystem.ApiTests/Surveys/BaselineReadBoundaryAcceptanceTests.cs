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
public sealed class BaselineReadBoundaryAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task GenuineBaselineSelectionReadsPreserveSnapshotAndCurrentAuthority()
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


        var assessed = await Command(client, $"/api/v1/datasets/{dataset}/assessments", new
        {
            methodVersion = "pm-evidence-review.v1",
            items = new[] { new { routeVersionId = route, segmentSetId = set, segmentId = segment, targetBand = "SURFACE", positionStatus = "PASS", qualityStatus = "PASS", coverageStatus = "PASS", reason = "Verified video", evidence = new[] { new { fileId = video, fromMilliseconds = 0, toMilliseconds = 1000 } } } }
        }, submitted.Headers.ETag!.Tag);
        assessed.StatusCode.Should().Be(HttpStatusCode.Created);
        var assessment = (await Body(assessed)).GetProperty("id").GetGuid();
        var path = $"/api/v1/projects/{project}/baseline-selections";
        var selectionBody = new { items = new[] { new { routeVersionId = route, segmentSetId = set, segmentId = segment, targetBand = "SURFACE", datasetId = dataset, assessmentId = assessment, expectedBaselineSelectionId = (Guid?)null } }, reason = "Initial baseline" };
        var selected = await Command(client, path, selectionBody);
        selected.StatusCode.Should().Be(HttpStatusCode.Created);
        var historyPath = selected.Headers.Location!.OriginalString;
        var selectedView = await Body(selected);
        var selectionId = selectedView.GetProperty("id").GetGuid();
        static void Fields(JsonElement value, params string[] expected) => value.EnumerateObject().Select(p => p.Name).Order().Should().Equal(expected.Order());
        void Item(JsonElement value)
        {
            Fields(value, "selectionId", "routeVersionId", "segmentSetId", "segmentId", "targetBand", "datasetId", "assessmentId");
            value.GetProperty("routeVersionId").GetGuid().Should().Be(route); value.GetProperty("segmentSetId").GetGuid().Should().Be(set);
            value.GetProperty("segmentId").GetGuid().Should().Be(segment); value.GetProperty("targetBand").GetString().Should().Be("SURFACE");
            value.GetProperty("datasetId").GetGuid().Should().Be(dataset); value.GetProperty("assessmentId").GetGuid().Should().Be(assessment);
            value.GetProperty("selectionId").GetGuid().Should().Be(selectedView.GetProperty("items")[0].GetProperty("selectionId").GetGuid());
        }
        async Task<JsonElement> Read(string url)
        {
            using var response = await client.GetAsync(url);
            response.StatusCode.Should().Be(HttpStatusCode.OK); response.Headers.ETag.Should().BeNull(); response.Headers.Location.Should().BeNull();
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
            Guid.TryParse(response.Headers.GetValues("X-Correlation-ID").Single(), out _).Should().BeTrue();
            var value = await Body(response);
            foreach (var secret in new[] { "storageUri", "storageKey", "objectKey", "email", "phoneNumber", "password", "accessToken", "evidence", "https://storage.test" }) value.GetRawText().Should().NotContain(secret);
            return value;
        }
        var current = await Read(path); Fields(current, "items"); Item(current.GetProperty("items").EnumerateArray().Single());
        var history = await Read(historyPath); Fields(history, "id", "items", "selectedBy", "selectedAt", "version");
        history.GetProperty("id").GetGuid().Should().Be(selectionId); history.GetProperty("selectedBy").GetGuid().Should().Be(manager.Id);
        history.GetProperty("selectedAt").GetDateTimeOffset().Should().BeBefore(DateTimeOffset.UtcNow); Item(history.GetProperty("items").EnumerateArray().Single());
        await using (var db = sql.CreateDbContext())
        {
            var row = await db.Set<BaselineSelection>().AsNoTracking().SingleAsync(b => b.Id == selectionId);
            Convert.ToBase64String(row.RowVersion).Should().Be(history.GetProperty("version").GetString());
            var pointer = await db.Set<BaselineCurrentPointer>().AsNoTracking().SingleAsync(b => b.ProjectId == project);
            pointer.SelectionId.Should().Be(current.GetProperty("items")[0].GetProperty("selectionId").GetGuid());
        }
        var filtered = await Read(path + $"?segmentSetId={set}"); filtered.GetRawText().Should().Be(current.GetRawText());
        var empty = await Read(path + $"?segmentSetId={Guid.NewGuid()}"); Fields(empty, "items"); empty.GetProperty("items").GetArrayLength().Should().Be(0);
        var paths = new[] { path, historyPath };
        async Task<string> Snapshot()
        {
            await using var db = sql.CreateDbContext();
            return JsonSerializer.Serialize(new
            {
                selections = await db.Set<BaselineSelection>().AsNoTracking().Where(b => b.ProjectId == project).OrderBy(b => b.Id).Select(b => new { b.Id, b.RowVersion, b.Reason }).ToListAsync(),
                entries = await (from i in db.Set<BaselineSelectionItem>().AsNoTracking() join b in db.Set<BaselineSelection>() on i.BaselineSelectionId equals b.Id where b.ProjectId == project orderby i.Id select new { i.Id, i.DatasetId, i.AssessmentId }).ToListAsync(),
                pointers = await db.Set<BaselineCurrentPointer>().AsNoTracking().Where(b => b.ProjectId == project).OrderBy(b => b.Id).Select(b => new { b.Id, b.SelectionId, b.RowVersion }).ToListAsync(),
                receipts = await db.IdempotencyRecords.Where(r => r.ProjectId == project).OrderBy(r => r.Id).Select(r => r.Id).ToListAsync(),
                audits = await db.AuditLogs.Where(a => a.EntityId == selectionId).OrderBy(a => a.Id).Select(a => a.Id).ToListAsync()
            });
        }
        var before = await Snapshot();
        async Task Denied(string url, HttpStatusCode expected, string code)
        {
            using var response = await client.GetAsync(url); response.StatusCode.Should().Be(expected);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
            var problem = await Body(response); problem.GetProperty("status").GetInt32().Should().Be((int)expected); problem.GetProperty("code").GetString().Should().Be(code);
            response.Headers.ETag.Should().BeNull(); response.Headers.Location.Should().BeNull(); Guid.TryParse(response.Headers.GetValues("X-Correlation-ID").Single(), out _).Should().BeTrue();
            (await Snapshot()).Should().Be(before);
        }
        foreach (var actor in new[] { manager, supervisor })
        {
            await Login(client, actor.UserName!);
            (await Read(path)).GetRawText().Should().Be(current.GetRawText()); (await Read(historyPath)).GetRawText().Should().Be(history.GetRawText());
        }
        foreach (var role in new[] { UserRoleCode.DroneOperator, UserRoleCode.Reporter, UserRoleCode.RepairCrew, UserRoleCode.ProjectManager })
        {
            var outsider = await sql.CreateUserAsync("baseline-outsider-" + Guid.NewGuid().ToString("N"), "Current1!", role);
            if (role == UserRoleCode.ProjectManager)
            {
                await Login(client, supervisor.UserName!);
                using var other = await client.PostAsJsonAsync("/api/v1/projects", new { projectCode = "BASE-OTHER-" + Guid.NewGuid().ToString("N"), name = "Other authority", engineeringUtmSrid = 32648, startDate = "2026-09-01", endDate = "2027-09-01", primaryProjectManagerUserId = outsider.Id, handover = new { documentNo = "DEMO", handoverDate = "2026-08-31" }, operationId = Guid.NewGuid() });
                other.StatusCode.Should().Be(HttpStatusCode.Created);
            }
            await Login(client, outsider.UserName!); foreach (var url in paths) await Denied(url, HttpStatusCode.Forbidden, "access_forbidden");
        }
        await Login(client, manager.UserName!);
        await Denied(path + "/" + Guid.NewGuid(), HttpStatusCode.NotFound, "not_found");
        await Denied(path + "/" + Guid.Empty, HttpStatusCode.NotFound, "not_found");
        await Denied(path + "?segmentSetId=malformed", HttpStatusCode.BadRequest, "validation_error");
        using (var malformed = await client.GetAsync(path + "/malformed")) malformed.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var credential = client.DefaultRequestHeaders.Authorization;
        var sid = Guid.Parse(new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(credential!.Parameter).Claims.Single(c => c.Type == "sid").Value);
        client.DefaultRequestHeaders.Authorization = null; foreach (var url in paths) await Denied(url, HttpStatusCode.Unauthorized, "auth_unauthorized");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "tampered"); foreach (var url in paths) await Denied(url, HttpStatusCode.Unauthorized, "auth_unauthorized");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new AccessTokenFactory(factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value).Create(manager.Id, sid, UserRoleCode.ProjectManager, DateTimeOffset.UtcNow.AddMinutes(-20), 15));
        foreach (var url in paths) await Denied(url, HttpStatusCode.Unauthorized, "auth_unauthorized");
        client.DefaultRequestHeaders.Authorization = credential;
        await using (var db = sql.CreateDbContext()) await db.Sessions.Where(s => s.Id == sid).ExecuteUpdateAsync(s => s.SetProperty(v => v.RevokedAt, DateTimeOffset.UtcNow));
        foreach (var url in paths) await Denied(url, HttpStatusCode.Unauthorized, "auth_session_revoked");
        await Login(client, manager.UserName!);
        await using (var db = sql.CreateDbContext())
        { var membership = await db.ProjectMembers.SingleAsync(m => m.ProjectId == project && m.UserId == manager.Id); membership.Status = ProjectMemberStatus.Ended; await db.SaveChangesAsync(); }
        foreach (var url in paths) await Denied(url, HttpStatusCode.Forbidden, "access_forbidden");
        (await Snapshot()).Should().Be(before);
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
