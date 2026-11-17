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
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ManualAssessmentPartialBaselineSupplementAndRevocationPersistThroughHttp(bool withAccessPoint)
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
        var staged = await VerifiedProjectHandoverFixture.StageAsync(sql, supervisor.Id);
        client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", staged.ETag);
        var created = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            projectCode = $"ANH-{Guid.NewGuid():N}",
            name = "DEMO survey evidence",
            engineeringUtmSrid = 32648,
            startDate = "2026-09-01",
            endDate = "2027-09-01",
            primaryProjectManagerUserId = manager.Id,
            handover = new { documentNo = "DEMO", handoverDate = "2026-08-31", fileId = staged.FileId },
            operationId = Guid.NewGuid()
        });
        client.DefaultRequestHeaders.IfMatch.Clear();
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
        using var draftSubmission = await RoadGuardSystem.ApiTests.Projects.GeometrySubmissionHttpTests.Submit(client, draft.Headers.Location!.OriginalString, draft.Headers.ETag!);
        await Login(client, supervisor.UserName!);
        var road = await Command(client, draft.Headers.Location!.OriginalString + "/confirm", new
        {
            expectedCurrentVersionId = (Guid?)null,
            effectiveFrom = "2026-10-02T00:00:00Z",
            reason = "Supervisor DEMO approval"
        }, draftSubmission.Headers.ETag!.Tag);
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
        using var pmWeb = await WebCookieTestClient.Login(factory, manager.Email!);
        using var supWeb = await WebCookieTestClient.Login(factory, supervisor.Email!);
        var outside = await sql.CreateUserAsync($"outside-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        using var outsideWeb = await WebCookieTestClient.Login(factory, outside.Email!);
        var pmActions = withAccessPoint ? pmWeb : client;
        async Task CheckReads(string path)
        {
            using var pmRead = await pmWeb.GetAsync(path);
            using var supRead = await supWeb.GetAsync(path);
            using var outsideRead = await outsideWeb.GetAsync(path);
            pmRead.StatusCode.Should().Be(HttpStatusCode.OK);
            supRead.StatusCode.Should().Be(HttpStatusCode.OK);
            outsideRead.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        async Task<HttpResponseMessage> WebMutation(string path, object input, string? version = null, string? key = null)
        {
            pmWeb.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
            using var missingCsrf = await Command(pmWeb, path, input, version);
            missingCsrf.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await Body(missingCsrf)).GetProperty("code").GetString().Should().Be("csrf_failed");
            pmWeb.DefaultRequestHeaders.Add("X-CSRF-TOKEN", "invalid-token");
            using var invalidCsrf = await Command(pmWeb, path, input, version);
            invalidCsrf.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await Body(invalidCsrf)).GetProperty("code").GetString().Should().Be("csrf_failed");
            await WebCookieTestClient.RefreshCsrf(pmWeb);
            using var wrongRole = await Command(supWeb, path, input, version);
            wrongRole.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await Body(wrongRole)).GetProperty("code").GetString().Should().Be("access_forbidden");
            using var wrongProject = await Command(outsideWeb, path, input, version);
            wrongProject.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await Body(wrongProject)).GetProperty("code").GetString().Should().Be("access_forbidden");
            return await Command(pmActions, path, input, version, key);
        }
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
            accessPoint = withAccessPoint ? new { point = new { longitude = 106.5, latitude = 10.5 }, source = "MANUAL", accuracyMeters = 2d } : (object?)null,
            planId
        });
        taskResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var task = (await Body(taskResponse)).GetProperty("id").GetGuid();
        await CheckReads($"/api/v1/survey-tasks/{task}/work-package");
        var sparePlan = await Command(client, $"/api/v1/projects/{project}/survey-plans",
            new { scope = scopes, plannedAt = "2026-10-04T08:00:00Z", surveyType = "PERIODIC" });
        sparePlan.StatusCode.Should().Be(HttpStatusCode.Created);
        var sparePlanId = (await Body(sparePlan)).GetProperty("id").GetGuid();
        var spare = await Command(client, $"/api/v1/projects/{project}/survey-tasks",
            new { scope = scopes, surveyType = "PERIODIC", operatorId = second.Id, dueAt = "2026-10-03T08:00:00Z", planId = sparePlanId });
        spare.StatusCode.Should().Be(HttpStatusCode.Created);
        var spareId = (await Body(spare)).GetProperty("id").GetGuid();
        using var cancelled = await WebMutation($"/api/v1/survey-tasks/{spareId}/cancel", new { reason = "Cancel spare task" }, spare.Headers.ETag!.Tag);
        cancelled.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Body(cancelled)).GetProperty("status").GetString().Should().Be("CANCELLED");
        using var freshPlan = await pmWeb.GetAsync($"/api/v1/survey-plans/{sparePlanId}");
        freshPlan.StatusCode.Should().Be(HttpStatusCode.OK);
        using var postponed = await WebMutation($"/api/v1/survey-plans/{sparePlanId}/postpone",
            new { reason = "Weather" }, freshPlan.Headers.ETag!.Tag);
        postponed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Body(postponed)).GetProperty("status").GetString().Should().Be("POSTPONED");
        await Login(client, first.UserName!);
        var work = await client.GetAsync($"/api/v1/survey-tasks/{task}/work-package");
        work.StatusCode.Should().Be(HttpStatusCode.OK);
        var workBody = await Body(work);
        workBody.TryGetProperty("accessPoint", out var actualAccessPoint).Should().Be(withAccessPoint);
        if (withAccessPoint)
        {
            actualAccessPoint.GetProperty("point").GetProperty("longitude").GetDouble().Should().Be(106.5);
            actualAccessPoint.GetProperty("point").GetProperty("latitude").GetDouble().Should().Be(10.5);
            actualAccessPoint.GetProperty("source").GetString().Should().Be("MANUAL");
            actualAccessPoint.GetProperty("accuracyMeters").GetDouble().Should().Be(2d);
        }
        workBody.GetProperty("schemaVersion").GetString().Should().Be("anh01.survey-work.v1");
        workBody.GetProperty("geometryRefs").GetArrayLength().Should().Be(1);
        workBody.GetProperty("geometryRefs")[0].TryGetProperty("targetBand", out _).Should().BeFalse();
        var eligiblePath = $"/api/v1/survey-tasks/{task}/eligible-drones";
        var sharedDrone = Guid.NewGuid();
        var maintenanceDrone = Guid.NewGuid();
        await using (var db = sql.CreateDbContext())
        {
            db.DroneDevices.Add(DroneDevice.Create(sharedDrone, "SHARED-" + sharedDrone, DroneDeviceStatus.Active));
            db.DroneDevices.Add(DroneDevice.Create(maintenanceDrone, "MAINTENANCE-" + maintenanceDrone, DroneDeviceStatus.Maintenance));
            await db.SaveChangesAsync();
        }
        var selectable = new List<Guid>();
        string? droneCursor = null;
        do
        {
            var pageResponse = await client.GetAsync(eligiblePath + "?limit=1" +
                (droneCursor is null ? "" : "&cursor=" + Uri.EscapeDataString(droneCursor)));
            pageResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            pageResponse.Headers.CacheControl!.Private.Should().BeTrue();
            pageResponse.Headers.CacheControl.NoStore.Should().BeTrue();
            var page = await Body(pageResponse);
            foreach (var drone in page.GetProperty("items").EnumerateArray())
            {
                drone.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("id", "serialNo");
                selectable.Add(drone.GetProperty("id").GetGuid());
            }
            droneCursor = page.GetProperty("nextCursor").GetString();
        } while (droneCursor is not null);
        selectable.Should().Contain(device).And.Contain(sharedDrone).And.NotContain(maintenanceDrone).And.OnlyHaveUniqueItems();
        var firstDronePage = await Body(await client.GetAsync(eligiblePath + "?limit=1"));
        var foreignDroneCursor = firstDronePage.GetProperty("nextCursor").GetString()!;
        (await client.GetAsync(eligiblePath + "?cursor=invalid")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.GetAsync(eligiblePath + "?cursor=")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.GetAsync(eligiblePath + "?limit=101")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.GetAsync(eligiblePath + "?limit=1&limit=2")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.GetAsync(eligiblePath + "?search=drone")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        foreach (var user in new[] { second, manager, supervisor })
        {
            await Login(client, user.UserName!);
            (await client.GetAsync(eligiblePath)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        using (var anonymous = factory.CreateClient(new() { HandleCookies = false }))
            (await anonymous.GetAsync(eligiblePath)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await Login(client, first.UserName!);
        await using (var db = sql.CreateDbContext())
        {
            await db.DroneDevices.Where(d => selectable.Contains(d.Id)).ExecuteUpdateAsync(x => x.SetProperty(d => d.Status, DroneDeviceStatus.Maintenance));
        }
        var emptyDrones = await Body(await client.GetAsync(eligiblePath));
        emptyDrones.GetProperty("items").GetArrayLength().Should().Be(0);
        emptyDrones.GetProperty("nextCursor").ValueKind.Should().Be(JsonValueKind.Null);
        await using (var db = sql.CreateDbContext())
            await db.DroneDevices.Where(d => selectable.Contains(d.Id)).ExecuteUpdateAsync(x => x.SetProperty(d => d.Status, DroneDeviceStatus.Active));
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
        var submitPath = $"/api/v1/survey-tasks/{task}/datasets";
        var submitKey = Guid.NewGuid().ToString("N");
        await using (var db = sql.CreateDbContext())
            await db.DroneDevices.Where(d => d.Id == device).ExecuteUpdateAsync(x => x.SetProperty(d => d.Status, DroneDeviceStatus.Maintenance));
        (await Command(client, submitPath, submit, accepted.Headers.ETag!.Tag)).StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Body(await client.GetAsync(eligiblePath))).GetProperty("items").EnumerateArray()
            .Select(d => d.GetProperty("id").GetGuid()).Should().NotContain(device);
        await using (var db = sql.CreateDbContext())
            await db.DroneDevices.Where(d => d.Id == device).ExecuteUpdateAsync(x => x.SetProperty(d => d.Status, DroneDeviceStatus.Active));
        var submitted = await Command(client, submitPath, submit, accepted.Headers.ETag!.Tag, submitKey);
        submitted.StatusCode.Should().Be(HttpStatusCode.Created);
        var submittedBody = await Body(submitted);
        var dataset = submittedBody.GetProperty("id").GetGuid();
        submittedBody.GetProperty("telemetryStatus").GetString().Should().Be("MISSING");
        // Model a lost response: the client retries the original body, key and If-Match after commit.
        await using (var db = sql.CreateDbContext())
            await db.DroneDevices.Where(d => d.Id == device).ExecuteUpdateAsync(x => x.SetProperty(d => d.Status, DroneDeviceStatus.Maintenance));
        var submitReplay = await Command(client, submitPath, submit, accepted.Headers.ETag!.Tag, submitKey);
        submitReplay.StatusCode.Should().Be(HttpStatusCode.Created);
        (await Body(submitReplay)).GetProperty("id").GetGuid().Should().Be(dataset);
        (await submitReplay.Content.ReadAsStringAsync()).Should().Be(await submitted.Content.ReadAsStringAsync());
        submitReplay.Headers.Location.Should().Be(submitted.Headers.Location);
        submitReplay.Headers.ETag.Should().Be(submitted.Headers.ETag);
        var changedPayload = await Command(client, submitPath, submit with { recordedAt = "2026-10-02T09:00:00Z" }, accepted.Headers.ETag!.Tag, submitKey);
        changedPayload.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Body(changedPayload)).GetProperty("code").GetString().Should().Be("duplicate_request");
        var changedVersion = await Command(client, submitPath, submit, submitted.Headers.ETag!.Tag, submitKey);
        changedVersion.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Body(changedVersion)).GetProperty("code").GetString().Should().Be("duplicate_request");
        await using (var db = sql.CreateDbContext())
        {
            var surveyId = await db.Surveys.Where(s => s.SurveyRequestId == task).Select(s => s.Id).SingleAsync();
            (await db.SurveyDataVersions.CountAsync(d => d.SurveyId == surveyId)).Should().Be(1);
            (await db.AuditLogs.CountAsync(a => a.EventType == "survey_dataset_submitted" && a.EntityId == dataset)).Should().Be(1);
            (await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == first.Id && r.ProjectId == project &&
                r.Operation == "SurveyDatasetSubmitted" && r.IdempotencyKey == submitKey)).Should().Be(1);
            await db.DroneDevices.Where(d => d.Id == device).ExecuteUpdateAsync(x => x.SetProperty(d => d.Status, DroneDeviceStatus.Active));
            await db.ProjectMembers.Where(m => m.UserId == first.Id && m.ProjectId == project)
                .ExecuteUpdateAsync(x => x.SetProperty(m => m.Status, ProjectMemberStatus.Ended));
        }
        (await Command(client, submitPath, submit, accepted.Headers.ETag!.Tag, submitKey)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync(eligiblePath)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await using (var db = sql.CreateDbContext())
            await db.ProjectMembers.Where(m => m.UserId == first.Id && m.ProjectId == project)
                .ExecuteUpdateAsync(x => x.SetProperty(m => m.Status, ProjectMemberStatus.Active));
        using var reopened = factory.CreateClient();
        await Login(reopened, first.UserName!);
        using var reopenedList = await reopened.GetAsync(submitPath);
        reopenedList.StatusCode.Should().Be(HttpStatusCode.OK);
        var reopenedItem = (await Body(reopenedList)).GetProperty("items").EnumerateArray().Should().ContainSingle().Which;
        reopenedItem.GetProperty("id").GetGuid().Should().Be(dataset);
        using var reopenedDetail = await reopened.GetAsync($"/api/v1/datasets/{reopenedItem.GetProperty("id").GetGuid()}");
        reopenedDetail.StatusCode.Should().Be(HttpStatusCode.OK);
        var videoSource = (await Body(reopenedDetail)).GetProperty("sourceFiles").EnumerateArray().Should().ContainSingle().Which;
        videoSource.GetProperty("purpose").GetString().Should().Be("SURVEY_VIDEO");
        using var downloaded = await reopened.GetAsync($"/api/v1/files/{videoSource.GetProperty("fileId").GetGuid()}/content");
        downloaded.StatusCode.Should().Be(HttpStatusCode.OK);
        downloaded.Content.Headers.ContentType!.MediaType.Should().Be("video/mp4");
        (await downloaded.Content.ReadAsByteArrayAsync()).Should().Equal(media);
        await Login(reopened, second.UserName!);
        (await reopened.GetAsync(submitPath)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await reopened.GetAsync($"/api/v1/datasets/{dataset}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await reopened.GetAsync($"/api/v1/files/{video}/content")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
        (await Command(client, $"/api/v1/datasets/{dataset}/assessments", assessmentBody, "\"malformed\""))
            .StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        var assessmentPath = $"/api/v1/datasets/{dataset}/assessments";
        using var datasetForReview = await pmWeb.GetAsync($"/api/v1/datasets/{dataset}");
        datasetForReview.StatusCode.Should().Be(HttpStatusCode.OK);
        var datasetETag = datasetForReview.Headers.ETag!.Tag;
        datasetETag.Should().Be(submitted.Headers.ETag!.Tag);
        using var staleAssessment = await Command(pmActions, assessmentPath, assessmentBody, "\"//////////8=\"");
        staleAssessment.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        (await Body(staleAssessment)).GetProperty("code").GetString().Should().Be("concurrency_conflict");
        var assessmentKey = Guid.NewGuid().ToString("N");
        var assessed = await WebMutation(assessmentPath, assessmentBody, datasetETag, assessmentKey);
        assessed.StatusCode.Should().Be(HttpStatusCode.Created);
        var assessmentView = await Body(assessed);
        var assessment = assessmentView.GetProperty("id").GetGuid();
        assessmentView.GetProperty("datasetId").GetGuid().Should().Be(dataset);
        assessed.Headers.Location!.OriginalString.Should().Be($"{assessmentPath}/{assessment}");
        var replay = await Command(pmActions, assessmentPath, assessmentBody, datasetETag, assessmentKey);
        replay.StatusCode.Should().Be(HttpStatusCode.Created);
        (await Body(replay)).GetProperty("id").GetGuid().Should().Be(assessment);
        (await client.GetAsync(assessed.Headers.Location)).StatusCode.Should().Be(HttpStatusCode.OK);
        using var webAssessment = await pmWeb.GetAsync(assessed.Headers.Location);
        webAssessment.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Body(webAssessment)).GetProperty("id").GetGuid().Should().Be(assessment);
        using var outsideAssessment = await outsideWeb.GetAsync(assessed.Headers.Location);
        outsideAssessment.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await CheckReads($"/api/v1/datasets/{dataset}");
        await CheckReads($"/api/v1/datasets/{dataset}/coverage");
        var coverage = await Body(await client.GetAsync($"/api/v1/datasets/{dataset}/coverage"));
        coverage.GetProperty("assessmentId").GetGuid().Should().Be(assessment);
        await CheckReads(submitPath);
        using var datasetList = await client.GetAsync(submitPath);
        datasetList.StatusCode.Should().Be(HttpStatusCode.OK);
        var singlePage = await Body(datasetList);
        var datasetItem = singlePage.GetProperty("items").EnumerateArray().Should().ContainSingle().Which;
        datasetItem.GetProperty("id").GetGuid().Should().Be(dataset);
        datasetItem.GetProperty("surveyTaskId").GetGuid().Should().Be(task);
        datasetItem.GetProperty("versionNo").GetInt32().Should().Be(1);
        datasetItem.GetProperty("integrityStatus").GetString().Should().Be("PASSED");
        datasetItem.GetProperty("version").GetString().Should().Be(submitted.Headers.ETag!.Tag.Trim('"'));
        datasetItem.GetProperty("submittedAt").GetDateTimeOffset().Should().Be((await Body(detail)).GetProperty("submittedAt").GetDateTimeOffset());
        datasetItem.GetProperty("assessmentId").GetGuid().Should().Be(assessment);
        singlePage.GetProperty("nextCursor").ValueKind.Should().Be(JsonValueKind.Null);
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
        var baselinePath = $"/api/v1/projects/{project}/baseline-selections";
        var denied = await Command(pmActions, baselinePath, new { items = new[] { Selection("SURFACE"), Selection("RIGHT_EDGE") }, reason = "Partial baseline" });
        denied.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Body(denied)).GetProperty("code").GetString().Should().Be("baseline_not_eligible");
        await using (var db = sql.CreateDbContext()) { (await db.Set<BaselineSelection>().CountAsync(x => x.ProjectId == project)).Should().Be(0); }
        var selectionKey = Guid.NewGuid().ToString("N");
        var selectBody = new { items = new[] { Selection("SURFACE") }, reason = "Surface evidence accepted" };
        var selected = await WebMutation(baselinePath, selectBody, key: selectionKey);
        selected.StatusCode.Should().Be(HttpStatusCode.Created);
        var selectedBody = await Body(selected);
        var selectedItem = selectedBody.GetProperty("items").EnumerateArray().Should().ContainSingle().Which;
        selectedItem.GetProperty("datasetId").GetGuid().Should().Be(dataset);
        selectedItem.GetProperty("assessmentId").GetGuid().Should().Be(assessment);
        selectedItem.GetProperty("segmentId").GetGuid().Should().Be(segment);
        selectedItem.GetProperty("targetBand").GetString().Should().Be("SURFACE");
        (await client.GetAsync(selected.Headers.Location)).StatusCode.Should().Be(HttpStatusCode.OK);
        using var webHistory = await supWeb.GetAsync(selected.Headers.Location);
        webHistory.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Body(webHistory)).GetProperty("id").GetGuid().Should().Be(selectedBody.GetProperty("id").GetGuid());
        using var outsideHistory = await outsideWeb.GetAsync(selected.Headers.Location);
        outsideHistory.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var selectedReplay = await Command(pmActions, baselinePath, selectBody, key: selectionKey);
        selectedReplay.StatusCode.Should().Be(HttpStatusCode.Created);
        (await Body(selectedReplay)).GetProperty("id").GetGuid().Should().Be(selectedBody.GetProperty("id").GetGuid());
        var stale = await Command(pmActions, baselinePath, selectBody);
        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Body(stale)).GetProperty("code").GetString().Should().Be("baseline_selection_conflict");

        var baselineCurrentPath = $"{baselinePath}?segmentSetId={set}";
        async Task<Guid> ReadCurrentSelection(HttpClient reader)
        {
            using var response = await reader.GetAsync(baselineCurrentPath);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var item = (await Body(response)).GetProperty("items").EnumerateArray().Should().ContainSingle().Which;
            item.GetProperty("routeVersionId").GetGuid().Should().Be(route);
            item.GetProperty("segmentSetId").GetGuid().Should().Be(set);
            item.GetProperty("segmentId").GetGuid().Should().Be(segment);
            item.GetProperty("targetBand").GetString().Should().Be("SURFACE");
            return item.GetProperty("selectionId").GetGuid();
        }

        using var reloadedPmWeb = await WebCookieTestClient.Login(factory, manager.Email!);
        reloadedPmWeb.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        var originalSelectionId = await ReadCurrentSelection(reloadedPmWeb);
        originalSelectionId.Should().Be(selectedItem.GetProperty("selectionId").GetGuid());
        (await ReadCurrentSelection(supWeb)).Should().Be(originalSelectionId);
        (await ReadCurrentSelection(client)).Should().Be(originalSelectionId);
        using var outsideCurrent = await outsideWeb.GetAsync(baselineCurrentPath);
        outsideCurrent.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Body(outsideCurrent)).GetProperty("code").GetString().Should().Be("access_forbidden");
        using var otherSet = await reloadedPmWeb.GetAsync($"{baselinePath}?segmentSetId={Guid.NewGuid()}");
        otherSet.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Body(otherSet)).GetProperty("items").GetArrayLength().Should().Be(0);

        await WebCookieTestClient.RefreshCsrf(reloadedPmWeb);
        var replaceBody = new { items = new[] { Selection("SURFACE", originalSelectionId) }, reason = "Replace after reloading" };
        using var replaced = await Command(reloadedPmWeb, baselinePath, replaceBody);
        replaced.StatusCode.Should().Be(HttpStatusCode.Created);
        var replacedSelectionId = (await Body(replaced)).GetProperty("items")[0].GetProperty("selectionId").GetGuid();
        replacedSelectionId.Should().NotBe(originalSelectionId);

        using var staleReplacement = await Command(reloadedPmWeb, baselinePath, replaceBody);
        staleReplacement.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Body(staleReplacement)).GetProperty("code").GetString().Should().Be("baseline_selection_conflict");

        reloadedPmWeb.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        var refreshedSelectionId = await ReadCurrentSelection(reloadedPmWeb);
        refreshedSelectionId.Should().Be(replacedSelectionId);
        await WebCookieTestClient.RefreshCsrf(reloadedPmWeb);
        var confirmedBody = new { items = new[] { Selection("SURFACE", refreshedSelectionId) }, reason = "PM confirmed current baseline after conflict" };
        using var confirmed = await Command(reloadedPmWeb, baselinePath, confirmedBody);
        confirmed.StatusCode.Should().Be(HttpStatusCode.Created);
        var confirmedSelectionId = (await Body(confirmed)).GetProperty("items")[0].GetProperty("selectionId").GetGuid();
        confirmedSelectionId.Should().NotBe(refreshedSelectionId);
        (await ReadCurrentSelection(reloadedPmWeb)).Should().Be(confirmedSelectionId);

        var compareBody = new { items = new[] { Selection("SURFACE", confirmedSelectionId) }, reason = "PM reconfirmation" };
        var selectionRace = await Task.WhenAll(Command(client, baselinePath, compareBody),
            Command(client, baselinePath, compareBody));
        selectionRace.Count(r => r.StatusCode == HttpStatusCode.Created).Should().Be(1);
        selectionRace.Single(r => r.StatusCode != HttpStatusCode.Created).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await client.GetAsync(selected.Headers.Location)).StatusCode.Should().Be(HttpStatusCode.OK);
        var failReview = await Command(client, $"/api/v1/datasets/{dataset}/assessments", new { methodVersion = "pm-evidence-review.v1", items = new[] { Item("SURFACE", "FAIL"), Item("RIGHT_EDGE", "UNKNOWN") } }, submitted.Headers.ETag!.Tag);
        failReview.StatusCode.Should().Be(HttpStatusCode.Created);
        (await client.GetAsync(selected.Headers.Location)).StatusCode.Should().Be(HttpStatusCode.OK);
        // A15: a new current route must not invalidate already assigned scopes or their supplement child.
        await RoadGuardSystem.ApiTests.Projects.GeometrySubmissionHttpTests.MarkAsPreMigrationConfirmedFixture(sql,
            (await Body(draft)).GetProperty("id").GetGuid());
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
        using var replacementDraftSubmission = await RoadGuardSystem.ApiTests.Projects.GeometrySubmissionHttpTests.Submit(client, replacementDraft.Headers.Location!.OriginalString, replacementDraft.Headers.ETag!);
        await Login(client, supervisor.UserName!);
        var replacementRoute = await Command(client, replacementDraft.Headers.Location!.OriginalString + "/confirm", new
        {
            expectedCurrentVersionId = route,
            effectiveFrom = "2026-10-03T00:00:00Z",
            reason = "Supersede route after assigned task"
        }, replacementDraftSubmission.Headers.ETag!.Tag);
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
        (await Body(parent)).GetProperty("supplementTaskId").ValueKind.Should().Be(JsonValueKind.Null);
        var supplementKey = Guid.NewGuid().ToString("N");
        var supplementBody = new { scope = new[] { Scope("RIGHT_EDGE") }, operatorId = second.Id, reason = "Missing right edge" };
        var supplemented = await WebMutation($"/api/v1/survey-tasks/{task}/supplements", supplementBody, parent.Headers.ETag!.Tag, supplementKey);
        supplemented.StatusCode.Should().Be(HttpStatusCode.Created);
        var child = (await Body(supplemented)).GetProperty("supplementTaskId").GetGuid();
        (await Body(supplemented)).GetProperty("id").GetGuid().Should().Be(task);
        var supplementReplay = await Command(pmActions, $"/api/v1/survey-tasks/{task}/supplements", supplementBody, parent.Headers.ETag!.Tag, supplementKey);
        (await Body(supplementReplay)).GetProperty("supplementTaskId").GetGuid().Should().Be(child);
        var parentAfterSupplement = await pmActions.GetAsync($"/api/v1/survey-tasks/{task}");
        parentAfterSupplement.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Body(parentAfterSupplement)).GetProperty("supplementTaskId").GetGuid().Should().Be(child);
        (await Body(await client.GetAsync($"/api/v1/survey-tasks/{child}"))).GetProperty("id").GetGuid().Should().Be(child);
        if (!withAccessPoint)
        {
            await using var db = sql.CreateDbContext();
            var originalScopes = await db.SurveyRequestScopes.AsNoTracking().Where(row => row.SurveyRequestId == task).ToArrayAsync();
            originalScopes.Should().NotBeEmpty();
            await db.SurveyRequestScopes.Where(row => row.SurveyRequestId == task).ExecuteDeleteAsync();
            var legacyRead = await client.GetAsync($"/api/v1/survey-tasks/{task}");
            legacyRead.StatusCode.Should().Be(HttpStatusCode.OK);
            (await Body(legacyRead)).GetProperty("supplementTaskId").GetGuid().Should().Be(child);
            db.SurveyRequestScopes.AddRange(originalScopes);
            await db.SaveChangesAsync();
        }
        var reopenParent = await Command(client, $"/api/v1/survey-tasks/{task}/reassign",
            new { operatorId = second.Id, reason = "Attempt to bypass supplement child" }, parentAfterSupplement.Headers.ETag!.Tag);
        reopenParent.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await Login(client, first.UserName!);
        (await client.GetAsync($"/api/v1/survey-tasks/{child}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var originalOperatorRead = await client.GetAsync($"/api/v1/survey-tasks/{task}");
        originalOperatorRead.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Body(originalOperatorRead)).GetProperty("supplementTaskId").ValueKind.Should().Be(JsonValueKind.Null);
        // Fixture authority change isolates replay authorization from the already submitted task lifecycle.
        await using (var db = sql.CreateDbContext())
        {
            var assignment = await db.SurveyAssignments.SingleAsync(a => a.SurveyRequestId == task && a.EndedAt == null);
            assignment.EndForReassignment(DateTimeOffset.UtcNow, "Replay authority fixture");
            await db.SaveChangesAsync();
            db.SurveyAssignments.Add(SurveyAssignment.Create(Guid.NewGuid(), task, second.Id, manager.Id,
                DateTimeOffset.UtcNow, null, null, null, null, null));
            await db.SaveChangesAsync();
        }
        await Login(client, first.UserName!);
        (await Command(client, submitPath, submit, accepted.Headers.ETag!.Tag, submitKey)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync(eligiblePath + "?cursor=" + Uri.EscapeDataString(foreignDroneCursor))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync(submitPath)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync($"/api/v1/files/{video}/content")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await Login(client, second.UserName!);
        var bothAssigned = await client.GetAsync($"/api/v1/survey-tasks/{task}");
        bothAssigned.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Body(bothAssigned)).GetProperty("supplementTaskId").GetGuid().Should().Be(child);
        var childRead = await client.GetAsync(supplemented.Headers.Location);
        childRead.StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"/api/v1/survey-tasks/{child}/eligible-drones?cursor=" + Uri.EscapeDataString(foreignDroneCursor)))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await Login(client, manager.UserName!);
        var reassignChild = $"/api/v1/survey-tasks/{child}/reassign";
        using var moved = await WebMutation(reassignChild, new { operatorId = first.Id, reason = "Move supplement assignment" },
            childRead.Headers.ETag!.Tag);
        moved.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Body(moved)).GetProperty("operatorId").GetGuid().Should().Be(first.Id);
        await Login(client, second.UserName!);
        using var parentAfterMove = await client.GetAsync($"/api/v1/survey-tasks/{task}");
        parentAfterMove.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Body(parentAfterMove)).GetProperty("supplementTaskId").ValueKind.Should().Be(JsonValueKind.Null);
        (await client.GetAsync($"/api/v1/survey-tasks/{child}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await Login(client, manager.UserName!);
        using var returned = await Command(client, reassignChild, new { operatorId = second.Id, reason = "Restore supplement assignment" },
            moved.Headers.ETag!.Tag);
        returned.StatusCode.Should().Be(HttpStatusCode.OK);
        await Login(client, second.UserName!);
        (await Command(client, $"/api/v1/survey-tasks/{child}/accept", null, returned.Headers.ETag!.Tag)).StatusCode.Should().Be(HttpStatusCode.OK);
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
            (await db.Set<BaselineSelection>().CountAsync(x => x.ProjectId == project)).Should().Be(4);
            (await db.SurveyRequests.CountAsync(x => x.ParentTaskId == task)).Should().Be(1);
            (await db.Surveys.CountAsync(x => x.SurveyRequestId == child)).Should().Be(1);
            var membership = await db.ProjectMembers.SingleAsync(x => x.UserId == second.Id && x.ProjectId == project);
            membership.Status = ProjectMemberStatus.Ended; await db.SaveChangesAsync();
        }
        (await client.GetAsync($"/api/v1/survey-tasks/{child}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await Login(client, manager.UserName!);
        var another = await Command(client, $"/api/v1/survey-tasks/{task}/supplements",
            new { scope = new[] { Scope("RIGHT_EDGE") }, operatorId = first.Id, reason = "Second direct round" },
            parentAfterSupplement.Headers.ETag!.Tag);
        another.StatusCode.Should().Be(HttpStatusCode.Created);
        (await Body(another)).GetProperty("supplementTaskId").GetGuid().Should().NotBe(child);
        (await Body(await client.GetAsync($"/api/v1/survey-tasks/{task}"))).GetProperty("supplementTaskId").ValueKind.Should().Be(JsonValueKind.Null);

        // V2 submits once per task. Seed two additional versions to verify historical readback.
        var earlierTieId = Guid.NewGuid();
        var laterTieId = Guid.NewGuid();
        if (new System.Data.SqlTypes.SqlGuid(earlierTieId).CompareTo(new System.Data.SqlTypes.SqlGuid(laterTieId)) > 0)
            (earlierTieId, laterTieId) = (laterTieId, earlierTieId);
        await using (var db = sql.CreateDbContext())
        {
            var original = await db.SurveyDataVersions.SingleAsync(d => d.Id == dataset);
            var tiedTime = original.ConfirmedAt!.Value.AddMinutes(1);
            db.SurveyDataVersions.AddRange(
                SurveyDataVersion.CreateSubmitted(laterTieId, original.SurveyId, 2, original.RecordedAt!.Value,
                    tiedTime, device, original.SourceManifest, original.ScopeManifest!),
                SurveyDataVersion.CreateSubmitted(earlierTieId, original.SurveyId, 3, original.RecordedAt.Value,
                    tiedTime, device, original.SourceManifest, original.ScopeManifest!));
            await db.SaveChangesAsync();
        }
        pmWeb.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        string? cursor = null;
        foreach (var expected in new[] { laterTieId, earlierTieId, dataset })
        {
            var path = submitPath + "?limit=1" + (cursor is null ? "" : "&cursor=" + Uri.EscapeDataString(cursor));
            using var response = await pmWeb.GetAsync(path);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var page = await Body(response);
            var item = page.GetProperty("items").EnumerateArray().Should().ContainSingle().Which;
            item.GetProperty("id").GetGuid().Should().Be(expected);
            cursor = page.GetProperty("nextCursor").GetString();
            if (expected == dataset)
            {
                using var latestCoverage = await pmWeb.GetAsync($"/api/v1/datasets/{dataset}/coverage");
                item.GetProperty("assessmentId").GetGuid().Should().Be((await Body(latestCoverage)).GetProperty("assessmentId").GetGuid());
                cursor.Should().BeNull();
            }
            else
            {
                item.GetProperty("assessmentId").ValueKind.Should().Be(JsonValueKind.Null);
                cursor.Should().NotBeNullOrEmpty();
                using var mismatchedTask = await pmWeb.GetAsync($"/api/v1/survey-tasks/{child}/datasets?cursor={Uri.EscapeDataString(cursor!)}");
                mismatchedTask.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            }
        }
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
