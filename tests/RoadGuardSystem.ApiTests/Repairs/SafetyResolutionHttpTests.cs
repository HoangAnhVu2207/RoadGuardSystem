using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Services.Files;
using Xunit;

namespace RoadGuardSystem.ApiTests.Repairs;

public sealed class SafetyResolutionHttpTests(AuthenticationSqlServerFixture sql) : IClassFixture<AuthenticationSqlServerFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActualCrewCheckAndPrivateEvidenceResolveOnlyUnderCurrentSafetyResponsibility(bool transferSafety)
    {
        await using var db = sql.CreateDbContext();
        var source = await GenuineProtectedSource.Seed(db, sql);
        var receiving = await GenuineProtectedSource.Seed(db, sql);
        await using var minio = await OwnedWebMinio.StartAsync(createBucket: true);
        using var logs = new InMemoryLogProvider();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, loggerProvider: logs,
            allowExternalStorage: true, configureTestServices: minio.Configure);
        async Task<HttpClient> Web(Guid actor) => await WebCookieTestClient.Login(factory,
            await db.Users.Where(u => u.Id == actor).Select(u => u.Email!).SingleAsync());
        using var pm = await Web(source.Pm); using var sup = await Web(source.Supervisor);
        using var otherPm = await Web(receiving.Pm); using var otherSup = await Web(receiving.Supervisor);
        using var crew = factory.CreateClient(new() { BaseAddress = new("https://localhost") });
        using var login = await crew.PostAsJsonAsync("/api/v1/auth/login", new
        { email = await db.Users.Where(u => u.Id == source.Crew).Select(u => u.Email).SingleAsync(), password = "Current1!" });
        crew.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Json(login, 200)).GetProperty("accessToken").GetString());
        var version = Convert.ToBase64String(await db.Defects.Where(d => d.Id == source.Defect)
            .Select(d => EF.Property<byte[]>(d, "RowVersion")).SingleAsync());
        var root = $"/api/v1/projects/{source.Project}/repair-packages";
        var physical = new
        {
            physicalRoadId = source.Road,
            routeVersionId = source.Route,
            segmentSetId = source.Set,
            fromMeters = 1,
            toMeters = 2,
            offsetFromMeters = 0,
            offsetToMeters = 1
        };
        using var created = await Post(pm, root, new
        {
            defectId = source.Defect,
            defectVersion = version,
            obligations = new[] { new { kind = "FORMAL_REPAIR", mandatory = true, scope = physical, reason = "formal" },
                new { kind = "TEMPORARY_SAFETY", mandatory = true, scope = physical, reason = "safety" } },
            reason = "resolution HTTP fixture"
        }, '"' + version + '"');
        var package = (await Json(created, 201)).GetProperty("id").GetGuid();
        var formal = await db.RepairObligations.Where(o => EF.Property<Guid?>(o, "PackageId") == package && o.Kind == RepairObligationKind.FormalRepair).Select(o => o.Id).SingleAsync();
        var safety = await db.RepairObligations.Where(o => EF.Property<Guid?>(o, "PackageId") == package && o.Kind == RepairObligationKind.TemporarySafety).Select(o => o.Id).SingleAsync();
        using var proposed = await Post(pm, root + $"/{package}/items", new
        {
            obligationId = formal,
            mode = "NORMAL",
            repairPlan = "Barriers around measured scope",
            checklistVersion = "safety-v1",
            reason = "plan"
        }, created.Headers.ETag!.Tag);
        var item = (await Json(proposed, 201)).GetProperty("id").GetGuid();
        var itemPath = root + $"/{package}/items/{item}";
        using var approved = await Post(sup, itemPath + "/approve", new { reason = "approve exact scope" }, proposed.Headers.ETag!.Tag);
        await Json(approved, 201);
        version = Convert.ToBase64String(await db.Defects.Where(d => d.Id == source.Defect).Select(d => EF.Property<byte[]>(d, "RowVersion")).SingleAsync());
        using var assigned = await Post(pm, itemPath + "/assign", new
        {
            task = new
            {
                defectId = source.Defect,
                defectVersion = version,
                sourceKind = "REPORTER",
                routeVersionId = source.Route,
                segmentSetId = source.Set,
                purpose = "POST_REPAIR",
                requiredMeasurementType = 4,
                measurementScope = "{}",
                assignedToUserId = source.Crew,
                dueAt = DateTimeOffset.UtcNow.AddDays(1)
            },
            reason = "assign"
        }, approved.Headers.ETag!.Tag);
        await Json(assigned, 201);
        var binding = await db.Set<RepairFieldTaskBinding>().SingleAsync(b => b.ItemId == item);
        var taskPath = $"/api/v1/projects/{source.Project}/field-inspection-tasks/{binding.TaskId}";
        using var measure = await Post(pm, itemPath + "/safety-measures", new
        {
            formalRepairObligationId = formal,
            safetyObligationId = safety,
            responsibleActorId = source.Crew,
            checkSchedule = "each shift",
            replacementCondition = "replacement barriers installed",
            removalCondition = "area made safe before removal",
            reason = "assign barrier"
        }, assigned.Headers.ETag!.Tag);
        var measureId = (await Json(measure, 201)).GetProperty("id").GetGuid();
        if (Environment.GetEnvironmentVariable("RG_SAFETY_RESOLUTION_EVIDENCE") is { } evidencePath)
            await File.WriteAllTextAsync(evidencePath + ".initial.json", await measure.Content.ReadAsStringAsync());
        var measurePath = itemPath + $"/safety-measures/{measureId}";
        using var installed = await Post(crew, measurePath + "/installation", new { firstCheckDueAt = DateTimeOffset.UtcNow.AddHours(12), reason = "installed" }, measure.Headers.ETag!.Tag);
        await Json(installed, 201);
        using var accepted = await Post(crew, taskPath + "/accept", new { reason = "accept" }, await Version(crew, taskPath)); await Json(accepted, 201);
        using var started = await Post(crew, taskPath + "/start", new { originId = Guid.NewGuid(), claimedAt = DateTimeOffset.UtcNow }, await Version(crew, taskPath)); await Json(started, 201);
        var start = await db.FieldTaskStartOrigins.Where(o => o.TaskId == binding.TaskId).Select(o => o.Id).SingleAsync();
        // Actual multipart upload, bytes and verification worker against owned MinIO.
        byte[] photo = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+j1ioAAAAASUVORK5CYII=");
        var hash = Convert.ToHexString(SHA256.HashData(photo)).ToLowerInvariant();
        using var upload = await Post(crew, "/api/v1/uploads", new
        {
            purpose = "MEASUREMENT",
            projectId = source.Project,
            targetId = binding.TaskId,
            fileName = "safe.png",
            mediaType = "image/png",
            sizeBytes = photo.Length,
            checksumSha256 = hash
        });
        var uploadBody = await Json(upload, 201); var uploadId = uploadBody.GetProperty("id").GetGuid(); var fileId = uploadBody.GetProperty("fileId").GetGuid();
        using var urls = await Post(crew, $"/api/v1/uploads/{uploadId}/part-urls", new { partNumbers = new[] { 1 } });
        var url = (await Json(urls, 200)).GetProperty("parts")[0].GetProperty("url").GetString();
        using var storageClient = new HttpClient(); using var bytes = new ByteArrayContent(photo); using var put = await storageClient.PutAsync(url, bytes); put.EnsureSuccessStatusCode();
        using var session = await crew.GetAsync($"/api/v1/uploads/{uploadId}"); var uploadVersion = (await Json(session, 200)).GetProperty("version").GetString();
        using var complete = await Post(crew, $"/api/v1/uploads/{uploadId}/complete", new { parts = new[] { new { partNumber = 1, eTag = put.Headers.ETag!.Tag } }, checksumSha256 = hash }, '"' + uploadVersion + '"');
        await Json(complete, 202);
        await using (var scope = factory.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<IUploadService>().ProcessOneVerificationAsync();
        using var verified = await crew.GetAsync($"/api/v1/files/{fileId}"); Assert.Equal("VERIFIED", (await Json(verified, 200)).GetProperty("status").GetString());
        using var downloaded = await crew.GetAsync($"/api/v1/files/{fileId}/content"); Assert.Equal(photo, await downloaded.Content.ReadAsByteArrayAsync());
        using var submitted = await Post(crew, taskPath + "/submissions", new
        {
            originId = Guid.NewGuid(),
            startOriginId = start,
            captureType = "MEASUREMENT",
            measurements = new[] { new { sampleId = "area", type = "Area", value = 0,
                state = "KNOWN", dimension = "AREA", unit = "m\u00b2", locationReason = "route markers", instrument = "area gauge", method = "manual" } },
            evidence = new[] { new { captureOriginId = Guid.NewGuid(), fileId, purpose = "MEASUREMENT", checksumSha256 = hash,
                mediaType = "image/png", capturedAt = DateTimeOffset.UtcNow } },
            locationProof = new { kind = "UNKNOWN" }
        }, await Version(crew, taskPath));
        await Json(submitted, 201);
        using var current = await crew.GetAsync(measurePath); var currentView = await Json(current, 200);
        var evidenceId = currentView.GetProperty("eligibleEvidenceLinks")[0].GetProperty("evidenceLinkId").GetGuid();
        using var checkedResponse = await Post(crew, measurePath + "/checks", new { result = "Safe", findings = "condition verified on site", evidenceIds = new[] { evidenceId } }, current.Headers.ETag!.Tag);
        var checkView = await Json(checkedResponse, 201); var check = checkView.GetProperty("currentCheckId").GetGuid();
        if (transferSafety)
        {
            var obligation = await db.RepairObligations.AsNoTracking().Include(o => o.Scope).SingleAsync(o => o.Id == safety);
            using var state = await sup.GetAsync($"/api/v1/projects/{source.Project}/lifecycle");
            using var issue = await Post(sup, $"/api/v1/projects/{source.Project}/lifecycle/obligation-transfers", new
            { obligationId = safety, receivingProjectId = receiving.Project, scopeHash = LD06LifecycleAction.HashScope(obligation), reason = "safety transfers independently", evidenceFileIds = Array.Empty<Guid>() }, state.Headers.ETag!.Tag);
            var issueId = (await Json(issue, 201)).GetProperty("actions").EnumerateArray().Single(a => a.GetProperty("kind").GetString() == "IssueTransfer").GetProperty("id").GetGuid();
            using var receiver = await otherSup.GetAsync($"/api/v1/projects/{receiving.Project}/lifecycle");
            using var receive = await Post(otherSup, $"/api/v1/projects/{receiving.Project}/lifecycle/obligation-transfer-acceptances", new
            { obligationId = safety, sourceActionId = issueId, scopeHash = LD06LifecycleAction.HashScope(obligation), reason = "accept safety", evidenceFileIds = Array.Empty<Guid>() }, receiver.Headers.ETag!.Tag);
            await Json(receive, 201);
        }
        var effective = transferSafety ? receiving.Project : source.Project; var manager = transferSafety ? otherPm : pm;
        var effectivePath = measurePath.Replace(source.Project.ToString(), effective.ToString(), StringComparison.Ordinal);
        if (transferSafety)
        {
            using var discoverySession = await Web(receiving.Pm);
            using var lifecycleRead = await discoverySession.GetAsync($"/api/v1/projects/{receiving.Project}/lifecycle");
            var responsibility = (await Json(lifecycleRead, 200)).GetProperty("responsibilities").EnumerateArray()
                .Single(r => r.GetProperty("obligationId").GetGuid() == safety);
            Assert.Equal(JsonValueKind.Null, responsibility.GetProperty("currentRepairItemId").ValueKind);
            var context = responsibility.GetProperty("safetyContext");
            Assert.Equal(package, context.GetProperty("packageId").GetGuid());
            Assert.Equal(item, context.GetProperty("itemId").GetGuid());
            Assert.Equal(measureId, context.GetProperty("measureId").GetGuid());
            Assert.Equal(formal, context.GetProperty("formalObligationId").GetGuid());
            var discoveredPath = $"/api/v1/projects/{receiving.Project}/repair-packages/{context.GetProperty("packageId").GetGuid()}/items/{context.GetProperty("itemId").GetGuid()}/safety-measures/{context.GetProperty("measureId").GetGuid()}";
            Assert.Equal(effectivePath, discoveredPath);
            using var discoveredMeasure = await discoverySession.GetAsync(discoveredPath);
            await Json(discoveredMeasure, 200);
            using var formalDenied = await discoverySession.GetAsync(itemPath.Replace(source.Project.ToString(), receiving.Project.ToString(), StringComparison.Ordinal));
            await Json(formalDenied, 404);
            using var oldLifecycle = await pm.GetAsync($"/api/v1/projects/{source.Project}/lifecycle");
            var former = (await Json(oldLifecycle, 200)).GetProperty("responsibilities").EnumerateArray()
                .Single(r => r.GetProperty("obligationId").GetGuid() == safety);
            Assert.False(former.TryGetProperty("safetyContext", out _));
        }
        using var read = await manager.GetAsync(effectivePath); var view = await Json(read, 200);
        var packageVersionBefore = await db.RepairPackages.Where(p => p.Id == package)
            .Select(p => EF.Property<byte[]>(p, "RowVersion")).SingleAsync();
        var input = new
        {
            checkId = check,
            outcome = transferSafety ? "REPLACED" : "REMOVED",
            conditionSatisfied = true,
            checklistConfirmation = "Recorded removal condition and site safety confirmed",
            reason = "PM confirms independent safety closure",
            evidenceIds = new[] { evidenceId },
            obligationVersion = view.GetProperty("obligation").GetProperty("version").GetString()
        };
        using var badCheck = await Post(manager, effectivePath + "/resolution", input with { checkId = Guid.NewGuid() }, read.Headers.ETag!.Tag);
        Assert.Equal("safety_resolution_not_ready", (await Json(badCheck, 409)).GetProperty("code").GetString());
        using var badEvidence = await Post(manager, effectivePath + "/resolution", input with { evidenceIds = new[] { fileId } }, read.Headers.ETag!.Tag);
        Assert.Equal("safety_resolution_evidence_conflict", (await Json(badEvidence, 409)).GetProperty("code").GetString());
        using var missingCondition = await Post(manager, effectivePath + "/resolution", input with { conditionSatisfied = false }, read.Headers.ETag!.Tag);
        await Json(missingCondition, 400);
        using var wrongItem = await Post(manager, effectivePath.Replace(item.ToString(), Guid.NewGuid().ToString(), StringComparison.Ordinal) + "/resolution", input, read.Headers.ETag!.Tag);
        await Json(wrongItem, 404);
        if (transferSafety)
        {
            using var oldOwner = await Post(pm, measurePath + "/resolution", input, read.Headers.ETag!.Tag);
            await Json(oldOwner, 404);
        }
        using var supDenied = await Post(sup, effectivePath + "/resolution", input, read.Headers.ETag!.Tag); Assert.Equal(HttpStatusCode.Forbidden, supDenied.StatusCode);
        using var crewDenied = await Post(crew, effectivePath + "/resolution", input, read.Headers.ETag!.Tag); Assert.Equal(HttpStatusCode.Forbidden, crewDenied.StatusCode);
        manager.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        using var csrf = await Post(manager, effectivePath + "/resolution", input, read.Headers.ETag!.Tag); Assert.Equal(HttpStatusCode.Forbidden, csrf.StatusCode);
        await WebCookieTestClient.RefreshCsrf(manager);
        using var stale = await Post(manager, effectivePath + "/resolution", input, "\"AAAAAAAAAAA=\""); Assert.Equal("concurrency_conflict", (await Json(stale, 409)).GetProperty("code").GetString());
        using var staleObligation = await Post(manager, effectivePath + "/resolution", input with { obligationVersion = "AAAAAAAAAAA=" }, read.Headers.ETag!.Tag);
        Assert.Equal("concurrency_conflict", (await Json(staleObligation, 409)).GetProperty("code").GetString());
        var key = Guid.NewGuid().ToString(); var race = await Task.WhenAll(Post(manager, effectivePath + "/resolution", input, read.Headers.ETag!.Tag, key), Post(manager, effectivePath + "/resolution", input, read.Headers.ETag!.Tag, key));
        Assert.Single(race.Where(r => r.StatusCode == HttpStatusCode.Created)); Assert.Single(race.Where(r => r.StatusCode == HttpStatusCode.OK));
        var resolution = (await Json(race.Single(r => r.StatusCode == HttpStatusCode.Created), 201)).GetProperty("resolution");
        Assert.Equal(source.Project, view.GetProperty("obligation").GetProperty("originProjectId").GetGuid());
        Assert.Equal(hash, resolution.GetProperty("evidence")[0].GetProperty("hash").GetString());
        using var fresh = await Web(transferSafety ? receiving.Pm : source.Pm); using var reopen = await fresh.GetAsync(effectivePath);
        var persisted = await Json(reopen, 200); Assert.True(persisted.GetProperty("obligation").GetProperty("resolved").GetBoolean());
        Assert.Equal(resolution.GetProperty("id").GetGuid(), persisted.GetProperty("resolution").GetProperty("id").GetGuid());
        var persistedEvidence = persisted.GetProperty("resolution").GetProperty("evidence")[0];
        Assert.Equal(hash, persistedEvidence.GetProperty("hash").GetString());
        Assert.Equal(fileId, persistedEvidence.GetProperty("fileId").GetGuid());
        Assert.Equal(evidenceId, persistedEvidence.GetProperty("evidenceLinkId").GetGuid());
        Assert.Equal(source.Crew, persistedEvidence.GetProperty("actualUploaderId").GetGuid());
        Assert.NotEqual(read.Headers.ETag!.Tag, reopen.Headers.ETag!.Tag);
        if (Environment.GetEnvironmentVariable("RG_SAFETY_RESOLUTION_EVIDENCE") is { } output)
            await File.WriteAllTextAsync(output + (transferSafety ? ".transferred.json" : ".origin.json"), await reopen.Content.ReadAsStringAsync());
        using var replay = await Post(manager, effectivePath + "/resolution", input, read.Headers.ETag!.Tag, key); await Json(replay, 200);
        using var changedReplay = await Post(manager, effectivePath + "/resolution", input with { reason = "different request" }, read.Headers.ETag!.Tag, key);
        Assert.Equal("idempotency_key_reused", (await Json(changedReplay, 409)).GetProperty("code").GetString());
        using var newCommand = await Post(manager, effectivePath + "/resolution", input, reopen.Headers.ETag!.Tag); Assert.Equal("safety_already_resolved", (await Json(newCommand, 409)).GetProperty("code").GetString());
        using var crewAfter = await crew.GetAsync(measurePath); Assert.Equal(HttpStatusCode.NotFound, crewAfter.StatusCode);
        using var history = await fresh.GetAsync(effectivePath[..effectivePath.LastIndexOf('/')]);
        Assert.Contains(measureId, (await Json(history, 200)).GetProperty("measureIds").EnumerateArray().Select(x => x.GetGuid()));
        db.ChangeTracker.Clear(); var saved = await db.RepairObligations.SingleAsync(o => o.Id == safety); Assert.True(saved.IsResolved);
        Assert.Equal(1, await db.AuditLogs.CountAsync(a => a.EntityId == safety && a.EventType == "repair_safety_resolved"));
        Assert.False((await db.RepairObligations.SingleAsync(o => o.Id == formal)).IsResolved);
        Assert.NotEqual(packageVersionBefore, await db.RepairPackages.Where(p => p.Id == package)
            .Select(p => EF.Property<byte[]>(p, "RowVersion")).SingleAsync());
        using var selection = await fresh.GetAsync(itemPath.Replace(source.Project.ToString(), effective.ToString(), StringComparison.Ordinal) + "/temporary-safety-obligations");
        if (!transferSafety) Assert.Empty((await Json(selection, 200)).GetProperty("items").EnumerateArray());
        else await Json(selection, 404); // Formal stays in A; safety ownership does not grant formal scope.
        var trigger = await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE RepairObligations SET SafetyResolutionId=NULL WHERE Id={safety}"));
        Assert.Equal(51143, trigger.Number);
        await db.ProjectMembers.Where(m => m.ProjectId == effective && m.UserId == (transferSafety ? receiving.Pm : source.Pm)).ExecuteUpdateAsync(s => s.SetProperty(m => m.Status, ProjectMemberStatus.Ended));
        using var revoked = await Post(manager, effectivePath + "/resolution", input, read.Headers.ETag!.Tag, key); Assert.Equal(HttpStatusCode.Forbidden, revoked.StatusCode);
        foreach (var r in race) r.Dispose();
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response, int expected)
    {
        var text = await response.Content.ReadAsStringAsync(); Assert.True((int)response.StatusCode == expected, text);
        return JsonSerializer.Deserialize<JsonElement>(text);
    }
    private static async Task<string> Version(HttpClient client, string path)
    { using var r = await client.GetAsync(path); await Json(r, 200); return r.Headers.ETag!.Tag; }
    private static async Task<HttpResponseMessage> Post(HttpClient client, string path, object body, string? version = null, string? key = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString());
        if (version is not null) request.Headers.TryAddWithoutValidation("If-Match", version);
        return await client.SendAsync(request);
    }
}
