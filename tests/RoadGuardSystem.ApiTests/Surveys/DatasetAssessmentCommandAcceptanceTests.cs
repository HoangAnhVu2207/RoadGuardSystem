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
public sealed class DatasetAssessmentCommandAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task GenuineAssessmentCommandPreservesWireAuthorityAndReplay()
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
        var item = new RoadGuardSystem.DTOs.Surveys.AssessmentItemDto(route, set, segment, "SURFACE", "PASS", "PASS", "PASS", "Verified source review", [new(video, 0, 1000)]);
        var request = new RoadGuardSystem.DTOs.Surveys.CreateAssessmentDto("pm-evidence-review.v1", [item]);
        var key = Guid.NewGuid().ToString("N");
        async Task<string> Snapshot()
        {
            await using var db = sql.CreateDbContext();
            return JsonSerializer.Serialize(new
            {
                assessments = await db.Set<DatasetAssessment>().AsNoTracking().Where(a => a.DatasetId == dataset).OrderBy(a => a.Id).Select(a => new { a.Id, a.ReviewedBy, a.ReviewedAt, a.RowVersion }).ToListAsync(),
                items = await (from i in db.Set<DatasetAssessmentItem>().AsNoTracking() join a in db.Set<DatasetAssessment>() on i.AssessmentId equals a.Id where a.DatasetId == dataset orderby i.Id select new { i.Id, i.AssessmentId, i.EvidenceJson, i.Reason }).ToListAsync(),
                receipts = await db.Set<RoadGuardSystem.BusinessObjects.Idempotency.IdempotencyRecord>().AsNoTracking().Where(r => r.ProjectId == project && r.Operation == "DatasetAssessmentCreated").OrderBy(r => r.Id).ToListAsync(),
                audits = await db.AuditLogs.AsNoTracking().Where(a => a.ActorUserId == manager.Id && a.EventType == "dataset_assessment_created").OrderBy(a => a.Id).ToListAsync(),
                source = await db.SurveyDataVersions.AsNoTracking().Where(d => d.Id == dataset).Select(d => new { d.RowVersion, d.ScopeManifest, d.SourceManifest }).SingleAsync()
            });
        }
        var before = await Snapshot();
        async Task Denied(object body, HttpStatusCode expected, string code, string? etag = null, string? commandKey = null, string? url = null)
        {
            using var response = await Command(client, url ?? path, body, etag ?? version, commandKey);
            response.StatusCode.Should().Be(expected, "request {0}", JsonSerializer.Serialize(body));
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
            var problem = await Body(response);
            problem.GetProperty("code").GetString().Should().Be(code);
            problem.GetProperty("status").GetInt32().Should().Be((int)expected);
            Guid.TryParse(response.Headers.GetValues("X-Correlation-ID").Single(), out _).Should().BeTrue();
            response.Headers.ETag.Should().BeNull(); response.Headers.Location.Should().BeNull();
            (await Snapshot()).Should().Be(before);
        }
        var credential = client.DefaultRequestHeaders.Authorization;
        var sid = Guid.Parse(new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(credential!.Parameter).Claims.Single(c => c.Type == "sid").Value);
        client.DefaultRequestHeaders.Authorization = null;
        await Denied(request, HttpStatusCode.Unauthorized, "auth_unauthorized");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "tampered");
        await Denied(request, HttpStatusCode.Unauthorized, "auth_unauthorized");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new AccessTokenFactory(factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value).Create(manager.Id, sid, UserRoleCode.ProjectManager, DateTimeOffset.UtcNow.AddMinutes(-20), 15));
        await Denied(request, HttpStatusCode.Unauthorized, "auth_unauthorized");
        client.DefaultRequestHeaders.Authorization = credential;
        // Current authoritative PM can create; another role or current unrelated PM cannot.
        foreach (var role in new[] { UserRoleCode.Supervisor, UserRoleCode.DroneOperator, UserRoleCode.Reporter, UserRoleCode.RepairCrew, UserRoleCode.ProjectManager })
        {
            var outsider = await sql.CreateUserAsync("assessment-outsider-" + Guid.NewGuid().ToString("N"), "Current1!", role);
            if (role == UserRoleCode.ProjectManager)
            {
                await Login(client, supervisor.UserName!);
                using var other = await client.PostAsJsonAsync("/api/v1/projects", new { projectCode = "ASSESS-OTHER-" + Guid.NewGuid().ToString("N"), name = "Other authority", engineeringUtmSrid = 32648, startDate = "2026-09-01", endDate = "2027-09-01", primaryProjectManagerUserId = outsider.Id, handover = new { documentNo = "DEMO", handoverDate = "2026-08-31" }, operationId = Guid.NewGuid() });
                other.StatusCode.Should().Be(HttpStatusCode.Created);
            }
            await Login(client, outsider.UserName!);
            await Denied(request, HttpStatusCode.Forbidden, "access_forbidden");
        }
        await Login(client, manager.UserName!);
        await Denied(request with { Items = [item with { Reason = " " }] }, HttpStatusCode.UnprocessableEntity, "assessment_validation_failed");
        await Denied(request with { MethodVersion = "unapproved" }, HttpStatusCode.UnprocessableEntity, "assessment_method_not_approved");
        foreach (var bad in new[] {
            request with { Items = [] }, request with { Items = [null!] },
            request with { Items = [item, item] }, request with { Items = [item with { RouteVersionId = Guid.Empty }] },
            request with { Items = [item with { SegmentSetId = Guid.Empty }] }, request with { Items = [item with { SegmentId = Guid.Empty }] },
            request with { Items = [item with { TargetBand = "OTHER" }] }, request with { Items = [item with { PositionStatus = "pass" }] },
            request with { Items = [item with { QualityStatus = "bad" }] }, request with { Items = [item with { CoverageStatus = "bad" }] },
            request with { Items = [item with { Evidence = [] }] },
            request with { Items = [item with { Evidence = [null!] }] },
            request with { Items = [item with { Evidence = [new(Guid.Empty)] }] }, request with { Items = [item with { Evidence = [new(video, 1, 1)] }] },
            request with { Items = [item with { Evidence = [new(video, -1, 10)] }] }, request with { Items = [item with { Evidence = [new(video, null, 10)] }] },
            request with { Items = [item with { RouteVersionId = Guid.NewGuid() }] }, request with { Items = [item with { SegmentSetId = Guid.NewGuid() }] },
            request with { Items = [item with { SegmentId = Guid.NewGuid() }] }, request with { Items = [item with { Evidence = [new(Guid.NewGuid())] }] }
        }) await Denied(bad, HttpStatusCode.UnprocessableEntity, "assessment_validation_failed");
        foreach (var bad in new[] { request with { Items = null! }, request with { Items = [item with { Evidence = null! }] } })
            await Denied(bad, HttpStatusCode.BadRequest, "validation_error");
        await Denied(request, HttpStatusCode.PreconditionFailed, "concurrency_conflict", "\"not-base64\"");
        await Denied(request, HttpStatusCode.PreconditionFailed, "concurrency_conflict", "\"AAAAAAAAAAA=\"");
        await Denied(request, HttpStatusCode.NotFound, "not_found", url: $"/api/v1/datasets/{Guid.NewGuid()}/assessments");
        using var createdAssessment = await Command(client, path, request, version, key);
        createdAssessment.StatusCode.Should().Be(HttpStatusCode.Created);
        createdAssessment.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        var view = await Body(createdAssessment);
        view.EnumerateObject().Select(p => p.Name).Order().Should().Equal(new[] { "id", "datasetId", "methodVersion", "reviewedBy", "reviewedAt", "items", "version" }.Order());
        view.GetProperty("datasetId").GetGuid().Should().Be(dataset);
        view.GetProperty("reviewedBy").GetGuid().Should().Be(manager.Id);
        view.GetProperty("methodVersion").GetString().Should().Be("pm-evidence-review.v1");
        view.GetProperty("items").GetArrayLength().Should().Be(1);
        createdAssessment.Headers.ETag!.Tag.Should().Be('"' + view.GetProperty("version").GetString() + '"');
        createdAssessment.Headers.Location!.OriginalString.Should().Be(path + "/" + view.GetProperty("id").GetGuid());
        Guid.TryParse(createdAssessment.Headers.GetValues("X-Correlation-ID").Single(), out _).Should().BeTrue();
        foreach (var secret in new[] { "storageUri", "storageKey", "objectKey", "email", "phoneNumber", "password", "accessToken", "https://storage.test" })
            view.GetRawText().Should().NotContain(secret);
        await using (var db = sql.CreateDbContext())
        {
            var row = await db.Set<DatasetAssessment>().AsNoTracking().SingleAsync(a => a.DatasetId == dataset);
            row.Id.Should().Be(view.GetProperty("id").GetGuid()); row.ReviewedBy.Should().Be(manager.Id);
            Convert.ToBase64String(row.RowVersion).Should().Be(view.GetProperty("version").GetString());
            (await db.Set<DatasetAssessmentItem>().CountAsync(i => i.AssessmentId == row.Id)).Should().Be(1);
            var receipt = await db.Set<RoadGuardSystem.BusinessObjects.Idempotency.IdempotencyRecord>().SingleAsync(r => r.ActorUserId == manager.Id && r.ProjectId == project && r.Operation == "DatasetAssessmentCreated");
            receipt.OperationId.Should().Be(row.Id); receipt.IdempotencyKey.Should().Be(key);
            var audit = await db.AuditLogs.SingleAsync(a => a.ActorUserId == manager.Id && a.EventType == "dataset_assessment_created");
            audit.EntityId.Should().Be(row.Id); audit.CorrelationId.Should().NotBeNull();
        }
        var projected = view.GetProperty("items").EnumerateArray().Single();
        projected.EnumerateObject().Select(p => p.Name).Order().Should().Equal(new[] { "routeVersionId", "segmentSetId", "segmentId", "targetBand", "positionStatus", "qualityStatus", "coverageStatus", "reason", "evidence" }.Order());
        projected.GetProperty("routeVersionId").GetGuid().Should().Be(route); projected.GetProperty("segmentSetId").GetGuid().Should().Be(set);
        projected.GetProperty("segmentId").GetGuid().Should().Be(segment); projected.GetProperty("targetBand").GetString().Should().Be("SURFACE");
        foreach (var dimension in new[] { "positionStatus", "qualityStatus", "coverageStatus" }) projected.GetProperty(dimension).GetString().Should().Be("PASS");
        projected.GetProperty("reason").GetString().Should().Be(item.Reason);
        var evidence = projected.GetProperty("evidence").EnumerateArray().Single();
        evidence.EnumerateObject().Select(p => p.Name).Order().Should().Equal(new[] { "fileId", "fromMilliseconds", "toMilliseconds" }.Order());
        evidence.GetProperty("fileId").GetGuid().Should().Be(video); evidence.GetProperty("fromMilliseconds").GetInt64().Should().Be(0); evidence.GetProperty("toMilliseconds").GetInt64().Should().Be(1000);
        view.GetProperty("reviewedAt").GetDateTimeOffset().Should().BeBefore(DateTimeOffset.UtcNow);
        before = await Snapshot();
        using var replay = await Command(client, path, request, version, key);
        replay.StatusCode.Should().Be(HttpStatusCode.Created);
        (await Body(replay)).GetRawText().Should().Be(view.GetRawText());
        replay.Headers.ETag.Should().Be(createdAssessment.Headers.ETag); replay.Headers.Location.Should().Be(createdAssessment.Headers.Location);
        (await Snapshot()).Should().Be(before);
        await Denied(request with { Items = [item with { Reason = "changed" }] }, HttpStatusCode.Conflict, "duplicate_request", commandKey: key);
        await using (var db = sql.CreateDbContext())
        {
            var membership = await db.ProjectMembers.SingleAsync(m => m.ProjectId == project && m.UserId == manager.Id);
            membership.Status = ProjectMemberStatus.Ended; await db.SaveChangesAsync();
        }
        await Denied(request, HttpStatusCode.Forbidden, "access_forbidden", commandKey: key);
        await Denied(request, HttpStatusCode.Forbidden, "access_forbidden");
        var token = client.DefaultRequestHeaders.Authorization;
        var currentSid = Guid.Parse(new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(token!.Parameter).Claims.Single(c => c.Type == "sid").Value);
        await using (var db = sql.CreateDbContext())
            await db.Sessions.Where(s => s.Id == currentSid).ExecuteUpdateAsync(s => s.SetProperty(v => v.RevokedAt, DateTimeOffset.UtcNow));
        await Denied(request, HttpStatusCode.Unauthorized, "auth_session_revoked", commandKey: key);
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
