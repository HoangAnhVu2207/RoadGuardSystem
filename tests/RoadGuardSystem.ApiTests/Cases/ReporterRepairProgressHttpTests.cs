using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Models.Huy01;
using Xunit;

namespace RoadGuardSystem.ApiTests.Cases;

[Collection(AuthenticationApiFixture.Name)]
public sealed class ReporterRepairProgressHttpTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task WebPmPublishesOnlyVerifiedMilestonesForTheOwner()
    {
        await using var db = sql.CreateDbContext();
        var source = await GenuineProtectedSource.Seed(db, sql);
        var incident = await db.IncidentCases.SingleAsync(row => row.ProjectId == source.Project);
        var reportId = Assert.Single(incident.ActiveReportIds);
        var reporter = await db.Reports.Where(row => row.Id == reportId).Select(row => row.ReporterUserId).SingleAsync();
        var obligation = RepairObligation.Create(Guid.NewGuid(), source.Project, source.Defect, RepairObligationKind.FormalRepair,
            true, RepairActualScope.Create(Guid.NewGuid(), source.Road, "fixture", "R", 1, 2, 0, 1));
        db.Add(RepairPackage.Create(Guid.NewGuid(), source.Project, source.Defect, [obligation]));
        await db.Defects.Where(row => row.Id == source.Defect).ExecuteUpdateAsync(set => set.SetProperty(row => row.Status, DefectStatus.Verified));
        await db.SaveChangesAsync();

        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        async Task<HttpClient> Login(Guid actor)
        {
            var email = await db.Users.Where(row => row.Id == actor).Select(row => row.Email).SingleAsync();
            return await WebCookieTestClient.Login(factory, email!);
        }
        using var pm = await Login(source.Pm);
        using var owner = await Login(reporter);
        using var sup = await Login(source.Supervisor);
        using var detail = await pm.GetAsync($"/api/v1/cases/{incident.Id}");
        var etag = detail.Headers.ETag!.ToString();
        var endpoint = $"/api/v1/cases/{incident.Id}/publications";

        async Task<HttpResponseMessage> Publish(HttpClient client, string? status, string key, Guid recipient)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(new
                {
                    reportIds = new[] { recipient },
                    defectIds = new[] { source.Defect },
                    evidenceIds = Array.Empty<Guid>(),
                    summary = "PM update",
                    repairProgressStatus = status
                })
            };
            request.Headers.Add("Idempotency-Key", key);
            request.Headers.Add("If-Match", etag);
            return await client.SendAsync(request);
        }

        async Task AssertProgress(string? expected)
        {
            using var single = await owner.GetAsync($"/api/v1/reports/{reportId}");
            single.EnsureSuccessStatusCode();
            var report = await single.Content.ReadFromJsonAsync<JsonElement>();
            using var list = await owner.GetAsync("/api/v1/reports");
            list.EnsureSuccessStatusCode();
            var page = await list.Content.ReadFromJsonAsync<JsonElement>();
            var progress = report.GetProperty("repairProgress");
            Assert.Equal(progress.GetRawText(), Assert.Single(page.GetProperty("items").EnumerateArray()).GetProperty("repairProgress").GetRawText());
            if (expected is null) Assert.Equal(JsonValueKind.Null, progress.ValueKind);
            else
            {
                Assert.Equal(expected, progress.GetProperty("status").GetString());
                Assert.Equal(2, progress.EnumerateObject().Count());
                Assert.True(progress.GetProperty("publishedAt").TryGetDateTimeOffset(out _));
            }
        }

        using var old = await Publish(pm, null, Guid.NewGuid().ToString(), reportId);
        Assert.Equal(HttpStatusCode.Created, old.StatusCode);
        etag = old.Headers.ETag!.ToString();
        await AssertProgress(null);
        using var premature = await Publish(pm, "ACCEPTED", Guid.NewGuid().ToString(), reportId);
        Assert.Equal(HttpStatusCode.Conflict, premature.StatusCode);
        Assert.Equal("repair_progress_not_ready", (await premature.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        using var wrongReport = await Publish(pm, "NOT_STARTED", Guid.NewGuid().ToString(), Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Conflict, wrongReport.StatusCode);
        using var denied = await Publish(sup, "NOT_STARTED", Guid.NewGuid().ToString(), reportId);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using var ownerDenied = await Publish(owner, "NOT_STARTED", Guid.NewGuid().ToString(), reportId);
        Assert.Equal(HttpStatusCode.Forbidden, ownerDenied.StatusCode);
        var key = Guid.NewGuid().ToString();
        using var published = await Publish(pm, "NOT_STARTED", key, reportId);
        Assert.Equal(HttpStatusCode.Created, published.StatusCode);
        using var replay = await Publish(pm, "NOT_STARTED", key, reportId);
        Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
        Assert.Equal(await published.Content.ReadAsStringAsync(), await replay.Content.ReadAsStringAsync());
        using var changed = await Publish(pm, "IN_PROGRESS", key, reportId);
        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
        Assert.Equal("idempotency_key_reused", (await changed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await AssertProgress("NOT_STARTED");
        Assert.Equal(2, await db.Set<HuyPublicationRecipient>().CountAsync(row => row.ReportId == reportId));
        var other = await sql.CreateUserAsync("progress-other-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter);
        using var outsider = await Login(other.Id);
        using var hidden = await outsider.GetAsync($"/api/v1/reports/{reportId}");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
    }
}
