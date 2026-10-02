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
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Reporting;

[Collection(AuthenticationApiFixture.Name)]
public sealed class Anh02ReportingHttpTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task GeneratedAssessmentChainReportsDistinctPerBandFactsAndReadOnlyTimeline()
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
        var created = await client.PostAsJsonAsync("/api/v1/projects", new { projectCode = $"ANH-{Guid.NewGuid():N}", name = "DEMO survey evidence",
            engineeringUtmSrid = 32648, startDate = "2026-09-01", endDate = "2027-09-01", primaryProjectManagerUserId = manager.Id,
            handover = new { documentNo = "DEMO", handoverDate = "2026-08-31" }, operationId = Guid.NewGuid() });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var project = (await Body(created)).GetProperty("projectId").GetGuid();
        await Login(client, manager.UserName!);
        var draft = await Command(client, $"/api/v1/projects/{project}/road-geometry-drafts", new { sourceKind = "COORDINATES", sourceCrs = 32648,
            stationOriginMeters = 0d, changeReason = "DEMO alignment", coordinates = new[] { new { x = 500000d, y = 1100000d }, new { x = 500200d, y = 1100000d } },
            widthProfile = new[] { new { fromOffsetMeters = 0d, toOffsetMeters = 200d, widthMeters = 8d } }, surveyWidthMeters = 12d, roadCode = "DEMO" });
        draft.StatusCode.Should().Be(HttpStatusCode.Created);
        await Login(client, supervisor.UserName!);
        var road = await Command(client, draft.Headers.Location!.OriginalString + "/confirm", new { expectedCurrentVersionId = (Guid?)null,
            effectiveFrom = "2026-10-02T00:00:00Z", reason = "Supervisor DEMO approval" }, draft.Headers.ETag!.Tag);
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
                db.ProjectMembers.Add(new ProjectMember { Id = Guid.NewGuid(), ProjectId = project, UserId = actor, RoleCode = UserRoleCode.DroneOperator,
                    ValidFrom = new DateOnly(2026, 1, 1), Status = ProjectMemberStatus.Active });
            await db.SaveChangesAsync();
        }
        object Scope(string band) => new { routeVersionId = route, segmentSetId = set, segmentIds = new[] { segment }, targetBand = band };
        var scopes = new[] { Scope("SURFACE"), Scope("RIGHT_EDGE") };
        await Login(client, manager.UserName!);
        var plan = await Command(client, $"/api/v1/projects/{project}/survey-plans", new { scope = scopes, plannedAt = "2026-10-02T08:00:00Z", surveyType = "BASELINE" });
        plan.StatusCode.Should().Be(HttpStatusCode.Created);
        var planId = (await Body(plan)).GetProperty("id").GetGuid();
        (await client.GetAsync(plan.Headers.Location)).StatusCode.Should().Be(HttpStatusCode.OK);
        var taskResponse = await Command(client, $"/api/v1/projects/{project}/survey-tasks", new { scope = scopes, surveyType = "BASELINE", operatorId = first.Id,
            dueAt = "2026-10-03T08:00:00Z", accessPoint = (object?)null, planId });
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
        var uploadCreated = await Command(client, "/api/v1/uploads", new { purpose = "SURVEY_VIDEO", projectId = project, targetId = task,
            fileName = "synthetic.mp4", mediaType = "video/mp4", sizeBytes = media.Length, checksumSha256 = checksum });
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
        var submit = new { videoFileIds = new[] { video }, telemetryFileIds = Array.Empty<Guid>(), recordedAt = "2026-10-02T08:00:00Z",
            deviceId = device, scope = scopes, pairs = new[] { new { videoFileId = video, telemetryFileId = (Guid?)null, timeOffsetMilliseconds = 0L } } };
        var submitted = await Command(client, $"/api/v1/survey-tasks/{task}/datasets", submit, accepted.Headers.ETag!.Tag);
        submitted.StatusCode.Should().Be(HttpStatusCode.Created);
        var dataset = (await Body(submitted)).GetProperty("id").GetGuid();
        var detail = await client.GetAsync(submitted.Headers.Location);
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Body(detail)).GetProperty("submittedBy").GetGuid().Should().Be(first.Id);
        await Login(client, manager.UserName!);
        object Item(string band, string status) => new { routeVersionId = route, segmentSetId = set, segmentId = segment, targetBand = band,
            positionStatus = status, qualityStatus = status, coverageStatus = status, reason = status == "PASS" ? "PM inspected position, clear image and full band" : "Band not visible",
            evidence = status == "PASS" ? new[] { new { fileId = video, fromMilliseconds = 0, toMilliseconds = 1000 } } : Array.Empty<object>() };
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
        object Selection(string band, Guid? expected = null) => new { routeVersionId = route, segmentSetId = set, segmentId = segment,
            targetBand = band, datasetId = dataset, assessmentId = assessment, expectedBaselineSelectionId = expected };
        var denied = await Command(client, $"/api/v1/projects/{project}/baseline-selections", new { items = new[] { Selection("SURFACE"), Selection("RIGHT_EDGE") }, reason = "Partial baseline" });
        denied.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await using (var db = sql.CreateDbContext()) { (await db.Set<BaselineSelection>().CountAsync(x => x.ProjectId == project)).Should().Be(0); }
        var selectionKey = Guid.NewGuid().ToString("N");
        var selectBody = new { items = new[] { Selection("SURFACE") }, reason = "Surface evidence accepted" };
        var selected = await Command(client, $"/api/v1/projects/{project}/baseline-selections", selectBody, key: selectionKey);
        selected.StatusCode.Should().Be(HttpStatusCode.Created);
        var export = await Command(client, $"/api/v1/projects/{project}/exports", new { kind = "DOSSIER", format = "ZIP", segmentIds = new[] { segment }, includeOriginalFiles = false });
        export.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var exportView = await Body(export);
        var manifest = await Body(await client.GetAsync($"/api/v1/projects/{project}/exports/{exportView.GetProperty("id").GetGuid()}/manifest"));
        var exportedVideo = manifest.GetProperty("files").EnumerateArray().Single(f => f.GetProperty("fileId").GetGuid() == video);
        exportedVideo.GetProperty("sha256").GetString().Should().Be(checksum); exportedVideo.GetProperty("sizeBytes").GetInt64().Should().Be(media.Length);
        await using (var proof = sql.CreateDbContext()) exportedVideo.GetProperty("fileVersion").GetString()
            .Should().Be(Convert.ToBase64String((await proof.UploadSessions.SingleAsync(u => u.FileId == video)).RowVersion));
        var revisions = manifest.GetProperty("sourceRevisions").EnumerateArray().ToArray();
        revisions.Should().Contain(r => r.GetProperty("id").GetGuid() == dataset && r.GetProperty("kind").GetString() == "SurveyDataVersion");
        revisions.Should().Contain(r => r.GetProperty("id").GetGuid() == assessment && r.GetProperty("kind").GetString() == "DatasetAssessment");
        revisions.Single(r => r.GetProperty("id").GetGuid() == dataset).GetProperty("version").GetString().Should().Be(submitted.Headers.ETag!.Tag.Trim('"'));
        revisions.Single(r => r.GetProperty("id").GetGuid() == assessment).GetProperty("version").GetString().Should().Be(assessed.Headers.ETag!.Tag.Trim('"'));
        await AssertReporting(client, factory, project, route, set, segment, video, manager.Id, sql);
    }
    private static async Task AssertReporting(HttpClient client, AuthenticationWebApplicationFactory factory, Guid project, Guid route, Guid set, Guid segment, Guid file, Guid manager, AuthenticationSqlServerFixture sql)
    {
        var from = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero); var to = from.AddDays(1);
        Guid task;
        await using (var db = sql.CreateDbContext())
        {
            task = await db.SurveyRequests.Where(x => x.ProjectId == project).Select(x => x.Id).FirstAsync();
            var parent = await db.SurveyRequests.SingleAsync(x => x.Id == task);
            var child = SurveyRequest.Create(Guid.NewGuid(), project, parent.RoadSectionId, null, manager, SurveyType.Original, SurveyRequestStatus.NewAssigned, from, from.AddDays(1), roadSectionVersionId: route);
            child.SetBandScope(task); db.SurveyRequests.Add(child);
            db.SurveyRequestScopes.Add(SurveyRequestScope.Create(Guid.NewGuid(), child.Id, route, set, JsonSerializer.Serialize(new[] { segment }), "SURFACE"));
            db.SurveyRequests.Add(SurveyRequest.Create(Guid.NewGuid(), project, parent.RoadSectionId, null, manager, SurveyType.Original, SurveyRequestStatus.NewAssigned, from, from.AddDays(1), roadSectionVersionId: route));
            foreach (var time in new[] { from, from, to })
                db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), manager, time, "survey_task_accept", "SurveyRequest", task,
                    null, "{}", "PRIVATE email person@example.test token do-not-expose", "fixture", null, []));
            db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), manager, from, "unknown_private_action", "SurveyRequest", task, null, null, "PRIVATE", "fixture", null));
            // SQL-only legacy fixture has Reports/Case snapshots but no canonical active link rows. Do not manufacture report attribution.
            var reporter = (await db.Files.SingleAsync(x => x.Id == file)).UploadedByUserId!.Value;
            var sourceVersion = Convert.ToBase64String((await db.UploadSessions.SingleAsync(x => x.FileId == file)).RowVersion);
            var telemetry = Guid.NewGuid(); var telemetryHash = new string('b', 64); const long telemetryBytes = 4L * 1024 * 1024 * 1024;
            db.Files.Add(StoredFile.Create(telemetry, $"fixture/{telemetry:N}", "telemetry.csv", "text/csv", telemetryBytes, telemetryHash, reporter, from, null));
            db.FileScopes.Add(FileScope.Create(Guid.NewGuid(), telemetry, project, task, reporter, "TELEMETRY", from));
            var uploaded = UploadSession.Create(Guid.NewGuid(), telemetry, reporter, $"fixture/{telemetry:N}", "TELEMETRY", "text/csv", telemetryBytes, telemetryHash, 8 * 1024 * 1024, from.AddHours(1));
            uploaded.StartUploading("fixture", from); db.UploadSessions.Add(uploaded); await db.SaveChangesAsync();
            uploaded.StartVerification(Convert.ToBase64String(uploaded.RowVersion), from); uploaded.MarkVerified(); await db.SaveChangesAsync();
            var originalDataset = await (from d in db.SurveyDataVersions join s in db.Surveys on d.SurveyId equals s.Id where s.ProjectId == project select d).SingleAsync();
            var source = await db.Files.SingleAsync(x => x.Id == file);
            // Isolated SQL fixture adds a repeated source in another version and a >Int32 telemetry file.
            // This proves read definitions, while the baseline/assessment above was generated through HTTP.
            var manifest = JsonSerializer.Serialize(new[] { new { fileId = source.Id, checksumSha256 = source.Checksum, sizeBytes = source.SizeBytes, mediaType = source.MimeType, purpose = "SURVEY_VIDEO" },
                new { fileId = telemetry, checksumSha256 = telemetryHash, sizeBytes = telemetryBytes, mediaType = "text/csv", purpose = "TELEMETRY" } });
            var repeated = SurveyDataVersion.CreateSubmitted(Guid.NewGuid(), originalDataset.SurveyId, 2, from, from, originalDataset.DeviceId!.Value, manifest, originalDataset.ScopeManifest!);
            repeated.SetSubmissionProvenance(reporter, originalDataset.PairsManifest!); db.SurveyDataVersions.Add(repeated); await db.SaveChangesAsync();
            var first = Report.Create(Guid.NewGuid(), reporter, "PRIVATE reporter one", from, [VerifiedEvidenceReference.Create(Guid.NewGuid(), file, sourceVersion, reporter)]);
            var second = Report.Create(Guid.NewGuid(), reporter, "PRIVATE reporter two", from, [VerifiedEvidenceReference.Create(Guid.NewGuid(), file, sourceVersion, reporter)]);
            db.Reports.AddRange(first, second);
            var incident = IncidentCase.CreateUnassigned(Guid.NewGuid(), first.Id, from);
            incident.Triage(project, CaseVerificationMethod.ExistingEvidence, "Fixture", from);
            var other = IncidentCase.CreateUnassigned(Guid.NewGuid(), second.Id, from);
            other.Triage(project, CaseVerificationMethod.ExistingEvidence, "Fixture", from);
            incident.LinkReportsFrom(other, [second.Id], manager, "Fixture link", from);
            db.IncidentCases.AddRange(incident, other);
            await db.SaveChangesAsync();
            (await db.Reports.CountAsync(x => x.Id == first.Id || x.Id == second.Id)).Should().Be(2);
            (await db.IncidentCases.CountAsync(x => x.ProjectId == project && x.Status == IncidentCaseStatus.Open)).Should().Be(1);
        }
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RoadGuardSystem.Repositories.RoadGuardDbContext>();
            var reporting = scope.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Reporting.IReportingService>();
            await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                var captured = await reporting.CaptureAsync(manager, project, new(), default);
                captured.Code.Should().Be("success"); db.Database.CurrentTransaction.Should().BeSameAs(transaction);
                captured.Value!.Summary.Isolation.Should().Be("SERIALIZABLE");
                db.ChangeTracker.Entries().Should().NotContain(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted);
                await transaction.RollbackAsync();
            });
        }
        long[] before;
        await using (var db = sql.CreateDbContext()) before = [await db.AuditLogs.LongCountAsync(), await db.IdempotencyRecords.LongCountAsync(), await db.OutboxMessages.LongCountAsync(),
            await db.Files.LongCountAsync(), await db.SurveyDataVersions.LongCountAsync(), await db.Set<BaselineCurrentPointer>().LongCountAsync()];
        var path = $"/api/v1/projects/{project}/reports";
        var summaryResponse = await client.GetAsync(path + "/summary"); summaryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        summaryResponse.Headers.ETag.Should().NotBeNull();
        var summary = await Body(summaryResponse); summary.GetProperty("isolation").GetString().Should().Be("SERIALIZABLE");
        var metrics = summary.GetProperty("metrics").EnumerateArray().ToArray();
        metrics.Single(m => m.GetProperty("code").GetString() == "legacyUnclassified").GetProperty("value").GetInt64().Should().Be(1);
        metrics.Where(m => m.GetProperty("code").GetString() == "surveyTasksByStatus").Sum(m => m.GetProperty("value").GetInt64()).Should().Be(2);
        var spatialSummary = await Body(await client.GetAsync(path + $"/summary?segmentSetId={set}"));
        var unknownLegacy = spatialSummary.GetProperty("metrics").EnumerateArray().Single(m => m.GetProperty("code").GetString() == "legacyUnclassified");
        unknownLegacy.GetProperty("value").ValueKind.Should().Be(JsonValueKind.Null); unknownLegacy.GetProperty("availability").GetString().Should().Be("UNAVAILABLE");
        unknownLegacy.GetProperty("reasonCodes").EnumerateArray().Select(r => r.GetString()).Should().Contain("LEGACY_SPATIAL_SCOPE_UNAVAILABLE");
        var tasks = await Body(await client.GetAsync(path + "/items?metric=surveyTasksByStatus"));
        tasks.GetProperty("items").EnumerateArray().Should().Contain(i => i.GetProperty("parentTaskId").ValueKind != JsonValueKind.Null && i.GetProperty("parentTaskId").GetGuid() == task);
        var surface = metrics.Single(m => m.GetProperty("code").GetString() == "baselineCoverageByBand" && m.GetProperty("dimensions").GetProperty("band").GetString() == "SURFACE");
        surface.GetProperty("numerator").GetInt64().Should().Be(1); surface.GetProperty("denominator").GetInt64().Should().Be(2);
        metrics.Single(m => m.GetProperty("code").GetString() == "verifiedSourceBytes").GetProperty("value").GetInt64().Should().Be(100 + 4L * 1024 * 1024 * 1024);
        foreach (var code in new[] { "defectsByStatus", "repairItemsByStatus", "repairAcceptanceRate" })
        {
            var missing = metrics.Single(m => m.GetProperty("code").GetString() == code);
            missing.GetProperty("value").ValueKind.Should().Be(JsonValueKind.Null); missing.GetProperty("availability").GetString().Should().Be("UNAVAILABLE");
        }
        var reportCount = metrics.Single(m => m.GetProperty("code").GetString() == "reportsReceived");
        reportCount.GetProperty("value").GetInt32().Should().Be(0);
        reportCount.GetProperty("availability").GetString().Should().Be("PARTIAL");
        metrics.Where(m => m.GetProperty("code").GetString() == "casesByStatus").Sum(m => m.GetProperty("value").GetInt32()).Should().Be(2);
        foreach (var missingSpatial in spatialSummary.GetProperty("metrics").EnumerateArray().Where(m => m.GetProperty("code").GetString() is "reportsReceived" or "casesByStatus"))
        {
            missingSpatial.GetProperty("value").ValueKind.Should().Be(JsonValueKind.Null);
            missingSpatial.GetProperty("availability").GetString().Should().Be("UNAVAILABLE");
        }
        var drilldown = await Body(await client.GetAsync(path + "/items?metric=baselineCoverageByBand"));
        drilldown.GetProperty("items").GetArrayLength().Should().Be(1);
        drilldown.GetProperty("items")[0].GetProperty("segmentId").GetGuid().Should().Be(segment);
        var itemFiles = await Body(await client.GetAsync(path + "/items?metric=verifiedSourceBytes"));
        itemFiles.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid()).Should().Contain(file).And.HaveCount(2);
        (await client.GetAsync(path + "/summary?from=2030-01-01T00:00:00Z")).StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await client.GetAsync(path + $"/summary?routeVersionId={Guid.NewGuid()}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetAsync(path + $"/summary?segmentSetId={Guid.NewGuid()}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var timelinePath = path + $"/timeline?aggregateType=SurveyRequest&aggregateId={task}&from=2030-01-01T00:00:00Z&to=2030-01-02T00:00:00Z&pageSize=1";
        var pageOne = await Body(await client.GetAsync(timelinePath)); var firstEvent = pageOne.GetProperty("items")[0].GetProperty("eventId").GetGuid();
        var cursor = pageOne.GetProperty("nextCursor").GetString(); cursor.Should().NotBeNull();
        var responseTwo = await client.GetAsync(timelinePath + "&cursor=" + Uri.EscapeDataString(cursor!));
        var pageTwo = await Body(responseTwo); pageTwo.GetProperty("items").GetArrayLength().Should().Be(1);
        pageTwo.GetProperty("items")[0].GetProperty("eventId").GetGuid().Should().NotBe(firstEvent);
        pageTwo.GetProperty("nextCursor").ValueKind.Should().Be(JsonValueKind.Null);
        var timelineText = await responseTwo.Content.ReadAsStringAsync(); timelineText.Should().NotContain("PRIVATE").And.NotContain("person@example").And.NotContain("token").And.NotContain("afterSnapshot");
        (await client.GetAsync(timelinePath.Replace("2030-01-02", "2030-01-03") + "&cursor=" + Uri.EscapeDataString(cursor!))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.GetAsync(path + $"/timeline?aggregateType=SurveyRequest&aggregateId={Guid.NewGuid()}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        await using (var db = sql.CreateDbContext())
        {
            long[] after = [await db.AuditLogs.LongCountAsync(), await db.IdempotencyRecords.LongCountAsync(), await db.OutboxMessages.LongCountAsync(), await db.Files.LongCountAsync(),
                await db.SurveyDataVersions.LongCountAsync(), await db.Set<BaselineCurrentPointer>().LongCountAsync()];
            after.Should().Equal(before);
            var original = await (from d in db.SurveyDataVersions join s in db.Surveys on d.SurveyId equals s.Id where s.ProjectId == project orderby d.VersionNo select d).FirstAsync();
            var validFile = await db.Files.SingleAsync(x => x.Id == file);
            var incorrectManifest = JsonSerializer.Serialize(new[] { new { fileId = file, checksumSha256 = new string('c', 64), sizeBytes = validFile.SizeBytes, mediaType = validFile.MimeType, purpose = "SURVEY_VIDEO" } });
            // Adversarial fixture only: production admission would reject this metadata mismatch.
            var inconsistent = SurveyDataVersion.CreateSubmitted(Guid.NewGuid(), original.SurveyId, 3, from, from, original.DeviceId!.Value, incorrectManifest, original.ScopeManifest!);
            inconsistent.SetSubmissionProvenance(validFile.UploadedByUserId!.Value, original.PairsManifest!); db.SurveyDataVersions.Add(inconsistent);
            // Existing assessment pointers must not inflate a superseded route's denominator or numerator.
            (await db.RoadSectionVersions.SingleAsync(x => x.Id == route)).ClearCurrent(); await db.SaveChangesAsync();
        }
        var partial = await Body(await client.GetAsync(path + "/summary"));
        var partialBytes = partial.GetProperty("metrics").EnumerateArray().Single(m => m.GetProperty("code").GetString() == "verifiedSourceBytes");
        partialBytes.GetProperty("availability").GetString().Should().Be("PARTIAL");
        partialBytes.GetProperty("value").GetInt64().Should().Be(4L * 1024 * 1024 * 1024);
        partialBytes.GetProperty("reasonCodes").EnumerateArray().Select(r => r.GetString()).Should().Contain("SOURCE_MANIFEST_METADATA_MISMATCH");
        var stale = await Body(await client.GetAsync(path + $"/summary?segmentSetId={set}"));
        stale.GetProperty("metrics").EnumerateArray().Where(m => m.GetProperty("code").GetString() == "baselineCoverageByBand")
            .Should().OnlyContain(m => m.GetProperty("denominator").GetInt64() == 0 && m.GetProperty("value").ValueKind == JsonValueKind.Null);
        await using (var db = sql.CreateDbContext())
        {
            var membership = await db.ProjectMembers.SingleAsync(x => x.ProjectId == project && x.UserId == manager);
            membership.Status = ProjectMemberStatus.Ended; await db.SaveChangesAsync();
        }
        (await client.GetAsync(path + "/summary")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
