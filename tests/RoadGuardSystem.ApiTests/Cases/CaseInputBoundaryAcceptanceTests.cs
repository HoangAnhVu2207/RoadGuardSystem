using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Cases;

[Collection(AuthenticationApiFixture.Name)]
public sealed class CaseInputBoundaryAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData("triage", false)]
    [InlineData("triage", true)]
    [InlineData("link", false)]
    [InlineData("link", true)]
    [InlineData("split", false)]
    [InlineData("split", true)]
    [InlineData("conclude", false)]
    [InlineData("conclude", true)]
    [InlineData("publish", false)]
    [InlineData("publish", true)]
    public async Task ExactCaseMutationRejectsMalformedHeadersAndCommandFieldsBeforeEffects(string action, bool web)
    {
        var actor = await sql.CreateUserAsync("CASE-INPUT-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = web ? await WebCookieTestClient.Login(factory, actor.Email!) : factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        if (!web)
        {
            using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        }
        var version = Convert.ToBase64String(new byte[8]); var caseId = Guid.NewGuid();
        var route = action switch { "link" => "report-links", "split" => "report-splits", "conclude" => "conclusions", "publish" => "publications", _ => "triage" };
        var reason = action == "publish" ? "summary" : "reason";
        JsonObject Valid() => action switch
        {
            "triage" => new() { ["projectId"] = Guid.NewGuid(), ["verificationMethod"] = "EXISTING_EVIDENCE", [reason] = "Valid reason" },
            "link" => new() { ["reportIds"] = new JsonArray(JsonValue.Create(Guid.NewGuid())), ["sourceCaseVersions"] = new JsonObject { [Guid.NewGuid().ToString()] = version }, [reason] = "Valid reason" },
            "split" => new() { ["reportIds"] = new JsonArray(JsonValue.Create(Guid.NewGuid())), [reason] = "Valid reason" },
            "conclude" => new() { ["outcome"] = "NO_DEFECT", ["defectIds"] = new JsonArray(), ["evidenceIds"] = new JsonArray(JsonValue.Create(Guid.NewGuid())), [reason] = "Valid reason" },
            _ => new() { ["reportIds"] = new JsonArray(JsonValue.Create(Guid.NewGuid())), ["defectIds"] = new JsonArray(), ["evidenceIds"] = new JsonArray(JsonValue.Create(Guid.NewGuid())), [reason] = "Valid reason" }
        };
        async Task<string> Snapshot()
        {
            await using var db = sql.CreateDbContext();
            return JsonSerializer.Serialize(new { cases = await db.IncidentCases.CountAsync(), receipts = await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == actor.Id), audits = await db.AuditLogs.CountAsync(a => a.ActorUserId == actor.Id && a.Source == "huy01.case") });
        }
        var before = await Snapshot();
        async Task Reject(JsonObject body, string? match = null, string? key = null, int status = 400, bool omitMatch = false, bool omitKey = false, string? rawBody = null, Guid? pathId = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/cases/{pathId ?? caseId}/{route}") { Content = rawBody is null ? JsonContent.Create(body) : new StringContent(rawBody, System.Text.Encoding.UTF8, "application/json") };
            if (!omitMatch) request.Headers.TryAddWithoutValidation("If-Match", match ?? '"' + version + '"');
            if (!omitKey) request.Headers.TryAddWithoutValidation("Idempotency-Key", key ?? Guid.NewGuid().ToString());
            using var response = await client.SendAsync(request);
            Assert.Equal((HttpStatusCode)status, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var error = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(status, error.GetProperty("status").GetInt32());
            Assert.Equal(status == 428 ? "precondition_required" : "validation_error", error.GetProperty("code").GetString());
            Assert.True(Guid.TryParse(Assert.Single(response.Headers.GetValues("X-Correlation-ID")), out _));
            Assert.Null(response.Headers.ETag); Assert.Null(response.Headers.Location);
            Assert.DoesNotContain(actor.Email!, error.GetRawText());
            Assert.Equal(before, await Snapshot());
        }
        foreach (var raw in new[] { "null", "{", "[]", "{\"reason\":123}", "{\"summary\":123}" })
        {
            // Only the selected DTO text property is constrained by its string type.
            if (raw.Contains("summary") && action != "publish" || raw.Contains("reason") && action == "publish") continue;
            await Reject(Valid(), rawBody: raw);
        }
        await Reject(Valid(), status: 428, omitMatch: true);
        await Reject(Valid(), status: 428, omitKey: true);
        foreach (var match in new[] { version, "W/\"" + version + "\"", "\"not-base64\"", "\"AA==\"" }) await Reject(Valid(), match: match);
        // Empty header is absent; a present whitespace value reaches key validation.
        await Reject(Valid(), key: "", status: 428);
        await Reject(Valid(), key: "   ");
        await Reject(Valid(), key: new string('k', 201));
        await Reject(Valid(), key: "bad\tkey");
        await Reject(Valid(), key: "bad\u00e9key");
        foreach (var value in new string?[] { null, "", "  ", new string('r', 1001) }) { var b = Valid(); b[reason] = value; await Reject(b); }
        var missing = Valid(); missing.Remove(reason); await Reject(missing);
        await Reject(Valid(), pathId: Guid.Empty);
        // DTOs explicitly disallow unmapped members; clients cannot inject internal state.
        foreach (var field in new[] { "unknownField", "actorUserId", "status", "version" })
        {
            var extra = Valid(); extra[field] = "untrusted"; await Reject(extra);
        }
        if (action == "triage")
        {
            foreach (var value in new JsonNode?[] { null, JsonValue.Create(Guid.Empty), JsonValue.Create("not-a-uuid") }) { var b = Valid(); b["projectId"] = value?.DeepClone(); await Reject(b); }
            foreach (var value in new string?[] { null, "INVALID" }) { var b = Valid(); b["verificationMethod"] = value; await Reject(b); }
            var partial = Valid(); partial["routeVersionId"] = Guid.NewGuid(); await Reject(partial);
            var emptyGeometry = Valid(); emptyGeometry["routeVersionId"] = Guid.Empty; emptyGeometry["segmentSetId"] = Guid.Empty; emptyGeometry["geometryVersion"] = version; await Reject(emptyGeometry);
            var geometry = Valid(); geometry["routeVersionId"] = Guid.NewGuid(); geometry["segmentSetId"] = Guid.NewGuid(); await Reject(geometry);
            var missingProject = Valid(); missingProject.Remove("projectId"); await Reject(missingProject);
            var missingMethod = Valid(); missingMethod.Remove("verificationMethod"); await Reject(missingMethod);
        }
        if (action is "link" or "split" or "publish")
        {
            var duplicate = Guid.NewGuid();
            foreach (var value in new JsonArray?[] { null, new(), new(JsonValue.Create(Guid.Empty)), new(JsonValue.Create("not-a-uuid")), new(JsonValue.Create(duplicate), JsonValue.Create(duplicate)) }) { var b = Valid(); b["reportIds"] = value?.DeepClone(); await Reject(b); }
        }
        if (action == "link")
        {
            var nullVersion = Valid(); nullVersion["sourceCaseVersions"] = new JsonObject { [Guid.NewGuid().ToString()] = null }; await Reject(nullVersion);
            var malformedSource = Valid(); malformedSource["sourceCaseVersions"] = new JsonObject { ["not-a-uuid"] = version }; await Reject(malformedSource);
            foreach (var value in new JsonObject?[] { null, new(), new() { [Guid.Empty.ToString()] = version }, new() { [caseId.ToString()] = version }, new() { [Guid.NewGuid().ToString()] = "invalid" }, new() { [Guid.NewGuid().ToString()] = "AA==" } }) { var b = Valid(); b["sourceCaseVersions"] = value?.DeepClone(); await Reject(b); }
        }
        if (action is "conclude" or "publish")
        {
            foreach (var field in new[] { "defectIds", "evidenceIds" })
            {
                var duplicate = Guid.NewGuid();
                foreach (var value in new JsonArray?[] { null, new(JsonValue.Create(Guid.Empty)), new(JsonValue.Create("not-a-uuid")), new(JsonValue.Create(duplicate), JsonValue.Create(duplicate)) }) { var b = Valid(); b[field] = value?.DeepClone(); await Reject(b); }
            }
        }
        if (action == "conclude")
        {
            foreach (var value in new string?[] { null, "INVALID" }) { var b = Valid(); b["outcome"] = value; await Reject(b); }
            var confirmed = Valid(); confirmed["outcome"] = "CONFIRMED"; await Reject(confirmed);
            var confirmedWithoutEvidence = Valid(); confirmedWithoutEvidence["outcome"] = "CONFIRMED"; confirmedWithoutEvidence["defectIds"] = new JsonArray(JsonValue.Create(Guid.NewGuid())); confirmedWithoutEvidence["evidenceIds"] = new JsonArray(); await Reject(confirmedWithoutEvidence);
            var contradictory = Valid(); contradictory["defectIds"] = new JsonArray(JsonValue.Create(Guid.NewGuid())); await Reject(contradictory);
            var noEvidence = Valid(); noEvidence["evidenceIds"] = new JsonArray(); await Reject(noEvidence);
        }
    }
}
