using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.Repositories.Implementations.Defects;
using Xunit;

namespace RoadGuardSystem.ApiTests.Repairs;

public sealed class AssignSourceResponsibilityHttpTests(AuthenticationSqlServerFixture sql) : IClassFixture<AuthenticationSqlServerFixture>
{
    private static readonly byte[] SourcePng = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+j1ioAAAAASUVORK5CYII=");
    [Fact]
    public async Task FieldReuseHistoryDiscoversImmutableProvenanceWithoutChangingFileOwnership()
    {
        await using var db = sql.CreateDbContext();
        var source = await GenuineProtectedSource.Seed(db, sql, SourcePng);
        var link = await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyDefectSourceLink>().SingleAsync(row => row.DefectId == source.Defect && row.EndedAt == null);
        var report = await db.Reports.AsNoTracking().SingleAsync(row => row.Id == link.ReportSourceId);
        var evidence = Assert.Single(report.OriginalEvidence);
        var file = await db.Files.AsNoTracking().SingleAsync(row => row.Id == evidence.FileId);
        var version = Convert.ToBase64String(await db.Defects.Where(row => row.Id == source.Defect).Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync());
        await using var minio = await OwnedWebMinio.StartAsync(createBucket: true);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString,
            allowExternalStorage: true, configureTestServices: minio.Configure);
        await OwnedWebMinio.WriteVerifiedObject(factory.Services, file.StorageUri, file.MimeType, SourcePng);
        async Task<HttpClient> Web(Guid id) => await WebCookieTestClient.Login(factory, await db.Users.Where(row => row.Id == id).Select(row => row.Email!).SingleAsync());
        using var pm = await Web(source.Pm); using var crew = await Web(source.Crew);
        using var task = await Post(pm, $"/api/v1/projects/{source.Project}/field-inspection-tasks", new
        {
            defectId = source.Defect,
            defectVersion = version,
            sourceKind = "REPORTER",
            routeVersionId = source.Route,
            segmentSetId = source.Set,
            purpose = "PRE_MEASUREMENT",
            requiredMeasurementType = 4,
            measurementScope = "{}",
            assignedToUserId = source.Crew,
            dueAt = DateTimeOffset.UtcNow.AddDays(1)
        });
        var taskId = (await Body(task, 201)).GetProperty("id").GetGuid();
        var path = $"/api/v1/projects/{source.Project}/field-inspection-tasks/{taskId}";
        var input = new { fileId = file.Id, sourceKind = "REPORTER", sourceEvidenceId = evidence.Id, expectedChecksum = file.Checksum, reason = "PM authorizes retained BEFORE source" };
        var key = Guid.NewGuid().ToString();
        using var reused = await Post(pm, path + "/evidence-reuse", input, task.Headers.ETag!.Tag, key);
        var decision = await Body(reused, 201);
        using var replay = await Post(pm, path + "/evidence-reuse", input, task.Headers.ETag!.Tag, key);
        Assert.Equal(decision.GetProperty("id").GetGuid(), (await Body(replay, 200)).GetProperty("id").GetGuid());
        using var reloaded = await Web(source.Pm);
        var history = await Body(await reloaded.GetAsync(path + "/history"));
        var fact = Assert.Single(history.GetProperty("details").GetProperty("evidenceReuse").EnumerateArray());
        Assert.Equal(decision.GetProperty("id").GetGuid(), fact.GetProperty("id").GetGuid());
        Assert.Equal(evidence.Id, fact.GetProperty("sourceEvidenceId").GetGuid());
        Assert.Equal(source.Pm, fact.GetProperty("actorId").GetGuid());
        Assert.Equal(file.Checksum, fact.GetProperty("fileChecksum").GetString());
        Assert.Equal(evidence.FileVersion, fact.GetProperty("provenance").GetProperty("fileVersion").GetString());
        Assert.Equal(input.reason, fact.GetProperty("reason").GetString());
        using var bytes = await reloaded.GetAsync(path + $"/evidence/{file.Id}/content");
        Assert.Equal(HttpStatusCode.OK, bytes.StatusCode); Assert.Equal(SourcePng, await bytes.Content.ReadAsByteArrayAsync());
        var crewHistory = await Body(await crew.GetAsync(path + "/history"));
        Assert.False(crewHistory.TryGetProperty("details", out _));
        using var wrongSource = await Post(pm, path + "/evidence-reuse", input with { sourceEvidenceId = Guid.NewGuid() }, task.Headers.ETag!.Tag);
        Assert.Equal(HttpStatusCode.Forbidden, wrongSource.StatusCode);
        Assert.Equal(report.ReporterUserId, (await db.FileScopes.AsNoTracking().SingleAsync(row => row.FileId == file.Id)).OwnerUserId);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReporterAndSurveyAssignmentsPreserveOriginAfterAcceptedTransfer(bool surveySource)
    {
        await using var db = sql.CreateDbContext();
        var source = await GenuineProtectedSource.Seed(db, sql, surveySource ? null : SourcePng);
        var now = DateTimeOffset.UtcNow;
        var defectId = source.Defect; Guid? surveyId = null; Guid? datasetId = null; Guid? surveyTaskId = null;
        if (surveySource)
        {
            var op = await sql.CreateUserAsync("assign-op-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.DroneOperator);
            var task = SurveyRequest.Create(Guid.NewGuid(), source.Project, source.Road, null, source.Pm, SurveyType.Periodic, SurveyRequestStatus.Submitted, now);
            var survey = Survey.Create(Guid.NewGuid(), task.Id, source.Project, source.Route, SurveyType.Periodic, SurveyStatus.InProgress, false, null, null);
            var data = SurveyDataVersion.Create(Guid.NewGuid(), survey.Id, 1, SurveyDataVersionStatus.Draft, SurveyDataIntegrityStatus.Pending, null, null, "[]");
            var block = ProcessingBlock.Create(Guid.NewGuid(), data.Id, 1, "{}");
            var model = AIModelVersion.Create(Guid.NewGuid(), "MOCK fixture", Guid.NewGuid().ToString("N"), "fixture://local", null, null, AIModelVersionStatus.Draft, null, null);
            var job = ProcessingJob.Create(Guid.NewGuid(), block.Id, model.Id, ProcessingJobStatus.Completed, now.AddMinutes(-1), now, null, null);
            db.AddRange(task, survey, data, block, model, job, SurveyAssignment.Create(Guid.NewGuid(), task.Id, op.Id, source.Pm, now, now, null, null, null, null));
            await db.SaveChangesAsync();
            var original = await db.Defects.SingleAsync(x => x.Id == source.Defect);
            var detection = AIDetection.Create(Guid.NewGuid(), job.Id, model.Id, source.Route, original.Geometry!, original.DefectTypeCode, .8m, null, null, "{}");
            db.Add(detection); await db.SaveChangesAsync();
            var accepted = await new CandidateDecisionRepository(db).SaveAcceptedAsync(source.Pm,
                CandidateSourceFacts.Create(CandidateSourceIdentity.Create(CandidateSourceKind.AiDetection, detection.Id, "SQL lineage fixture"), source.Project, "geometry fixture"),
                CandidateDecisionKind.KeepNew, CandidateClassification.Create(source.Route, original.DefectTypeCode, null, DefectSeverity.Low, null), null, null, null, "Fixture PM KEEP_NEW", null, default);
            defectId = accepted.DefectId!.Value; surveyId = survey.Id; datasetId = data.Id; surveyTaskId = task.Id;
        }
        var receiverPm = await sql.CreateUserAsync("receiver-pm-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var receiverCrew = await sql.CreateUserAsync("receiver-crew-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.RepairCrew);
        var receiver = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString("N"), "Receiving project", null, 32648, null, null, now);
        db.AddRange(receiver, ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), receiver.Id, receiverPm.Id, new(2000, 1, 1)),
            new ProjectMember { Id = Guid.NewGuid(), ProjectId = receiver.Id, UserId = receiverCrew.Id, RoleCode = UserRoleCode.RepairCrew, Status = ProjectMemberStatus.Active, ValidFrom = new(2000, 1, 1) });
        await db.SaveChangesAsync();
        await using var minio = surveySource ? null : await OwnedWebMinio.StartAsync(createBucket: true);
        using var logs = new InMemoryLogProvider();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, loggerProvider: logs, allowExternalStorage: !surveySource,
            configureTestServices: services =>
            {
                if (minio is not null) minio.Configure(services);
                else { services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(new ScopedReadStorage()); }
            });
        if (minio is not null)
        {
            var reportId = await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyDefectSourceLink>().Where(l => l.DefectId == source.Defect && l.EndedAt == null).Select(l => l.ReportSourceId).SingleAsync();
            var originalFile = await db.Reports.Where(r => r.Id == reportId).SelectMany(r => r.OriginalEvidence).Select(e => e.FileId).SingleAsync();
            var stored = await db.Files.SingleAsync(f => f.Id == originalFile);
            await OwnedWebMinio.WriteVerifiedObject(factory.Services, stored.StorageUri, stored.MimeType, SourcePng);
        }
        async Task<HttpClient> Web(Guid actor) => await WebCookieTestClient.Login(factory, await db.Users.Where(x => x.Id == actor).Select(x => x.Email!).SingleAsync());
        using var pm = await Web(source.Pm); using var sup = await Web(source.Supervisor); using var receivingPm = await Web(receiverPm.Id);
        using var crew = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        using var login = await crew.PostAsJsonAsync("/api/v1/auth/login", new { email = await db.Users.Where(x => x.Id == source.Crew).Select(x => x.Email).SingleAsync(), password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode); crew.DefaultRequestHeaders.Authorization = new("Bearer", (await Body(login)).GetProperty("accessToken").GetString());
        if (datasetId is Guid dataset)
        {
            var read = await Body(await pm.GetAsync($"/api/v1/datasets/{dataset}"));
            Assert.Equal(surveyId, read.GetProperty("surveyId").GetGuid());
            Assert.NotEqual(read.GetProperty("surveyId").GetGuid(), read.GetProperty("surveyTaskId").GetGuid());
        }
        var defect = await db.Defects.AsNoTracking().SingleAsync(x => x.Id == defectId);
        var version = Convert.ToBase64String(await db.Defects.Where(x => x.Id == defectId).Select(x => EF.Property<byte[]>(x, "RowVersion")).SingleAsync());
        var root = $"/api/v1/projects/{source.Project}/repair-packages";
        object Scope(decimal from) => new { physicalRoadId = source.Road, routeVersionId = source.Route, segmentSetId = source.Set, fromMeters = from, toMeters = from + 1, offsetFromMeters = 0, offsetToMeters = 1 };
        var packageResponse = await Post(pm, root, new
        {
            defectId,
            defectVersion = version,
            obligations = new[] {
            new { kind = "FORMAL_REPAIR", mandatory = true, scope = Scope(1), reason = "Before transfer" },
            new { kind = "FORMAL_REPAIR", mandatory = true, scope = Scope(3), reason = "After transfer" } },
            reason = "Source boundary HTTP test"
        }, '"' + version + '"');
        var package = (await Body(packageResponse, 201)).GetProperty("id").GetGuid();
        var obligations = await db.RepairObligations.AsNoTracking().Where(x => x.DefectId == defectId).OrderBy(x => x.Scope.From).Select(x => x.Id).ToArrayAsync();
        async Task<(Guid Id, string Version)> Propose(Guid obligation)
        {
            var packages = await Body(await pm.GetAsync(root));
            var packageVersion = packages.GetProperty("items").EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == package).GetProperty("version").GetString();
            var proposal = await Post(pm, root + $"/{package}/items", new { obligationId = obligation, mode = "FAST_TRACK", repairPlan = "Test plan", checklistVersion = "assign-v1", reason = "Test proposal" }, '"' + packageVersion + '"');
            return ((await Body(proposal, 201)).GetProperty("id").GetGuid(), proposal.Headers.ETag!.Tag);
        }
        var first = await Propose(obligations[0]); var second = await Propose(obligations[1]);
        var policyRoot = $"/api/v1/projects/{source.Project}/repair-policies";
        async Task<Guid> Policy()
        {
            var draft = await Post(pm, policyRoot + "/drafts", new { defectTypeCode = defect.DefectTypeCode, checklistVersion = "assign-v1", measurements = Array.Empty<object>(), stopConditions = Array.Empty<string>(), reason = "Test policy" });
            var draftId = (await Body(draft, 201)).GetProperty("id").GetGuid();
            var published = await Post(pm, policyRoot + $"/drafts/{draftId}/publish", new { reason = "Publish fixture policy" }, draft.Headers.ETag!.Tag);
            return (await Body(published, 201)).GetProperty("id").GetGuid();
        }
        var policyId = await Policy();
        object Input(Guid assignedCrew, Guid? survey, Guid route, string defectVersion, Guid policy) => new
        {
            task = new
            {
                defectId,
                defectVersion,
                surveyId = survey,
                sourceKind = surveySource ? "SURVEY" : "REPORTER",
                routeVersionId = route,
                segmentSetId = source.Set,
                purpose = "POST_REPAIR",
                requiredMeasurementType = 1,
                measurementScope = "{}",
                assignedToUserId = assignedCrew,
                dueAt = now.AddDays(1)
            },
            policyRevisionId = policy,
            reason = "Assign fixture task"
        };
        // Both source types use the same published/checklist policy guard.
        var policyGet = await pm.GetAsync(policyRoot + $"/revisions/{policyId}");
        await Body(await Post(pm, policyRoot + $"/revisions/{policyId}/revoke", new { reason = "Test revoke" }, policyGet.Headers.ETag!.Tag), 201);
        var firstPath = root + $"/{package}/items/{first.Id}";
        Assert.Equal(HttpStatusCode.Conflict, (await Post(pm, firstPath + "/assign", Input(source.Crew, surveyId, source.Route, version, policyId), first.Version)).StatusCode);
        policyId = await Policy();
        var wrongChecklist = await Post(pm, policyRoot + "/drafts", new { defectTypeCode = defect.DefectTypeCode, checklistVersion = "wrong-checklist", measurements = Array.Empty<object>(), stopConditions = Array.Empty<string>(), reason = "Mismatch fixture" });
        var wrongDraftId = (await Body(wrongChecklist, 201)).GetProperty("id").GetGuid();
        var wrongPublished = await Post(pm, policyRoot + $"/drafts/{wrongDraftId}/publish", new { reason = "Test wrong checklist" }, wrongChecklist.Headers.ETag!.Tag);
        var wrongPolicyId = (await Body(wrongPublished, 201)).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict, (await Post(pm, firstPath + "/assign", Input(source.Crew, surveyId, source.Route, version, wrongPolicyId), first.Version)).StatusCode);

        using var wrongRole = await Post(sup, firstPath + "/assign", Input(source.Crew, surveyId, source.Route, version, policyId), first.Version);
        Assert.True(wrongRole.StatusCode == HttpStatusCode.Forbidden, await wrongRole.Content.ReadAsStringAsync() + logs.CombinedEntries);
        Assert.Equal(HttpStatusCode.Conflict, (await Post(pm, firstPath + "/assign", Input(source.Crew, surveyTaskId ?? Guid.NewGuid(), source.Route, version, policyId), first.Version)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Post(pm, firstPath + "/assign", Input(source.Crew, surveyId, Guid.NewGuid(), version, policyId), first.Version)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Post(pm, firstPath + "/assign", Input(source.Crew, surveyId, source.Route, Convert.ToBase64String(new byte[8]), policyId), first.Version)).StatusCode);
        var key = Guid.NewGuid().ToString("N"); var body = Input(source.Crew, surveyId, source.Route, version, policyId);
        var assigned = await Post(pm, firstPath + "/assign", body, first.Version, key); await Body(assigned, 201);
        await Body(await Post(pm, firstPath + "/assign", body, first.Version, key));
        Assert.Equal(HttpStatusCode.Conflict, (await Post(pm, firstPath + "/assign", body, first.Version)).StatusCode);
        var eligibility = await Body(await pm.GetAsync(firstPath + "/eligibility"));
        Assert.False(eligibility.GetProperty("eligible").GetBoolean());

        var exportA = $"/api/v1/projects/{source.Project}/exports";
        var exportKey = Guid.NewGuid().ToString("N");
        var beforeTransferExport = await Body(await Post(pm, exportA,
            new { kind = "DOSSIER", format = "PDF", repairItemIds = new[] { second.Id } }, key: exportKey), 202);
        var exportId = beforeTransferExport.GetProperty("id").GetGuid();
        var lifeA = $"/api/v1/projects/{source.Project}/lifecycle"; var lifeB = $"/api/v1/projects/{receiver.Id}/lifecycle";
        using var sourceLife = await sup.GetAsync(lifeA); var transferable = (await Body(sourceLife)).GetProperty("transferableObligations").EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == obligations[1]);
        var offered = await Post(sup, lifeA + "/obligation-transfers", new { reason = "Exact scope transfer", evidenceFileIds = Array.Empty<Guid>(), obligationId = obligations[1], receivingProjectId = receiver.Id, scopeHash = transferable.GetProperty("scopeHash").GetString() }, sourceLife.Headers.ETag!.Tag);
        var offer = (await Body(offered, 201)).GetProperty("actions").EnumerateArray().Single(x => x.GetProperty("kind").GetString() == "IssueTransfer");
        using var targetLife = await sup.GetAsync(lifeB);
        Assert.Equal(HttpStatusCode.NotFound, (await receivingPm.GetAsync($"/api/v1/projects/{receiver.Id}/repair-packages/{package}/items/{second.Id}/source-defect")).StatusCode);
        var acceptedTransfer = await Post(sup, lifeB + "/obligation-transfer-acceptances", new { reason = "Accept exact scope", evidenceFileIds = Array.Empty<Guid>(), sourceActionId = offer.GetProperty("id").GetGuid(), obligationId = obligations[1], scopeHash = offer.GetProperty("scopeHash").GetString() }, targetLife.Headers.ETag!.Tag);
        await Body(acceptedTransfer, 201);
        foreach (var suffix in new[] { "", "/manifest", "/content" })
            Assert.Equal(HttpStatusCode.Forbidden, (await pm.GetAsync(exportA + $"/{exportId}" + suffix)).StatusCode);
        await Body(await Post(pm, exportA, new { kind = "DOSSIER", format = "PDF", repairItemIds = new[] { second.Id } }, key: exportKey), 403);
        // A still owns the sibling item, but a package export must not silently omit B's item.
        Assert.Equal("export_source_unavailable", (await Body(await Post(pm, exportA,
            new { kind = "DOSSIER", format = "PDF", repairPackageIds = new[] { package } }), 422)).GetProperty("code").GetString());
        Assert.Equal("export_source_unavailable", (await Body(await Post(receivingPm,
            $"/api/v1/projects/{receiver.Id}/exports", new { kind = "DOSSIER", format = "PDF", repairItemIds = new[] { second.Id } }), 422)).GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await receivingPm.GetAsync(exportA + $"/{exportId}")).StatusCode);
        using (var exportWorker = factory.Services.CreateScope())
            Assert.True(await exportWorker.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Exports.IExportService>().ProcessNextAsync(default));
        using (var exportVerification = factory.Services.CreateScope())
        {
            var exportDb = exportVerification.ServiceProvider.GetRequiredService<RoadGuardSystem.Repositories.RoadGuardDbContext>();
            var failed = await exportDb.Set<RoadGuardSystem.BusinessObjects.Exports.ExportJob>().SingleAsync(j => j.Id == exportId);
            Assert.Equal("FAILED", failed.Status); Assert.Equal("export_source_access_revoked", failed.ErrorCode);
            var frozen = await exportDb.Set<RoadGuardSystem.BusinessObjects.Exports.ExportSnapshot>().SingleAsync(s => s.Id == failed.SnapshotId);
            Assert.Equal(beforeTransferExport.GetProperty("snapshotHash").GetString(), frozen.Hash);
            Assert.Single(await exportDb.Set<RoadGuardSystem.BusinessObjects.Exports.ExportJob>().Where(j => j.ProjectId == source.Project).ToArrayAsync());
        }
        foreach (var path in new[] { lifeA, lifeB }) Assert.Contains((await Body(await sup.GetAsync(path))).GetProperty("responsibilities").EnumerateArray(), x => x.GetProperty("obligationId").GetGuid() == obligations[1] && x.GetProperty("currentProjectId").GetGuid() == receiver.Id);
        var received = await Body(await receivingPm.GetAsync(lifeB));
        var responsibility = received.GetProperty("responsibilities").EnumerateArray().Single(x => x.GetProperty("obligationId").GetGuid() == obligations[1]);
        Assert.Equal(package, responsibility.GetProperty("packageId").GetGuid());
        Assert.Equal(second.Id, responsibility.GetProperty("currentRepairItemId").GetGuid());
        var secondPath = $"/api/v1/projects/{receiver.Id}/repair-packages/{responsibility.GetProperty("packageId").GetGuid()}/items/{responsibility.GetProperty("currentRepairItemId").GetGuid()}";
        var itemList = await Body(await receivingPm.GetAsync($"/api/v1/projects/{receiver.Id}/repair-packages/{package}/items"));
        Assert.Equal(second.Id, Assert.Single(itemList.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        using var receivedItemResponse = await receivingPm.GetAsync(secondPath);
        var receivedItem = await Body(receivedItemResponse);
        var receivedItemVersion = receivedItemResponse.Headers.ETag!.Tag;
        Assert.Equal(second.Id, receivedItem.GetProperty("id").GetGuid());
        var sourceContext = receivedItem.GetProperty("sourceContext");
        Assert.Equal(source.Project, sourceContext.GetProperty("originProjectId").GetGuid());
        Assert.Equal(receiver.Id, sourceContext.GetProperty("responsibilityProjectId").GetGuid());
        Assert.Equal(defectId, sourceContext.GetProperty("defectId").GetGuid());
        var sourceDefect = await Body(await receivingPm.GetAsync(secondPath + "/source-defect"));
        Assert.Equal(defectId, sourceDefect.GetProperty("defectId").GetGuid());
        string? retainedPhotoPath = null;
        if (!surveySource)
        {
            retainedPhotoPath = Assert.Single(sourceDefect.GetProperty("evidence").EnumerateArray()).GetProperty("contentPath").GetString();
            using var sourcePhoto = await receivingPm.GetAsync(retainedPhotoPath);
            Assert.Equal(HttpStatusCode.OK, sourcePhoto.StatusCode);
            Assert.Equal(SourcePng, await sourcePhoto.Content.ReadAsByteArrayAsync());
            Assert.Equal(HttpStatusCode.Forbidden, (await pm.GetAsync(retainedPhotoPath)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await receivingPm.GetAsync(retainedPhotoPath!.Replace(second.Id.ToString(), first.Id.ToString(), StringComparison.Ordinal))).StatusCode);
        }
        Assert.Equal(HttpStatusCode.OK, (await receivingPm.GetAsync(secondPath + "/planning")).StatusCode);
        var commonPlanning = await Body(await receivingPm.GetAsync($"/api/v1/projects/{receiver.Id}/repair-packages/{package}/planning"));
        Assert.Equal(JsonValueKind.Null, commonPlanning.GetProperty("description").ValueKind);
        Assert.Equal(source.Route, sourceContext.GetProperty("routeVersionId").GetGuid());
        Assert.Equal(HttpStatusCode.NotFound, (await receivingPm.GetAsync(secondPath.Replace(second.Id.ToString(), first.Id.ToString(), StringComparison.Ordinal))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Post(pm, root + $"/{package}/items/{second.Id}/assign", Input(source.Crew, surveyId, source.Route, version, policyId), second.Version)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await pm.GetAsync(root + $"/{package}/items/{second.Id}")).StatusCode);
        // Current policy preserves original task scope. A receiver-only Crew is not silently granted source project access.
        var deniedCrew = await Post(receivingPm, secondPath + "/assign", Input(receiverCrew.Id, surveyId, source.Route, version, policyId), receivedItemVersion);
        Assert.Equal("assignee_not_authorized", (await Body(deniedCrew, 403)).GetProperty("code").GetString());
        var afterInput = Input(source.Crew, surveyId, source.Route, version, policyId);
        var afterKey = Guid.NewGuid().ToString("N");
        var after = await Post(receivingPm, secondPath + "/assign", afterInput, receivedItemVersion, afterKey);
        var afterBody = await Body(after, 201);
        Assert.Equal(second.Id, afterBody.GetProperty("itemId").GetGuid());
        Assert.Equal(obligations[1], afterBody.GetProperty("obligationId").GetGuid());
        Assert.Equal("CONDITIONAL_FT", afterBody.GetProperty("mode").GetString());
        Assert.Equal(policyId, afterBody.GetProperty("policyRevisionId").GetGuid());
        Assert.Equal(source.Route, afterBody.GetProperty("routeVersionId").GetGuid());
        Assert.Equal(source.Set, afterBody.GetProperty("segmentSetId").GetGuid());
        Assert.NotEqual(Guid.Empty, afterBody.GetProperty("authorizationId").GetGuid());
        Assert.Equal(afterBody.GetRawText(), (await Body(await Post(receivingPm, secondPath + "/assign", afterInput, receivedItemVersion, afterKey))).GetRawText());
        db.ChangeTracker.Clear();
        var binding = await db.Set<RepairFieldTaskBinding>().SingleAsync(x => x.ItemId == second.Id);
        Assert.Equal(source.Project, binding.ProjectId); Assert.Equal(policyId, binding.PolicyRevisionId);
        Assert.Equal(binding.TaskId, afterBody.GetProperty("taskId").GetGuid());
        Assert.Equal(binding.AssignmentId, afterBody.GetProperty("assignmentId").GetGuid());
        Assert.Equal(binding.AuthorizationId, afterBody.GetProperty("authorizationId").GetGuid());
        var readBound = await Body(await receivingPm.GetAsync(secondPath));
        Assert.Equal(binding.Id, readBound.GetProperty("currentBinding").GetProperty("id").GetGuid());
        Assert.Equal(binding.TaskId, readBound.GetProperty("sourceContext").GetProperty("task").GetProperty("taskId").GetGuid());
        Assert.Equal(policyId, readBound.GetProperty("currentBinding").GetProperty("policyRevisionId").GetGuid());
        var sourceReads = secondPath + $"/source-tasks/{binding.TaskId}";
        Assert.Equal(HttpStatusCode.OK, (await receivingPm.GetAsync(sourceReads + "/geometry")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await receivingPm.GetAsync(sourceReads + "/history")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await receivingPm.GetAsync(sourceReads.Replace(binding.TaskId.ToString(), Guid.NewGuid().ToString(), StringComparison.Ordinal) + "/history")).StatusCode);
        var fieldPath = $"/api/v1/projects/{source.Project}/field-inspection-tasks/{binding.TaskId}";
        var field = await Body(await crew.GetAsync(fieldPath)); Assert.Equal("CONDITIONAL_FT", field.GetProperty("mode").GetString());
        var mobileList = await Body(await crew.GetAsync("/api/v1/me/inspection-tasks?mode=CONDITIONAL_FT"));
        var mobileTask = mobileList.GetProperty("items").EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == binding.TaskId);
        Assert.Equal(field.GetProperty("mode").GetString(), mobileTask.GetProperty("mode").GetString());
        using var currentTask = await crew.GetAsync(fieldPath);
        await Body(await Post(crew, fieldPath + "/accept", new { reason = "Crew accepts retained-source task" }, currentTask.Headers.ETag!.Tag), 201);
        await db.ProjectMembers.Where(x => x.ProjectId == receiver.Id && x.UserId == receiverPm.Id).ExecuteUpdateAsync(set => set.SetProperty(x => x.Status, ProjectMemberStatus.Ended));
        Assert.Equal(HttpStatusCode.Forbidden, (await receivingPm.GetAsync(secondPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await receivingPm.GetAsync(sourceReads + "/history")).StatusCode);
        if (retainedPhotoPath is not null) Assert.Equal(HttpStatusCode.Forbidden, (await receivingPm.GetAsync(retainedPhotoPath)).StatusCode);
        await db.ProjectMembers.Where(x => x.ProjectId == source.Project && x.UserId == source.Crew).ExecuteUpdateAsync(set => set.SetProperty(x => x.Status, ProjectMemberStatus.Ended));
        Assert.Equal(HttpStatusCode.Forbidden, (await crew.GetAsync(fieldPath)).StatusCode);
    }

    [Fact]
    public async Task SwaggerDocumentsMeasurementCodesAndAssignmentDiscoveryFields()
    {
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        var api = factory.Services.GetRequiredService<Swashbuckle.AspNetCore.Swagger.ISwaggerProvider>().GetSwagger("v1");
        var measurement = api.Components.Schemas["FieldTaskCreateInput"].Properties["requiredMeasurementType"];
        Assert.Equal(1, measurement.Minimum); Assert.Equal(6, measurement.Maximum);
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, measurement.Enum.Cast<Microsoft.OpenApi.Any.OpenApiInteger>().Select(x => x.Value));
        Assert.Contains("Unknown 0", measurement.Description);
        Assert.True(api.Components.Schemas["DatasetDetailView"].Properties["surveyId"].Nullable);
        Assert.Contains("packageId", api.Components.Schemas["ObligationResponsibilityDto"].Properties.Keys);
        Assert.Contains("currentRepairItemId", api.Components.Schemas["ObligationResponsibilityDto"].Properties.Keys);
    }

    private static async Task<HttpResponseMessage> Post(HttpClient client, string path, object input, string? etag = null, string? key = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(input) };
        request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString("N"));
        if (etag is not null) request.Headers.TryAddWithoutValidation("If-Match", etag);
        return await client.SendAsync(request);
    }
    private sealed class ScopedReadStorage : IUploadObjectStorage
    {
        public Task<string> InitiateAsync(string objectKey, string mediaType, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(string objectKey, string uploadId, IReadOnlyList<int> partNumbers, DateTimeOffset expiresAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<UploadObjectVerification> CompleteAndVerifyAsync(string objectKey, string uploadId, IReadOnlyList<CompletedStoragePart> parts, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(new MemoryStream([1, 2, 3, 4]));
    }
    private static async Task<JsonElement> Body(HttpResponseMessage response, int expected = 200)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True((int)response.StatusCode == expected, $"Expected {expected}, got {(int)response.StatusCode}: {text}");
        return JsonDocument.Parse(text).RootElement.Clone();
    }
}
