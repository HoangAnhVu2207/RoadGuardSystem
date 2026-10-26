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
public sealed class BaselineCommandAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task GenuineBaselineSelectionCommandPreservesWireReplayAndAuthority()
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
        var item = new RoadGuardSystem.DTOs.Surveys.BaselineItemDto(route, set, segment, "SURFACE", dataset, assessment, null);
        var request = new RoadGuardSystem.DTOs.Surveys.CreateBaselineDto([item], "Initial baseline");
        var key = Guid.NewGuid().ToString("N");
        async Task<string> Snapshot()
        {
            await using var db = sql.CreateDbContext();
            return JsonSerializer.Serialize(new
            {
                selections = await db.Set<BaselineSelection>().AsNoTracking().Where(b => b.ProjectId == project).OrderBy(b => b.Id).Select(b => new { b.Id, b.RowVersion, b.Reason }).ToListAsync(),
                entries = await (from i in db.Set<BaselineSelectionItem>().AsNoTracking() join b in db.Set<BaselineSelection>() on i.BaselineSelectionId equals b.Id where b.ProjectId == project orderby i.Id select new { i.Id, i.DatasetId, i.AssessmentId }).ToListAsync(),
                pointers = await db.Set<BaselineCurrentPointer>().AsNoTracking().Where(b => b.ProjectId == project).OrderBy(b => b.Id).Select(b => new { b.Id, b.SelectionId, b.RowVersion }).ToListAsync(),
                receipts = await db.IdempotencyRecords.AsNoTracking().Where(r => r.ProjectId == project && r.Operation == "BaselineSelected").OrderBy(r => r.Id).ToListAsync(),
                audits = await db.AuditLogs.AsNoTracking().Where(a => a.ActorUserId == manager.Id && a.EventType == "baseline_selected").OrderBy(a => a.Id).ToListAsync()
            });
        }
        var before = await Snapshot();
        async Task CheckDenied(HttpResponseMessage response, HttpStatusCode status, string code)
        {
            using (response)
            {
                response.StatusCode.Should().Be(status);
                response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
                var problem = await Body(response); problem.GetProperty("status").GetInt32().Should().Be((int)status);
                problem.GetProperty("code").GetString().Should().Be(code);
                Guid.TryParse(response.Headers.GetValues("X-Correlation-ID").Single(), out _).Should().BeTrue();
                response.Headers.ETag.Should().BeNull(); response.Headers.Location.Should().BeNull();
                (await Snapshot()).Should().Be(before);
            }
        }
        async Task Denied(object body, HttpStatusCode status, string code, string? commandKey = null, string? url = null)
            => await CheckDenied(await Command(client, url ?? path, body, key: commandKey), status, code);
        var credential = client.DefaultRequestHeaders.Authorization;
        var sid = Guid.Parse(new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(credential!.Parameter).Claims.Single(c => c.Type == "sid").Value);
        client.DefaultRequestHeaders.Authorization = null; await Denied(request, HttpStatusCode.Unauthorized, "auth_unauthorized");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "tampered"); await Denied(request, HttpStatusCode.Unauthorized, "auth_unauthorized");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new AccessTokenFactory(factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value).Create(manager.Id, sid, UserRoleCode.ProjectManager, DateTimeOffset.UtcNow.AddMinutes(-20), 15));
        await Denied(request, HttpStatusCode.Unauthorized, "auth_unauthorized"); client.DefaultRequestHeaders.Authorization = credential;
        foreach (var role in new[] { UserRoleCode.Supervisor, UserRoleCode.DroneOperator, UserRoleCode.Reporter, UserRoleCode.RepairCrew, UserRoleCode.ProjectManager })
        {
            var outsider = await sql.CreateUserAsync("baseline-command-" + Guid.NewGuid().ToString("N"), "Current1!", role);
            if (role == UserRoleCode.ProjectManager)
            {
                await Login(client, supervisor.UserName!);
                using var other = await client.PostAsJsonAsync("/api/v1/projects", new { projectCode = "BASE-CMD-" + Guid.NewGuid().ToString("N"), name = "Other authority", engineeringUtmSrid = 32648, startDate = "2026-09-01", endDate = "2027-09-01", primaryProjectManagerUserId = outsider.Id, handover = new { documentNo = "DEMO", handoverDate = "2026-08-31" }, operationId = Guid.NewGuid() });
                other.StatusCode.Should().Be(HttpStatusCode.Created);
            }
            await Login(client, outsider.UserName!); await Denied(request, HttpStatusCode.Forbidden, "access_forbidden");
        }
        await Login(client, manager.UserName!);
        foreach (var bad in new[] {
            request with { Items = [] }, request with { Items = [null!] }, request with { Items = [item, item] },
            request with { Reason = " " }, request with { Items = [item with { DatasetId = Guid.Empty }] },
            request with { Items = [item with { AssessmentId = Guid.Empty }] }, request with { Items = [item with { SegmentId = Guid.Empty }] },
            request with { Items = [item with { RouteVersionId = Guid.Empty }] }, request with { Items = [item with { SegmentSetId = Guid.Empty }] },
            request with { Items = [item with { TargetBand = "OTHER" }] }
        }) await Denied(bad, HttpStatusCode.UnprocessableEntity, "baseline_not_eligible");
        await Denied(request with { Items = null! }, HttpStatusCode.BadRequest, "validation_error");
        await Denied(request with { Reason = null! }, HttpStatusCode.BadRequest, "validation_error");
        foreach (var bad in new[] { item with { DatasetId = Guid.NewGuid() }, item with { AssessmentId = Guid.NewGuid() } })
            await Denied(request with { Items = [bad] }, HttpStatusCode.NotFound, "not_found");
        foreach (var bad in new[] { item with { RouteVersionId = Guid.NewGuid() }, item with { SegmentSetId = Guid.NewGuid() }, item with { SegmentId = Guid.NewGuid() }, item with { TargetBand = "RIGHT_EDGE" } })
            await Denied(request with { Items = [bad] }, HttpStatusCode.UnprocessableEntity, "baseline_not_eligible");
        await Denied(request with { Items = [item with { ExpectedBaselineSelectionId = Guid.NewGuid() }] }, HttpStatusCode.Conflict, "baseline_selection_conflict");
        using (var missingKey = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(request) })
            await CheckDenied(await client.SendAsync(missingKey), (HttpStatusCode)428, "validation_error");
        using (var malformed = new HttpRequestMessage(HttpMethod.Post, path) { Content = new StringContent("{", System.Text.Encoding.UTF8, "application/json") })
        { malformed.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N")); await CheckDenied(await client.SendAsync(malformed), HttpStatusCode.BadRequest, "validation_error"); }
        using var selected = await Command(client, path, request, key: key);
        selected.StatusCode.Should().Be(HttpStatusCode.Created);
        var view = await Body(selected); var selectionId = view.GetProperty("id").GetGuid();
        var entry = view.GetProperty("items").EnumerateArray().Single(); var entryId = entry.GetProperty("selectionId").GetGuid();
        view.EnumerateObject().Select(p => p.Name).Order().Should().Equal(new[] { "id", "items", "selectedBy", "selectedAt", "version" }.Order());
        entry.EnumerateObject().Select(p => p.Name).Order().Should().Equal(new[] { "selectionId", "routeVersionId", "segmentSetId", "segmentId", "targetBand", "datasetId", "assessmentId" }.Order());
        entryId.Should().NotBeEmpty(); entry.GetProperty("routeVersionId").GetGuid().Should().Be(route); entry.GetProperty("segmentSetId").GetGuid().Should().Be(set);
        entry.GetProperty("segmentId").GetGuid().Should().Be(segment); entry.GetProperty("targetBand").GetString().Should().Be("SURFACE");
        entry.GetProperty("datasetId").GetGuid().Should().Be(dataset); entry.GetProperty("assessmentId").GetGuid().Should().Be(assessment);
        view.GetProperty("selectedBy").GetGuid().Should().Be(manager.Id); view.GetProperty("selectedAt").GetDateTimeOffset().Should().BeBefore(DateTimeOffset.UtcNow);
        selected.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        selected.Headers.ETag!.Tag.Should().Be('"' + view.GetProperty("version").GetString() + '"');
        selected.Headers.Location!.OriginalString.Should().Be(path + "/" + selectionId);
        var correlation = Guid.Parse(selected.Headers.GetValues("X-Correlation-ID").Single());
        foreach (var secret in new[] { "storageUri", "storageKey", "objectKey", "email", "phoneNumber", "password", "accessToken", "evidence", "https://storage.test" }) view.GetRawText().Should().NotContain(secret);
        await using (var db = sql.CreateDbContext())
        {
            var row = await db.Set<BaselineSelection>().SingleAsync(b => b.ProjectId == project);
            row.Id.Should().Be(selectionId); row.SelectedBy.Should().Be(manager.Id); row.Reason.Should().Be(request.Reason);
            Convert.ToBase64String(row.RowVersion).Should().Be(view.GetProperty("version").GetString());
            (await db.Set<BaselineSelectionItem>().CountAsync(b => b.BaselineSelectionId == selectionId)).Should().Be(1);
            (await db.Set<BaselineCurrentPointer>().SingleAsync(b => b.ProjectId == project)).SelectionId.Should().Be(entryId);
            var receipt = await db.IdempotencyRecords.SingleAsync(r => r.ProjectId == project && r.Operation == "BaselineSelected");
            receipt.ActorUserId.Should().Be(manager.Id); receipt.IdempotencyKey.Should().Be(key); receipt.OperationId.Should().Be(selectionId);
            var audit = await db.AuditLogs.SingleAsync(a => a.EntityId == selectionId && a.EventType == "baseline_selected");
            audit.ActorUserId.Should().Be(manager.Id); audit.EntityType.Should().Be("SurveyAssessment"); audit.CorrelationId.Should().Be(correlation);
            audit.BeforeSnapshot.Should().BeNull(); audit.AfterSnapshot.Should().Be("{}"); audit.Source.Should().Be("anh01"); audit.Reason.Should().Be("Manual survey evidence review");
        }
        before = await Snapshot();
        using var replay = await Command(client, path, request, key: key); replay.StatusCode.Should().Be(HttpStatusCode.Created);
        (await Body(replay)).GetRawText().Should().Be(view.GetRawText()); replay.Headers.ETag.Should().Be(selected.Headers.ETag); replay.Headers.Location.Should().Be(selected.Headers.Location);
        (await Snapshot()).Should().Be(before);
        await Denied(request with { Reason = "changed" }, HttpStatusCode.Conflict, "duplicate_request", key);
        await Denied(request, HttpStatusCode.Conflict, "baseline_selection_conflict");
        // Replacement names the entry pointer, not the batch ID; old history remains immutable.
        await Denied(request with { Items = [item with { ExpectedBaselineSelectionId = selectionId }] }, HttpStatusCode.Conflict, "baseline_selection_conflict");
        using var replaced = await Command(client, path, request with { Items = [item with { ExpectedBaselineSelectionId = entryId }], Reason = "Replace baseline" });
        replaced.StatusCode.Should().Be(HttpStatusCode.Created); var replacement = await Body(replaced);
        replacement.GetProperty("id").GetGuid().Should().NotBe(selectionId);
        var current = await client.GetAsync(path); current.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Body(current)).GetProperty("items")[0].GetProperty("selectionId").GetGuid().Should().Be(replacement.GetProperty("items")[0].GetProperty("selectionId").GetGuid());
        var old = await client.GetAsync(selected.Headers.Location); old.StatusCode.Should().Be(HttpStatusCode.OK); (await Body(old)).GetRawText().Should().Be(view.GetRawText());
        before = await Snapshot();
        using var oldReplay = await Command(client, path, request, key: key); oldReplay.StatusCode.Should().Be(HttpStatusCode.Created); (await Body(oldReplay)).GetRawText().Should().Be(view.GetRawText());
        (await Snapshot()).Should().Be(before);
        await using (var db = sql.CreateDbContext()) { var member = await db.ProjectMembers.SingleAsync(m => m.ProjectId == project && m.UserId == manager.Id); member.Status = ProjectMemberStatus.Ended; await db.SaveChangesAsync(); }
        await Denied(request, HttpStatusCode.Forbidden, "access_forbidden", key); await Denied(request, HttpStatusCode.Forbidden, "access_forbidden");
        var token = client.DefaultRequestHeaders.Authorization;
        var currentSid = Guid.Parse(new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(token!.Parameter).Claims.Single(c => c.Type == "sid").Value);
        await using (var db = sql.CreateDbContext()) await db.Sessions.Where(s => s.Id == currentSid).ExecuteUpdateAsync(s => s.SetProperty(v => v.RevokedAt, DateTimeOffset.UtcNow));
        await Denied(request, HttpStatusCode.Unauthorized, "auth_session_revoked", key);
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
