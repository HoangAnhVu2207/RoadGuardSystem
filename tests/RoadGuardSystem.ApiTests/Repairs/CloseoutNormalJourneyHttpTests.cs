using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RoadGuardSystem.API.Workers;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Services.Exports;
using Xunit;

namespace RoadGuardSystem.ApiTests.Repairs;

// Source fixture supplies retained Reporter/Defect lineage and test geometry only.
// Every package/proposal/approval/assignment below goes through production HTTP/DI.
public sealed class CloseoutNormalJourneyHttpTests(AuthenticationSqlServerFixture sql)
    : IClassFixture<AuthenticationSqlServerFixture>
{
    [Fact]
    public async Task PopulatedCollectionsFollowActualNormalProposalApprovalAssignmentAndCurrentAuthority()
    {
        await using var db = sql.CreateDbContext();
        var source = await GenuineProtectedSource.Seed(db, sql);
        var useMinio = MinioSmokeFactAttribute.IsConfigured();
        using var logs = new InMemoryLogProvider();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, allowExternalStorage: useMinio,
            loggerProvider: logs,
            configureTestServices: services =>
            {
                // This journey drains the real dispatcher explicitly after assignment.
                var worker = services.Single(x => x.ServiceType == typeof(IHostedService)
                    && x.ImplementationType == typeof(H6NotificationWorker));
                services.Remove(worker);
                services.PostConfigure<ExportOptions>(options =>
                    options.UnicodeFontPath = Environment.GetEnvironmentVariable("RG_TEST_UNICODE_FONT") ?? options.UnicodeFontPath);
            });
        using var pm = await Client(source.Pm);
        using var supervisor = await Client(source.Supervisor);
        using var crew = await Client(source.Crew);
        var root = $"/api/v1/projects/{source.Project}/repair-packages";
        var defectVersion = Convert.ToBase64String(await db.Defects.Where(x => x.Id == source.Defect)
            .Select(x => EF.Property<byte[]>(x, "RowVersion")).SingleAsync());
        var created = await Send(pm, root, new
        {
            defectId = source.Defect,
            defectVersion,
            obligations = new[] { new { kind = "FORMAL_REPAIR", mandatory = true,
                scope = new { physicalRoadId = source.Road, routeVersionId = source.Route, segmentSetId = source.Set,
                    fromMeters = 1, toMeters = 2, offsetFromMeters = 0, offsetToMeters = 1 }, reason = "controlled actual scope" } },
            reason = "closeout Normal HTTP producer"
        }, Guid.NewGuid().ToString(), '"' + defectVersion + '"');
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var package = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var page = await pm.GetFromJsonAsync<JsonElement>(root + "?limit=1");
        Assert.Equal(package, page.GetProperty("items")[0].GetProperty("id").GetGuid());
        var obligation = await db.RepairObligations.Where(x => x.DefectId == source.Defect).Select(x => x.Id).SingleAsync();
        var proposed = await Send(pm, root + $"/{package}/items", new
        { obligationId = obligation, mode = "NORMAL", repairPlan = "controlled Normal plan", checklistVersion = "v1", reason = "propose" },
            Guid.NewGuid().ToString(), created.Headers.ETag!.ToString());
        Assert.Equal(HttpStatusCode.Created, proposed.StatusCode);
        var item = (await proposed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var itemPath = root + $"/{package}/items/{item}";
        var detail = await supervisor.GetAsync(itemPath);
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        var key = Guid.NewGuid().ToString();
        var approval = await Send(supervisor, itemPath + "/approve", new { reason = "current Supervisor decision" }, key, detail.Headers.ETag!.ToString());
        Assert.Equal(HttpStatusCode.Created, approval.StatusCode);
        var replay = await Send(supervisor, itemPath + "/approve", new { reason = "current Supervisor decision" }, key, detail.Headers.ETag!.ToString());
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        var stale = await Send(supervisor, itemPath + "/approve", new { reason = "stale decision" }, Guid.NewGuid().ToString(), detail.Headers.ETag!.ToString());
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        var current = await pm.GetAsync(itemPath);
        var assigned = await Send(pm, itemPath + "/assign", new
        {
            task = new
            {
                defectId = source.Defect,
                defectVersion,
                sourceKind = "REPORTER",
                routeVersionId = source.Route,
                segmentSetId = source.Set,
                purpose = "POST_REPAIR",
                requiredMeasurementType = 1,
                measurementScope = "{}",
                assignedToUserId = source.Crew,
                dueAt = DateTimeOffset.UtcNow.AddDays(1)
            },
            reason = "actual Crew assignment"
        }, Guid.NewGuid().ToString(), current.Headers.ETag!.ToString());
        Assert.Equal(HttpStatusCode.Created, assigned.StatusCode);
        db.ChangeTracker.Clear();
        var binding = await db.Set<RepairFieldTaskBinding>().SingleAsync(x => x.ItemId == item);
        Assert.Equal("NORMAL", (await db.FieldInspectionTasks.SingleAsync(x => x.Id == binding.TaskId)).TaskMode);
        Assert.Equal(source.Crew, (await db.FieldInspectionAssignments.SingleAsync(x => x.Id == binding.AssignmentId)).AssignedToUserId);
        var listed = await pm.GetFromJsonAsync<JsonElement>(root + $"/{package}/items?mode=NORMAL");
        Assert.Equal(item, listed.GetProperty("items")[0].GetProperty("id").GetGuid());
        // Continue the SAME genuine Normal item through production HTTP/DI to durable final.
        var taskPath = $"/api/v1/projects/{source.Project}/field-inspection-tasks/{binding.TaskId}";
        await using (var dispatchScope = factory.Services.CreateAsyncScope())
        {
            var dispatch = new RoadGuardSystem.Services.Messaging.H6NotificationDispatchService(dispatchScope.ServiceProvider.GetRequiredService<RoadGuardSystem.Repositories.Messaging.IH6NotificationDispatchRepository>());
            for (var i = 0; i < 20; i++) { var outcome = await dispatch.ProcessOneAsync(); if (outcome.Status == "IDLE") break; }
        }
        var crewNoticeId = await db.Notifications.AsNoTracking().Where(x => x.RecipientUserId == source.Crew && x.SourceEntityType == "RepairWork" && x.SourceEntityId == item).Select(x => x.Id).SingleAsync();
        var navigation = await crew.GetFromJsonAsync<JsonElement>($"/api/v1/notifications/{crewNoticeId}/scope");
        Assert.Equal(binding.TaskId, navigation.GetProperty("taskId").GetGuid());
        Assert.Equal(source.Project, navigation.GetProperty("taskProjectId").GetGuid());
        Assert.Equal("repair.work.assigned.v1", navigation.GetProperty("eventCode").GetString());
        var crewList = await crew.GetFromJsonAsync<JsonElement>("/api/v1/me/inspection-tasks?mode=NORMAL");
        var crewSummary = Assert.Single(crewList.GetProperty("items").EnumerateArray());
        Assert.Equal(binding.TaskId, crewSummary.GetProperty("id").GetGuid());
        Assert.Equal("NORMAL", crewSummary.GetProperty("mode").GetString());
        var taskDetail = await crew.GetFromJsonAsync<JsonElement>(taskPath);
        Assert.Equal(taskDetail.GetProperty("mode").GetString(), crewSummary.GetProperty("mode").GetString());
        var executionPath = itemPath + $"/tasks/{binding.TaskId}";
        Assert.Equal(HttpStatusCode.Created, (await Send(crew, taskPath + "/accept", new { reason = "accept genuine Normal task" },
            Guid.NewGuid().ToString(), await Version(crew, taskPath))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Send(crew, taskPath + "/start", new { originId = Guid.NewGuid(), claimedAt = DateTimeOffset.UtcNow },
            Guid.NewGuid().ToString(), await Version(crew, taskPath))).StatusCode);
        var task = await crew.GetFromJsonAsync<JsonElement>(taskPath);
        var firstStart = task.GetProperty("firstStartOriginId").GetGuid();
        var assessment = await Send(crew, executionPath + "/assessment", new { originId = Guid.NewGuid(), fieldFirstStartId = firstStart },
            Guid.NewGuid().ToString(), await Version(pm, itemPath));
        Assert.True(assessment.StatusCode == HttpStatusCode.Created, await assessment.Content.ReadAsStringAsync());
        var assessmentId = (await assessment.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var execution = await Send(crew, executionPath + "/execution-start", new
        {
            originId = Guid.NewGuid(),
            fieldFirstStartId = firstStart,
            claimedAt = DateTimeOffset.UtcNow,
            assessmentId
        }, Guid.NewGuid().ToString(), await Version(pm, itemPath));
        Assert.True(execution.StatusCode == HttpStatusCode.Created, await execution.Content.ReadAsStringAsync());
        var executionStartId = (await execution.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var finished = await Send(crew, executionPath + "/execution-finish", new
        {
            originId = Guid.NewGuid(),
            executionStartId,
            claimedAt = DateTimeOffset.UtcNow
        }, Guid.NewGuid().ToString(), await Version(pm, itemPath));
        Assert.True(finished.StatusCode == HttpStatusCode.Created, await finished.Content.ReadAsStringAsync());
        var executionFinishId = (await finished.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        // Controlled verified storage lineage only; this test does not assert real MinIO bytes.
        var now = DateTimeOffset.UtcNow;
        var after = StoredFile.Create(Guid.NewGuid(), "private/normal-closeout/" + Guid.NewGuid(), "after.jpg", "image/jpeg", 4,
            new string('c', 64), source.Crew, now, null);
        var upload = UploadSession.Create(Guid.NewGuid(), after.Id, source.Crew, after.StorageUri, "AFTER", "image/jpeg", 4,
            after.Checksum, 8388608, now.AddHours(24));
        upload.StartUploading("fixture", now);
        db.AddRange(after, upload, FileScope.Create(Guid.NewGuid(), after.Id, source.Project, binding.TaskId, source.Crew, "AFTER", now));
        await db.SaveChangesAsync(); upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now);
        await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var submission = await Send(crew, taskPath + "/submissions", new
        {
            originId = Guid.NewGuid(),
            startOriginId = firstStart,
            captureType = "REPAIR_CLAIM",
            repaired = true,
            measurements = new[] { new { sampleId = "depth", type = "DepressionDepth", value = 0, state = "KNOWN", dimension = "LENGTH",
                unit = "mm", locationReason = "on road", instrument = "depth gauge", method = "depth measurement" } },
            evidence = new[] { new { captureOriginId = Guid.NewGuid(), fileId = after.Id, purpose = "AFTER", checksumSha256 = after.Checksum,
                mediaType = after.MimeType, capturedAt = DateTimeOffset.UtcNow, attemptChecklist = "POST_REPAIR_CAPTURE" } },
            locationProof = new
            {
                kind = "POSITION_CHECKLIST",
                checklist = "ROUTE_CHAINAGE_MARKINGS_CONFIRMED",
                observedRouteVersionId = source.Route,
                observedChainageMeters = 1
            }
        }, Guid.NewGuid().ToString(), await Version(crew, taskPath));
        Assert.True(submission.StatusCode == HttpStatusCode.Created, await submission.Content.ReadAsStringAsync());
        var submissionBody = await submission.Content.ReadFromJsonAsync<JsonElement>();
        var submissionId = submissionBody.GetProperty("id").GetGuid();
        var sourceReadPath = itemPath + $"/source-tasks/{binding.TaskId}/submissions/";
        async Task<JsonElement> ReadSubmission(Guid readId)
        {
            using var direct = await pm.GetAsync(taskPath + "/submissions/" + readId);
            using var scoped = await pm.GetAsync(sourceReadPath + readId);
            Assert.Equal(HttpStatusCode.OK, direct.StatusCode); Assert.Equal(HttpStatusCode.OK, scoped.StatusCode);
            var a = await direct.Content.ReadAsStringAsync(); var b = await scoped.Content.ReadAsStringAsync();
            Assert.Equal(a, b);
            if (Environment.GetEnvironmentVariable("RG_SCOPE_HANDOFF_EVIDENCE") is { } directory)
            {
                Directory.CreateDirectory(directory);
                await File.WriteAllTextAsync(Path.Combine(directory, $"field-{readId}.json"), a);
            }
            return JsonDocument.Parse(a).RootElement.Clone();
        }
        var atIntake = await ReadSubmission(submissionId);
        Assert.Contains(atIntake.GetProperty("missingReasons").EnumerateArray(), x => x.GetString() == "AFTER_ATTEMPT_BINDING_UNAVAILABLE");
        Assert.False(atIntake.GetProperty("currentRepair").GetProperty("isEffectiveSubmission").GetBoolean());
        Assert.Equal(1, atIntake.GetProperty("locationProof").GetProperty("observedChainageMeters").GetDouble());

        var intake = await Send(crew, executionPath + "/attempts", new
        {
            fieldSubmissionId = submissionId,
            expectedContentHash = submissionBody.GetProperty("contentHash").GetString(),
            executionFinishId
        },
            Guid.NewGuid().ToString(), await Version(pm, itemPath));
        Assert.True(intake.StatusCode == HttpStatusCode.Created, await intake.Content.ReadAsStringAsync());
        var initialContext = (await crew.GetFromJsonAsync<JsonElement>(taskPath)).GetProperty("repairContext");
        Assert.Equal(JsonValueKind.Null, initialContext.GetProperty("attempt").GetProperty("review").ValueKind);
        var attemptId = initialContext.GetProperty("attempt").GetProperty("attemptId").GetGuid();
        async Task RequestSupplement(string reason)
        {
            var key = Guid.NewGuid().ToString(); var version = await Version(pm, itemPath);
            var body = new { fieldSubmissionId = submissionId, decision = "SUPPLEMENT", reason };
            using var review = await Send(pm, executionPath + "/attempts/reviews", body, key, version);
            Assert.True(review.StatusCode == HttpStatusCode.Created, await review.Content.ReadAsStringAsync());
            var reviewId = (await review.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("reviewId").GetGuid();
            using var newCrewSession = await Client(source.Crew);
            foreach (var session in new[] { crew, newCrewSession })
            {
                var current = (await session.GetFromJsonAsync<JsonElement>(taskPath)).GetProperty("repairContext").GetProperty("attempt");
                Assert.Equal(attemptId, current.GetProperty("attemptId").GetGuid());
                Assert.Equal(submissionId, current.GetProperty("effectiveSubmissionId").GetGuid());
                var actual = current.GetProperty("review");
                Assert.Equal(reviewId, actual.GetProperty("reviewId").GetGuid());
                Assert.Equal("SUPPLEMENT", actual.GetProperty("decision").GetString());
                Assert.Equal(reason, actual.GetProperty("reason").GetString());
            }
            await using var persisted = sql.CreateDbContext();
            var stored = await persisted.Set<RepairAttemptReview>().SingleAsync(x => x.Id == reviewId);
            Assert.Equal(submissionId, stored.SubmissionId); Assert.Equal(attemptId, stored.AttemptId); Assert.Equal(reason, stored.Reason);
            using var historyResponse = await pm.GetAsync(itemPath + "/history");
            Assert.True(historyResponse.StatusCode == HttpStatusCode.OK, await historyResponse.Content.ReadAsStringAsync() + logs.CombinedEntries);
            var history = await historyResponse.Content.ReadFromJsonAsync<JsonElement>();
            var historicReview = history.GetProperty("attemptReviews").EnumerateArray().Single(row => row.GetProperty("id").GetGuid() == reviewId);
            Assert.Equal(submissionId, historicReview.GetProperty("submissionId").GetGuid());
            Assert.Equal(attemptId, historicReview.GetProperty("attemptId").GetGuid());
            Assert.Equal(reason, historicReview.GetProperty("reason").GetString());
            Assert.True(historicReview.GetProperty("current").GetBoolean());
        }
        await RequestSupplement("Bổ sung ảnh xác nhận phạm vi sửa");
        var supplementReviewRead = await ReadSubmission(submissionId);
        Assert.Equal("SUPPLEMENT", supplementReviewRead.GetProperty("currentRepair").GetProperty("reviewDecision").GetString());

        var otherCrew = await sql.CreateUserAsync("reason-other-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.RepairCrew);
        using var unauthorizedCrew = await Client(otherCrew.Id);
        Assert.Equal(HttpStatusCode.Forbidden, (await unauthorizedCrew.GetAsync(taskPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await crew.GetAsync(taskPath.Replace(source.Project.ToString(), Guid.NewGuid().ToString(), StringComparison.Ordinal))).StatusCode);
        using var revised = await Send(crew, taskPath + "/submissions", new
        {
            originId = Guid.NewGuid(),
            startOriginId = firstStart,
            parentSubmissionId = submissionId,
            captureType = "REPAIR_CLAIM",
            repaired = true,
            measurements = Array.Empty<object>(),
            evidence = Array.Empty<object>()
        }, Guid.NewGuid().ToString(), await Version(crew, taskPath));
        Assert.True(revised.StatusCode == HttpStatusCode.Created, await revised.Content.ReadAsStringAsync());
        var revisedBody = await revised.Content.ReadFromJsonAsync<JsonElement>();
        var revisedId = revisedBody.GetProperty("id").GetGuid();
        using var linkedSupplement = await Send(crew, executionPath + "/attempts/supplements", new
        { fieldSubmissionId = revisedId, expectedContentHash = revisedBody.GetProperty("contentHash").GetString() },
            Guid.NewGuid().ToString(), await Version(pm, itemPath));
        Assert.True(linkedSupplement.StatusCode == HttpStatusCode.Created, await linkedSupplement.Content.ReadAsStringAsync());
        var updatedAttempt = (await crew.GetFromJsonAsync<JsonElement>(taskPath)).GetProperty("repairContext").GetProperty("attempt");
        Assert.Equal(attemptId, updatedAttempt.GetProperty("attemptId").GetGuid());
        Assert.Equal(revisedId, updatedAttempt.GetProperty("effectiveSubmissionId").GetGuid());
        Assert.Equal(JsonValueKind.Null, updatedAttempt.GetProperty("review").ValueKind);
        var supplementedRead = await ReadSubmission(submissionId);
        Assert.Equal(JsonValueKind.Null, supplementedRead.GetProperty("currentRepair").GetProperty("reviewId").ValueKind);
        var afterSupplementHistory = await pm.GetFromJsonAsync<JsonElement>(itemPath + "/history");
        Assert.All(afterSupplementHistory.GetProperty("attemptReviews").EnumerateArray(), row => Assert.False(row.GetProperty("current").GetBoolean()));
        var effectiveIntake = Assert.Single(afterSupplementHistory.GetProperty("intakeSubmissions").EnumerateArray().Where(row => row.GetProperty("effective").GetBoolean()));
        Assert.Equal(revisedId, effectiveIntake.GetProperty("submissionId").GetGuid());
        Assert.Equal(attemptId, effectiveIntake.GetProperty("attemptId").GetGuid());
        submissionId = revisedId;
        await RequestSupplement("Kiểm tra lại ảnh bổ sung của bản mới");
        // A second supplement must clear the review again before ACCEPT can target the new intake.
        using var finalRevision = await Send(crew, taskPath + "/submissions", new
        {
            originId = Guid.NewGuid(),
            startOriginId = firstStart,
            parentSubmissionId = submissionId,
            captureType = "REPAIR_CLAIM",
            repaired = true,
            measurements = new[] { new { sampleId = "depth", type = "DepressionDepth", value = 0, state = "KNOWN", dimension = "LENGTH",
                unit = "mm", locationReason = "on road", instrument = "depth gauge", method = "depth measurement" } },
            evidence = new[] { new { captureOriginId = Guid.NewGuid(), fileId = after.Id, purpose = "AFTER", checksumSha256 = after.Checksum,
                mediaType = after.MimeType, capturedAt = DateTimeOffset.UtcNow, attemptChecklist = "POST_REPAIR_CAPTURE" } },
            locationProof = new
            {
                kind = "POSITION_CHECKLIST",
                checklist = "ROUTE_CHAINAGE_MARKINGS_CONFIRMED",
                observedRouteVersionId = source.Route,
                observedChainageMeters = 1
            }
        }, Guid.NewGuid().ToString(), await Version(crew, taskPath));
        Assert.True(finalRevision.StatusCode == HttpStatusCode.Created, await finalRevision.Content.ReadAsStringAsync());
        var finalRevisionBody = await finalRevision.Content.ReadFromJsonAsync<JsonElement>();
        submissionId = finalRevisionBody.GetProperty("id").GetGuid();
        using var finalLink = await Send(crew, executionPath + "/attempts/supplements", new
        { fieldSubmissionId = submissionId, expectedContentHash = finalRevisionBody.GetProperty("contentHash").GetString() },
            Guid.NewGuid().ToString(), await Version(pm, itemPath));
        Assert.True(finalLink.StatusCode == HttpStatusCode.Created, await finalLink.Content.ReadAsStringAsync());
        var reviewed = await Send(pm, executionPath + "/attempts/reviews", new
        {
            fieldSubmissionId = submissionId,
            decision = "ACCEPT",
            reason = "source-backed genuine Normal review"
        }, Guid.NewGuid().ToString(), await Version(pm, itemPath));
        Assert.True(reviewed.StatusCode == HttpStatusCode.Created, await reviewed.Content.ReadAsStringAsync());
        var afterReview = (await crew.GetFromJsonAsync<JsonElement>(taskPath)).GetProperty("repairContext");
        Assert.Equal("ACCEPT", afterReview.GetProperty("attempt").GetProperty("review").GetProperty("decision").GetString());
        Assert.Equal(JsonValueKind.Null, afterReview.GetProperty("attempt").GetProperty("review").GetProperty("reason").ValueKind);
        Assert.Equal(JsonValueKind.Null, afterReview.GetProperty("finalDecision").ValueKind);
        var acceptedRead = await ReadSubmission(submissionId);
        Assert.Equal("ACCEPT", acceptedRead.GetProperty("currentRepair").GetProperty("reviewDecision").GetString());
        Assert.Equal(JsonValueKind.Null, acceptedRead.GetProperty("currentRepair").GetProperty("finalDecisionId").ValueKind);
        Assert.Equal(atIntake.GetProperty("missingReasons").GetRawText(), acceptedRead.GetProperty("missingReasons").GetRawText());
        var finalKey = Guid.NewGuid().ToString(); var finalVersion = await Version(pm, itemPath);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(pm, itemPath + "/final-confirmation", new { reason = "wrong role" },
            Guid.NewGuid().ToString(), finalVersion)).StatusCode);
        var final = await Send(supervisor, itemPath + "/final-confirmation", new { reason = "verified Normal final" }, finalKey, finalVersion);
        Assert.True(final.StatusCode == HttpStatusCode.Created, await final.Content.ReadAsStringAsync());
        var confirmedRead = await ReadSubmission(submissionId);
        Assert.Equal("CONFIRMED", confirmedRead.GetProperty("currentRepair").GetProperty("finalDecisionResult").GetString());
        Assert.Empty(confirmedRead.GetProperty("currentRepair").GetProperty("missingReasons").EnumerateArray());
        Assert.Equal(atIntake.GetProperty("missingReasons").GetRawText(), confirmedRead.GetProperty("missingReasons").GetRawText());

        var completedScopeRead = await pm.GetFromJsonAsync<JsonElement>(root + $"/eligibility?defectId={source.Defect}&scope.physicalRoadId={source.Road}&scope.routeVersionId={source.Route}&scope.segmentSetId={source.Set}&scope.fromMeters=1&scope.toMeters=2&scope.offsetFromMeters=0&scope.offsetToMeters=1");
        Assert.False(completedScopeRead.GetProperty("eligible").GetBoolean());
        Assert.Equal("completed_scope_conflict", completedScopeRead.GetProperty("code").GetString());
        var duplicateScope = new { physicalRoadId = source.Road, routeVersionId = source.Route, segmentSetId = source.Set, fromMeters = 1m, toMeters = 2m, offsetFromMeters = 0m, offsetToMeters = 1m };
        var defectEtag = '"' + Convert.ToBase64String(await db.Defects.Where(x => x.Id == source.Defect).Select(x => EF.Property<byte[]>(x, "RowVersion")).SingleAsync()) + '"';
        using var completedDuplicate = await Send(pm, root, new { defectId = source.Defect, defectVersion = defectEtag.Trim('"'), obligations = new[] { new { kind = "FORMAL_REPAIR", mandatory = true, scope = duplicateScope, reason = "must not reserve completed scope again" } }, reason = "completed duplicate" }, Guid.NewGuid().ToString(), defectEtag);
        Assert.Equal(HttpStatusCode.Conflict, completedDuplicate.StatusCode);
        Assert.Equal("completed_scope_conflict", (await completedDuplicate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        Assert.Equal(HttpStatusCode.OK, (await Send(supervisor, itemPath + "/final-confirmation", new { reason = "verified Normal final" }, finalKey, finalVersion)).StatusCode);
        await using (var persisted = sql.CreateDbContext())
        {
            var resolved = await persisted.RepairItems.SingleAsync(x => x.Id == item);
            Assert.Equal(RepairItemState.Confirmed, resolved.State);
            Assert.True((await persisted.RepairObligations.SingleAsync(x => x.Id == resolved.ObligationId)).IsResolved);
            Assert.True((await persisted.RepairPackages.Include(x => x.Obligations).SingleAsync(x => x.Id == package)).IsComplete);
            Assert.NotNull((await persisted.Set<DeadlineClock>().SingleAsync(x => x.TargetId == item && x.Kind == DeadlineClockKind.SupervisorFinalConfirmation)).CompletedAt);
        }
        var afterFinal = (await crew.GetFromJsonAsync<JsonElement>(taskPath)).GetProperty("repairContext");
        Assert.Equal("CONFIRMED", afterFinal.GetProperty("finalDecision").GetProperty("result").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await crew.GetAsync(root)).StatusCode);
        var exports = $"/api/v1/projects/{source.Project}/exports";
        var archived = await CaptureDossier(new { kind = "DOSSIER", format = "PDF", repairItemIds = new[] { item } });
        var originalManifest = await ReadManifest(archived);
        Assert.Equal("AVAILABLE", Acceptance(originalManifest));
        var originalPdf = useMinio ? await RenderAndRead(archived) : null;
        var finalDecision = (await (await pm.GetAsync(itemPath)).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("effectiveDecisionId").GetGuid();
        var correction = await Send(supervisor, itemPath + "/corrections", new
        {
            supersedesDecisionId = finalDecision,
            result = "UNREPAIRED",
            reason = "Reopen after site review",
            basis = new { text = "Physical finish no longer meets acceptance criteria", evidenceReferenceIds = Array.Empty<Guid>() }
        }, Guid.NewGuid().ToString(), await Version(supervisor, itemPath));
        Assert.True(correction.StatusCode == HttpStatusCode.Created, await correction.Content.ReadAsStringAsync());
        var afterCorrection = (await crew.GetFromJsonAsync<JsonElement>(taskPath)).GetProperty("repairContext");
        Assert.Equal("UNREPAIRED", afterCorrection.GetProperty("finalDecision").GetProperty("result").GetString());
        var correctedRead = await ReadSubmission(submissionId);
        Assert.NotEqual("CONFIRMED", correctedRead.GetProperty("currentRepair").GetProperty("finalDecisionResult").GetString());
        Assert.Contains(correctedRead.GetProperty("currentRepair").GetProperty("missingReasons").EnumerateArray(), x => x.GetString() == "FINAL_CONFIRMATION_NOT_EFFECTIVE");
        Assert.NotEqual(afterFinal.GetProperty("finalDecision").GetProperty("decisionId").GetGuid(), afterCorrection.GetProperty("finalDecision").GetProperty("decisionId").GetGuid());
        Assert.Equal(originalManifest.GetRawText(), (await ReadManifest(archived)).GetRawText());
        var refreshedId = await CaptureDossier(new { kind = "DOSSIER", format = "PDF", repairPackageIds = new[] { package } });
        var refreshed = await ReadManifest(refreshedId);
        Assert.Equal("UNAVAILABLE", Acceptance(refreshed));
        Assert.Equal(2, refreshed.GetProperty("sourceRevisions").EnumerateArray().Count(row => row.GetProperty("kind").GetString() == "RepairDecision"));
        if (originalPdf is not null)
        {
            var revisedPdf = await RenderAndRead(refreshedId);
            Assert.False(originalPdf.AsSpan().SequenceEqual(revisedPdf));
            Assert.Equal(originalPdf, await pm.GetByteArrayAsync($"{exports}/{archived}/content"));
        }
        // Accepted responsibility grants exact source reads, not membership in the origin project.
        var receiverPm = await sql.CreateUserAsync("field-reader-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var receiver = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString("N"), "Exact FIELD responsibility read", null, 32648, null, null, DateTimeOffset.UtcNow);
        db.AddRange(receiver, ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), receiver.Id, receiverPm.Id, new(2000, 1, 1)));
        await db.SaveChangesAsync();
        using var receivingPm = await Client(receiverPm.Id);
        var obligationId = await db.RepairItems.Where(x => x.Id == item).Select(x => x.ObligationId).SingleAsync();
        var lifeA = $"/api/v1/projects/{source.Project}/lifecycle"; var lifeB = $"/api/v1/projects/{receiver.Id}/lifecycle";
        using var beforeOffer = await supervisor.GetAsync(lifeA);
        var transferScope = (await beforeOffer.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("transferableObligations").EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == obligationId);
        using var offered = await Send(supervisor, lifeA + "/obligation-transfers", new { reason = "Exact FIELD read transfer", evidenceFileIds = Array.Empty<Guid>(), obligationId, receivingProjectId = receiver.Id, scopeHash = transferScope.GetProperty("scopeHash").GetString() }, Guid.NewGuid().ToString(), beforeOffer.Headers.ETag!.Tag);
        Assert.True(offered.StatusCode == HttpStatusCode.Created, await offered.Content.ReadAsStringAsync());
        var offer = (await offered.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("actions").EnumerateArray().Single(x => x.GetProperty("kind").GetString() == "IssueTransfer");
        using var beforeAccept = await supervisor.GetAsync(lifeB);
        using var acceptedTransfer = await Send(supervisor, lifeB + "/obligation-transfer-acceptances", new { reason = "Accept exact FIELD source read", evidenceFileIds = Array.Empty<Guid>(), sourceActionId = offer.GetProperty("id").GetGuid(), obligationId, scopeHash = offer.GetProperty("scopeHash").GetString() }, Guid.NewGuid().ToString(), beforeAccept.Headers.ETag!.Tag);
        Assert.True(acceptedTransfer.StatusCode == HttpStatusCode.Created, await acceptedTransfer.Content.ReadAsStringAsync());
        var hiddenConflict = await pm.GetFromJsonAsync<JsonElement>(root + $"/eligibility?defectId={source.Defect}&scope.physicalRoadId={source.Road}&scope.routeVersionId={source.Route}&scope.segmentSetId={source.Set}&scope.fromMeters=1&scope.toMeters=2&scope.offsetFromMeters=0&scope.offsetToMeters=1");
        Assert.False(hiddenConflict.GetProperty("eligible").GetBoolean());
        Assert.Equal("active_scope_conflict", hiddenConflict.GetProperty("code").GetString());
        foreach (var name in new[] { "obligationId", "packageId", "itemId", "mode", "status" })
            Assert.Equal(JsonValueKind.Null, hiddenConflict.GetProperty(name).ValueKind);
        var receivedPath = $"/api/v1/projects/{receiver.Id}/repair-packages/{package}/items/{item}/source-tasks/{binding.TaskId}/submissions/{submissionId}";
        using var receivedSubmission = await receivingPm.GetAsync(receivedPath);
        Assert.Equal(HttpStatusCode.OK, receivedSubmission.StatusCode);
        Assert.Equal(correctedRead.GetRawText(), (await receivedSubmission.Content.ReadFromJsonAsync<JsonElement>()).GetRawText());
        Assert.Equal(HttpStatusCode.Forbidden, (await receivingPm.GetAsync(taskPath + "/submissions/" + submissionId)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await receivingPm.GetAsync(receivedPath.Replace(item.ToString(), Guid.NewGuid().ToString(), StringComparison.Ordinal))).StatusCode);
        var transferredTimeline = await receivingPm.GetFromJsonAsync<JsonElement>($"/api/v1/projects/{receiver.Id}/reports/timeline?aggregateType=Project&aggregateId={receiver.Id}");
        foreach (var action in new[] { "repair_execution_start", "repair_execution_finish", "repair_attempt_reviewed", "repair_final_confirmed", "repair_decision_corrected" })
            Assert.Contains(transferredTimeline.GetProperty("items").EnumerateArray(), x => x.GetProperty("action").GetString() == action);
        Assert.Contains(transferredTimeline.GetProperty("items").EnumerateArray(), x => x.GetProperty("source").GetProperty("type").GetString() == "RepairItem" && x.GetProperty("source").GetProperty("id").GetGuid() == item);
        await db.ProjectMembers.Where(x => x.ProjectId == receiver.Id && x.UserId == receiverPm.Id).ExecuteUpdateAsync(x => x.SetProperty(m => m.Status, ProjectMemberStatus.Ended));
        Assert.Equal(HttpStatusCode.Forbidden, (await receivingPm.GetAsync(receivedPath)).StatusCode);

        // Current project admission denies unknown/unauthorized projects before resource lookup.
        Assert.Equal(HttpStatusCode.Forbidden, (await pm.GetAsync(itemPath.Replace(source.Project.ToString(), Guid.NewGuid().ToString()))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await pm.GetAsync(itemPath.Replace(package.ToString(), Guid.NewGuid().ToString()))).StatusCode);
        await db.ProjectMembers.Where(x => x.ProjectId == source.Project && x.UserId == source.Pm)
            .ExecuteUpdateAsync(x => x.SetProperty(m => m.Status, ProjectMemberStatus.Ended));
        Assert.Equal(HttpStatusCode.Forbidden, (await pm.GetAsync(root)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await pm.GetAsync($"{exports}/{archived}/manifest")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await pm.GetAsync($"{exports}/{archived}/content")).StatusCode);
        Assert.Single(await db.Set<RepairFieldTaskBinding>().Where(x => x.ItemId == item).ToArrayAsync());
        var currentAssignment = await db.FieldInspectionAssignments.SingleAsync(x => x.Id == binding.AssignmentId);
        currentAssignment.End(DateTimeOffset.UtcNow, "reason readback permission regression");
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await crew.GetAsync(taskPath)).StatusCode);

        async Task<Guid> CaptureDossier(object filters)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, exports) { Content = JsonContent.Create(filters) };
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
            using var response = await pm.SendAsync(request);
            Assert.True(response.StatusCode == HttpStatusCode.Accepted, await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        }
        async Task<JsonElement> ReadManifest(Guid exportId)
        {
            using var response = await pm.GetAsync($"{exports}/{exportId}/manifest");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }
        async Task<byte[]> RenderAndRead(Guid exportId)
        {
            using var serviceScope = factory.Services.CreateScope();
            Assert.True(await serviceScope.ServiceProvider.GetRequiredService<IExportService>().ProcessNextAsync(default));
            using var response = await pm.GetAsync($"{exports}/{exportId}/content");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var bytes = await response.Content.ReadAsByteArrayAsync();
            Assert.True(bytes.AsSpan().StartsWith("%PDF-"u8));
            return bytes;
        }
        string? Acceptance(JsonElement manifest) => manifest.GetProperty("sections").EnumerateArray()
            .Single(row => row.GetProperty("name").GetString() == $"item/{item}/currentAcceptance").GetProperty("availability").GetString();

        async Task<HttpClient> Client(Guid actor)
        {
            var user = await db.Users.AsNoTracking().SingleAsync(x => x.Id == actor);
            var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
            var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = user.Email, password = "Current1!" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
            return client;
        }
    }

    [Fact]
    public async Task CrewReviewReasonSwaggerIsNullableAndExistingDtoDefaultsRemainCompatible()
    {
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        var swagger = factory.Services.GetRequiredService<Swashbuckle.AspNetCore.Swagger.ISwaggerProvider>().GetSwagger("v1");
        var review = swagger.Components.Schemas["CrewRepairReviewFact"];
        Assert.True(review.Properties["reason"].Nullable);
        Assert.Contains("SUPPLEMENT", review.Properties["reason"].Description);
        Assert.True(review.Properties.ContainsKey("reviewId")); Assert.True(review.Properties.ContainsKey("decision"));
        var legacy = new RoadGuardSystem.DTOs.Inspections.CrewRepairContext(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "NORMAL", "Rework", "version", "plan", "checklist",
            new(Guid.NewGuid(), Guid.NewGuid(), new(Guid.NewGuid(), "SUPPLEMENT")));
        RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.CrewRepairContextFact persisted = legacy;
        Assert.Null(persisted.Attempt!.Review!.Reason);
        RoadGuardSystem.DTOs.Inspections.CrewRepairContext restored = persisted;
        Assert.Null(restored.Attempt!.Review!.Reason);
        Assert.Equal(legacy.Attempt!.Review!.ReviewId, restored.Attempt.Review.ReviewId);
    }

    private static Task<HttpResponseMessage> Send(HttpClient client, string path, object input, string key, string etag)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(input) };
        request.Headers.Add("Idempotency-Key", key);
        request.Headers.TryAddWithoutValidation("If-Match", etag);
        return client.SendAsync(request);
    }

    private static async Task<string> Version(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return response.Headers.ETag!.ToString();
    }
}
