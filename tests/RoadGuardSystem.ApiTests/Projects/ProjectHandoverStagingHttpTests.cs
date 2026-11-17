using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

[Collection(AuthenticationApiFixture.Name)]
public sealed class ProjectHandoverStagingHttpTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task StagedFileIsConsumedOnceAndOnlyByItsOwner()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var owner = await sql.CreateUserAsync("staging-owner-" + suffix, "Current1!", UserRoleCode.Supervisor);
        var other = await sql.CreateUserAsync("staging-other-" + suffix, "Current1!", UserRoleCode.Supervisor);
        var pm = await sql.CreateUserAsync("staging-pm-" + suffix, "Current1!", UserRoleCode.ProjectManager);
        var staged = await VerifiedProjectHandoverFixture.StageAsync(sql, owner.Id);
        var expired = await VerifiedProjectHandoverFixture.StageAsync(sql, owner.Id, DateTimeOffset.UtcNow.AddDays(-2));
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var ownerWeb = await WebCookieTestClient.Login(factory, owner.Email!);
        using var otherWeb = await WebCookieTestClient.Login(factory, other.Email!);

        await AssertError(await FinalizeProject(otherWeb, staged, "other"), HttpStatusCode.NotFound, "project_handover_file_not_found");
        await AssertError(await FinalizeProject(ownerWeb, expired, "expired"), HttpStatusCode.Conflict, "handover_file_not_ready");

        using var winner = await FinalizeProject(ownerWeb, staged, "winner");
        Assert.Equal(HttpStatusCode.Created, winner.StatusCode);
        var projectId = (await winner.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("projectId").GetGuid();
        using var replay = await FinalizeProject(ownerWeb, staged, "winner");
        Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
        Assert.Equal(projectId, (await replay.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("projectId").GetGuid());
        var retentionPath = $"/api/v1/projects/{projectId}/retention/files/{staged.FileId}";
        using var initial = await ownerWeb.GetAsync(retentionPath);
        Assert.Equal(HttpStatusCode.OK, initial.StatusCode);
        var inventory = await initial.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(inventory.GetProperty("inventoryComplete").GetBoolean(), inventory.GetRawText());
        var warranties = inventory.GetProperty("applicableObligations").EnumerateArray()
            .Select(row => row.GetProperty("id").GetGuid()).ToArray();
        Assert.Single(warranties);
        var basisKey = Guid.NewGuid().ToString("N");
        var basisInput = new
        {
            warrantyIds = warranties,
            expectedReferenceInventoryVersion = inventory.GetProperty("inventoryVersion").GetString(),
            reason = "Confirm verified project handover retention"
        };
        async Task<HttpResponseMessage> PutBasis(string key, string etag)
        {
            using var mutation = new HttpRequestMessage(HttpMethod.Put, retentionPath + "/basis")
            {
                Content = JsonContent.Create(basisInput)
            };
            mutation.Headers.Add("Idempotency-Key", key);
            mutation.Headers.TryAddWithoutValidation("If-Match", etag);
            return await ownerWeb.SendAsync(mutation);
        }
        ownerWeb.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        await AssertError(await PutBasis(basisKey, initial.Headers.ETag!.Tag), HttpStatusCode.Forbidden, "csrf_failed");
        await WebCookieTestClient.RefreshCsrf(ownerWeb);
        using var confirmed = await PutBasis(basisKey, initial.Headers.ETag!.Tag);
        Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
        Assert.NotEqual(initial.Headers.ETag, confirmed.Headers.ETag);
        using var sameBasis = await PutBasis(basisKey, initial.Headers.ETag.Tag);
        Assert.Equal(await confirmed.Content.ReadAsStringAsync(), await sameBasis.Content.ReadAsStringAsync());
        await AssertError(await PutBasis(Guid.NewGuid().ToString("N"), initial.Headers.ETag.Tag),
            HttpStatusCode.PreconditionFailed, "concurrency_conflict");
        using var reload = await WebCookieTestClient.Login(factory, owner.Email!);
        using var readback = await reload.GetAsync(retentionPath);
        Assert.Equal(HttpStatusCode.OK, readback.StatusCode);
        Assert.Equal(confirmed.Headers.ETag, readback.Headers.ETag);
        Assert.NotNull((await readback.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("basisHeadId").GetString());

        await AssertError(await FinalizeProject(ownerWeb, staged, "consumed"), HttpStatusCode.NotFound, "project_handover_file_not_found");
        await using var db = sql.CreateDbContext();
        var scope = await db.FileScopes.AsNoTracking().SingleAsync(row => row.FileId == staged.FileId);
        Assert.Equal(projectId, scope.ProjectId);
        Assert.Equal(projectId, scope.TargetId);
        Assert.Equal("DOCUMENT", scope.Purpose);
        Assert.Equal(projectId, (await db.HandoverDocuments.AsNoTracking().SingleAsync(row => row.FileId == staged.FileId)).ProjectId);
        Assert.Equal(1, await db.AuditLogs.CountAsync(row => row.EntityId == projectId && row.EventType == "project_created"));

        async Task<HttpResponseMessage> FinalizeProject(HttpClient client, (Guid FileId, string ETag) input, string key)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/projects")
            {
                Content = JsonContent.Create(new
                {
                    code = $"P-{suffix}-{key}",
                    name = "Project handover fixture",
                    primaryPmId = pm.Id,
                    handoverDate = "2026-10-01",
                    warrantyEndDate = "2027-10-01",
                    handoverFileIds = new[] { input.FileId }
                })
            };
            request.Headers.Add("Idempotency-Key", suffix + key);
            request.Headers.TryAddWithoutValidation("If-Match", input.ETag);
            return await client.SendAsync(request);
        }
    }

    private static async Task AssertError(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        using (response)
        {
            Assert.Equal(status, response.StatusCode);
            Assert.Equal(code, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        }
    }
}
