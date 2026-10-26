using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.aBusinessObjects.Commons;
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
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
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
        var intake = await Send(crew, executionPath + "/attempts", new
        {
            fieldSubmissionId = submissionId,
            expectedContentHash = submissionBody.GetProperty("contentHash").GetString(),
            executionFinishId
        },
            Guid.NewGuid().ToString(), await Version(pm, itemPath));
        Assert.True(intake.StatusCode == HttpStatusCode.Created, await intake.Content.ReadAsStringAsync());
        var reviewed = await Send(pm, executionPath + "/attempts/reviews", new
        {
            fieldSubmissionId = submissionId,
            decision = "ACCEPT",
            reason = "source-backed genuine Normal review"
        }, Guid.NewGuid().ToString(), await Version(pm, itemPath));
        Assert.True(reviewed.StatusCode == HttpStatusCode.Created, await reviewed.Content.ReadAsStringAsync());
        var finalKey = Guid.NewGuid().ToString(); var finalVersion = await Version(pm, itemPath);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(pm, itemPath + "/final-confirmation", new { reason = "wrong role" },
            Guid.NewGuid().ToString(), finalVersion)).StatusCode);
        var final = await Send(supervisor, itemPath + "/final-confirmation", new { reason = "verified Normal final" }, finalKey, finalVersion);
        Assert.True(final.StatusCode == HttpStatusCode.Created, await final.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await Send(supervisor, itemPath + "/final-confirmation", new { reason = "verified Normal final" }, finalKey, finalVersion)).StatusCode);
        await using (var persisted = sql.CreateDbContext())
        {
            var resolved = await persisted.RepairItems.SingleAsync(x => x.Id == item);
            Assert.Equal(RepairItemState.Confirmed, resolved.State);
            Assert.True((await persisted.RepairObligations.SingleAsync(x => x.Id == resolved.ObligationId)).IsResolved);
            Assert.True((await persisted.RepairPackages.Include(x => x.Obligations).SingleAsync(x => x.Id == package)).IsComplete);
            Assert.NotNull((await persisted.Set<DeadlineClock>().SingleAsync(x => x.TargetId == item && x.Kind == DeadlineClockKind.SupervisorFinalConfirmation)).CompletedAt);
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await crew.GetAsync(root)).StatusCode);
        // Current project admission denies unknown/unauthorized projects before resource lookup.
        Assert.Equal(HttpStatusCode.Forbidden, (await pm.GetAsync(itemPath.Replace(source.Project.ToString(), Guid.NewGuid().ToString()))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await pm.GetAsync(itemPath.Replace(package.ToString(), Guid.NewGuid().ToString()))).StatusCode);
        await db.ProjectMembers.Where(x => x.ProjectId == source.Project && x.UserId == source.Pm)
            .ExecuteUpdateAsync(x => x.SetProperty(m => m.Status, ProjectMemberStatus.Ended));
        Assert.Equal(HttpStatusCode.Forbidden, (await pm.GetAsync(root)).StatusCode);
        Assert.Single(await db.Set<RepairFieldTaskBinding>().Where(x => x.ItemId == item).ToArrayAsync());

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
