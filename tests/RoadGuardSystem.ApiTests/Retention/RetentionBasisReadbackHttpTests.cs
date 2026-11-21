using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using Xunit;

namespace RoadGuardSystem.ApiTests.Retention;

public sealed class RetentionBasisReadbackHttpTests(AuthenticationSqlServerFixture sql) : IClassFixture<AuthenticationSqlServerFixture>
{
    [Fact]
    public async Task WarrantyHttpCompletesInventoryAndWebBasisRequiresFreshFullObligations()
    {
        var sup = await sql.CreateUserAsync("basis-sup-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        var pm = await sql.CreateUserAsync("basis-pm-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var now = DateTimeOffset.UtcNow;
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString("N"), "Isolated retention fixture", null, 32648, null, null, now);
        var file = StoredFile.Create(Guid.NewGuid(), "fixture/retention/" + Guid.NewGuid().ToString("N"), "handover.pdf", "application/pdf", 4, new string('a', 64), sup.Id, now, null);
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, sup.Id, file.StorageUri, "DOCUMENT", file.MimeType, 4, file.Checksum, 8388608, now.AddDays(1));
        upload.StartUploading("SQL fixture", now);
        var handover = HandoverDocument.Create(Guid.NewGuid(), project.Id, "Fixture", new(2026, 8, 31), sup.Id, file.Id, "TEST_ONLY");
        await using var db = sql.CreateDbContext();
        db.AddRange(project, file, upload, handover, FileScope.Create(Guid.NewGuid(), file.Id, project.Id, project.Id, sup.Id, "DOCUMENT", now),
            ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, pm.Id, new(2000, 1, 1)));
        await db.SaveChangesAsync(); upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now); await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var web = await WebCookieTestClient.Login(factory, sup.Email!);
        using var pmWeb = await WebCookieTestClient.Login(factory, pm.Email!);
        var root = $"/api/v1/projects/{project.Id}/retention/files/{file.Id}";
        var initialResponse = await web.GetAsync(root); var initial = await Body(initialResponse);
        Assert.False(initial.GetProperty("inventoryComplete").GetBoolean());
        Assert.Contains(initial.GetProperty("inventoryReasonCodes").EnumerateArray(), x => x.GetString() == "WARRANTY_OBLIGATION_MISSING");
        await Error(await Put(web, initial, initialResponse.Headers.ETag!.Tag, []), 409, "retention_inventory_incomplete");
        var warrantyInput = new { handoverDocumentId = handover.Id, handoverDate = "2026-08-31", warrantyStartDate = "2026-09-01", warrantyEndDate = "2028-08-31", scope = "PROJECT", status = "ACTIVE", operationId = Guid.NewGuid() };
        using var warranty = await web.PostAsJsonAsync($"/api/v1/projects/{project.Id}/warranties", warrantyInput);
        await Body(warranty, 201);
        using var completeResponse = await web.GetAsync(root); var complete = await Body(completeResponse);
        Assert.True(complete.GetProperty("inventoryComplete").GetBoolean(), complete.GetRawText());
        Assert.NotEqual(initial.GetProperty("inventoryVersion").GetString(), complete.GetProperty("inventoryVersion").GetString());
        var ids = complete.GetProperty("applicableObligations").EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ToArray(); Assert.Single(ids);
        await Error(await Put(web, initial, completeResponse.Headers.ETag!.Tag, ids), 409, "retention_inventory_stale");
        await Error(await Put(pmWeb, complete, completeResponse.Headers.ETag.Tag, ids), 403, "access_forbidden");
        await Error(await Put(web, complete, completeResponse.Headers.ETag.Tag, []), 422, "basis_invalid");
        using var saved = await Put(web, complete, completeResponse.Headers.ETag.Tag, ids); await Body(saved);
        await Error(await Put(web, complete, completeResponse.Headers.ETag.Tag, ids), 412, "concurrency_conflict");
        using var fresh = await WebCookieTestClient.Login(factory, sup.Email!);
        using var readback = await fresh.GetAsync(root); var final = await Body(readback);
        Assert.True(final.GetProperty("inventoryComplete").GetBoolean()); Assert.Equal(saved.Headers.ETag, readback.Headers.ETag);
        Assert.Equal(JsonValueKind.String, final.GetProperty("basisHeadId").ValueKind);
        // Ambiguous legacy content cannot prove that this file is unreferenced,
        // even when the legacy dataset belongs to another project.
        var legacyProject = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString("N"), "Legacy other project", null, null, null, null, now);
        var road = RoadSection.Create(Guid.NewGuid(), legacyProject.Id, "LEGACY");
        var route = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true,
            new NetTopologySuite.Geometries.LineString([new(0, 0), new(10, 0)]) { SRID = 32648 }, now, "Fixture");
        var survey = Survey.Create(Guid.NewGuid(), null, legacyProject.Id, route.Id, SurveyType.Periodic, SurveyStatus.InProgress, false, null, null);
        var legacy = SurveyDataVersion.Create(Guid.NewGuid(), survey.Id, 1, SurveyDataVersionStatus.Draft, SurveyDataIntegrityStatus.Pending, null, null, "[{}]");
        db.AddRange(legacyProject, road, route, survey, legacy); await db.SaveChangesAsync();
        var ambiguous = await Body(await fresh.GetAsync(root));
        Assert.False(ambiguous.GetProperty("inventoryComplete").GetBoolean());
        Assert.Contains(ambiguous.GetProperty("inventoryReasonCodes").EnumerateArray(), x => x.GetString() == "LEGACY_DATASET_MANIFEST_UNRESOLVED");
        Assert.Contains(ambiguous.GetProperty("references").EnumerateArray(), x => x.GetProperty("kind").GetString() == "UNRESOLVED_DATASET" && x.GetProperty("id").GetGuid() == legacy.Id);

        async Task<HttpResponseMessage> Put(HttpClient client, JsonElement inventory, string etag, Guid[] warranties)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, root + "/basis")
            { Content = JsonContent.Create(new { warrantyIds = warranties, expectedReferenceInventoryVersion = inventory.GetProperty("inventoryVersion").GetString(), reason = "Confirm all current obligations" }) };
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N")); request.Headers.Add("If-Match", etag);
            return await client.SendAsync(request);
        }
    }
    private static async Task<JsonElement> Body(HttpResponseMessage response, int status = 200)
    {
        var text = await response.Content.ReadAsStringAsync(); Assert.True((int)response.StatusCode == status, $"Expected {status}, got {(int)response.StatusCode}: {text}");
        return JsonDocument.Parse(text).RootElement.Clone();
    }
    private static async Task Error(HttpResponseMessage response, int status, string code) => Assert.Equal(code, (await Body(response, status)).GetProperty("code").GetString());
}
