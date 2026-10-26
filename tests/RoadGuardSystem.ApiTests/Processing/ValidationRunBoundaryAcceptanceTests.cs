using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NetTopologySuite.Geometries;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Processing;

// An owned database isolates pending-queue assertions from the shared auth host's worker.
public sealed class ValidationRunBoundaryAcceptanceTests(AuthenticationSqlServerFixture sql) : IClassFixture<AuthenticationSqlServerFixture>
{
    [Fact]
    public async Task HttpProducerReplayWorkerAndCurrentReadAuthorityPreserveValidationResult()
    {
        var manager = await sql.CreateUserAsync("validation-pm-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var outsider = await sql.CreateUserAsync("validation-other-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var supervisor = await sql.CreateUserAsync("validation-super-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        var crew = await sql.CreateUserAsync("validation-op-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.DroneOperator);
        var now = DateTimeOffset.UtcNow;
        var project = Project.Create(Guid.NewGuid(), "VAL-" + Guid.NewGuid().ToString("N"), "Isolated validation HTTP", null, 32648, null, null, now);
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, "ROAD-" + Guid.NewGuid().ToString("N"));
        var version = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true,
            new GeometryFactory(new PrecisionModel(), 32648).CreateLineString([new Coordinate(1, 1), new Coordinate(2, 2)]), now, "fixture");
        var survey = Survey.Create(Guid.NewGuid(), null, project.Id, version.Id, SurveyType.Periodic, SurveyStatus.InProgress, false, null, null);
        var data = SurveyDataVersion.Create(Guid.NewGuid(), survey.Id, 1, SurveyDataVersionStatus.ServerConfirmed, SurveyDataIntegrityStatus.Passed, now, SurveyDataConfirmationActor.Backend, "[]");
        var model = AIModelVersion.Create(Guid.NewGuid(), "validation-fixture", Guid.NewGuid().ToString("N"), "fixture://validation", null, null, AIModelVersionStatus.Released, now, supervisor.Id);
        // Synthetic measurement provenance is fixture setup, not an executed field-inspection journey.
        var session = FieldInspectionSession.Create(Guid.NewGuid(), FieldInspectionPurpose.ResearchValidation, null, project.Id, version.Id, null, Guid.NewGuid().ToString("N"), null, "fixture", now, null, "fixture", FieldInspectionSessionStatus.Imported, null);
        var truth = GroundTruthMeasurement.Create(Guid.NewGuid(), session.Id, "sample-1", version.Id, null, null, MeasurementType.DepressionDepth, 10m, "mm",
            new GeometryFactory(new PrecisionModel(), 4326).CreatePoint(new Coordinate(106, 10)), "gauge", null, "fixture", "fixture", now, null, "fixture");
        var derived = DerivedMeasurement.Create(Guid.NewGuid(), data.Id, version.Id, "sample-1", MeasurementType.DepressionDepth, 11m, "mm", null, DerivedMeasurementSourceType.ManualDerived, "fixture", now, DerivedMeasurementStatus.Published);
        var member = new ProjectMember { Id = Guid.NewGuid(), ProjectId = project.Id, UserId = manager.Id, RoleCode = UserRoleCode.ProjectManager, Status = ProjectMemberStatus.Active, ValidFrom = DateOnly.FromDateTime(now.UtcDateTime.AddDays(-1)) };
        await using (var db = sql.CreateDbContext())
        {
            db.AddRange(project, road, version, survey, data, model, session, truth, derived, member);
            await db.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services => services.RemoveAll<IHostedService>());
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var path = $"/api/v1/projects/{project.Id}/validation-runs";
        var split = Guid.NewGuid();
        object Input(Guid datasetSplit, Guid? modelOverride = null) => new { pairs = new[] { new { groundTruthId = truth.Id, derivedMeasurementId = derived.Id } }, modelVersionId = modelOverride ?? model.Id, datasetSplitId = datasetSplit, measurementType = "DEPRESSION_DEPTH", unit = "mm" };
        var key = Guid.NewGuid().ToString("N");
        await Login(client, manager.UserName!);
        var created = await Command(client, path, Input(split), key);
        Assert.Equal(HttpStatusCode.Accepted, created.StatusCode);
        Assert.Equal("application/json", created.Content.Headers.ContentType!.MediaType);
        var receipt = await Body(created);
        AssertSet(receipt, "id", "status", "resultId", "attemptNumber", "version", "jobType", "error");
        var runId = receipt.GetProperty("id").GetGuid();
        Assert.Equal($"/api/v1/validation-runs/{runId}", created.Headers.Location!.OriginalString);
        Assert.Equal("\"" + receipt.GetProperty("version").GetString() + "\"", created.Headers.ETag!.Tag);
        Assert.Equal("QUEUED", receipt.GetProperty("status").GetString());
        Assert.Equal("VALIDATION", receipt.GetProperty("jobType").GetString());
        Assert.Equal(0, receipt.GetProperty("attemptNumber").GetInt32());
        Assert.Equal(JsonValueKind.Null, receipt.GetProperty("resultId").ValueKind);
        Assert.Equal(JsonValueKind.Null, receipt.GetProperty("error").ValueKind);
        Correlation(created);
        await using (var db = sql.CreateDbContext())
            Assert.Equal(Convert.ToBase64String((await db.ValidationRuns.SingleAsync(x => x.Id == runId)).RowVersion), receipt.GetProperty("version").GetString());
        var replay = await Command(client, path, Input(split), key);
        Assert.Equal(HttpStatusCode.Accepted, replay.StatusCode);
        Assert.Equal(receipt.GetRawText(), (await Body(replay)).GetRawText());
        Assert.Equal(created.Headers.ETag, replay.Headers.ETag);
        await Problem(await Command(client, path, Input(Guid.NewGuid()), key), HttpStatusCode.Conflict, "duplicate_request");
        var readPath = created.Headers.Location.OriginalString;
        var queued = await client.GetAsync(readPath);
        Assert.Equal(HttpStatusCode.OK, queued.StatusCode);
        var queuedBody = await Body(queued);
        AssertSet(queuedBody, "id", "usedCount", "excludedCount", "bias", "mae", "rmse", "unit", "exclusionReasons");
        Assert.Equal(runId, queuedBody.GetProperty("id").GetGuid());
        Assert.Equal(JsonValueKind.Null, queuedBody.GetProperty("bias").ValueKind);
        Assert.Equal(0, queuedBody.GetProperty("usedCount").GetInt32());
        // The production worker consumes the global pending queue, including
        // other tests' committed items in this fixture-owned database.
        int pending;
        await using (var db = sql.CreateDbContext())
            pending = await db.OutboxMessages.CountAsync(x => x.MessageType == "validation_run.dispatch" && x.DeliveryStatus == OutboxDeliveryStatus.Pending);
        var ownCompleted = false;
        for (var i = 0; i < pending && !ownCompleted; i++)
        {
            using var scope = factory.Services.CreateScope();
            Assert.True(await scope.ServiceProvider.GetRequiredService<IProcessingV2Repository>().CompleteNextValidationRunAsync());
            await using var db = sql.CreateDbContext();
            ownCompleted = await db.ValidationRuns.AnyAsync(x => x.Id == runId && x.Status == ValidationRunStatus.Completed);
        }
        Assert.True(ownCompleted);
        var completed = await client.GetAsync(readPath);
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        Assert.Equal("application/json", completed.Content.Headers.ContentType!.MediaType);
        Correlation(completed);
        var result = await Body(completed);
        AssertSet(result, "id", "usedCount", "excludedCount", "bias", "mae", "rmse", "unit", "exclusionReasons");
        Assert.Equal(runId, result.GetProperty("id").GetGuid());
        Assert.Equal(1, result.GetProperty("usedCount").GetInt32());
        Assert.Equal(0, result.GetProperty("excludedCount").GetInt32());
        foreach (var metric in new[] { "bias", "mae", "rmse" }) Assert.Equal(1m, result.GetProperty(metric).GetDecimal());
        Assert.Equal("mm", result.GetProperty("unit").GetString());
        Assert.Equal(0, result.GetProperty("exclusionReasons").GetArrayLength());
        await using (var db = sql.CreateDbContext())
        {
            Assert.Equal(ValidationRunStatus.Completed, (await db.ValidationRuns.SingleAsync(x => x.Id == runId)).Status);
            Assert.Equal(1, await db.ValidationRuns.CountAsync(x => x.ProjectId == project.Id));
            Assert.Equal(1, await db.MeasurementValidationSamples.CountAsync(x => x.ValidationRunId == runId));
            Assert.Equal(1, await db.AuditLogs.CountAsync(x => x.EventType == "validation_run_created" && x.EntityId == runId));
        }
        await Login(client, outsider.UserName!);
        await Problem(await client.GetAsync(readPath), HttpStatusCode.NotFound, "not_found");
        await Problem(await Command(client, path, Input(split), Guid.NewGuid().ToString()), HttpStatusCode.Forbidden, "access_forbidden");
        await Login(client, crew.UserName!);
        await Problem(await client.GetAsync(readPath), HttpStatusCode.NotFound, "not_found");
        await Problem(await Command(client, path, Input(split), Guid.NewGuid().ToString()), HttpStatusCode.Forbidden, "access_forbidden");
        await Login(client, supervisor.UserName!);
        var globalRead = await client.GetAsync(readPath);
        Assert.Equal(HttpStatusCode.OK, globalRead.StatusCode);
        Assert.Equal(result.GetRawText(), (await Body(globalRead)).GetRawText());
        await Problem(await Command(client, path, Input(split), Guid.NewGuid().ToString()), HttpStatusCode.Forbidden, "access_forbidden");
        await Login(client, manager.UserName!);
        var unavailableModel = Guid.NewGuid();
        var failedKey = Guid.NewGuid().ToString();
        await Problem(await Command(client, path, Input(split, unavailableModel), failedKey), HttpStatusCode.Conflict, "validation_error");
        Guid failedReceiptId;
        await using (var db = sql.CreateDbContext())
            failedReceiptId = (await db.IdempotencyRecords.SingleAsync(x => x.ProjectId == project.Id && x.IdempotencyKey == failedKey)).Id;
        await Problem(await Command(client, path, Input(split, unavailableModel), failedKey), HttpStatusCode.Conflict, "validation_error");
        await using (var db = sql.CreateDbContext())
            Assert.Equal(failedReceiptId, (await db.IdempotencyRecords.SingleAsync(x => x.ProjectId == project.Id && x.IdempotencyKey == failedKey)).Id);
        await using (var db = sql.CreateDbContext())
        {
            db.ProjectMembers.Remove(await db.ProjectMembers.SingleAsync(x => x.Id == member.Id));
            await db.SaveChangesAsync();
        }
        await Problem(await client.GetAsync(readPath), HttpStatusCode.NotFound, "not_found");
        await Problem(await Command(client, path, Input(split), key), HttpStatusCode.Forbidden, "access_forbidden");
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(readPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Command(client, path, Input(split), key)).StatusCode);
        await using (var db = sql.CreateDbContext())
        {
            var persisted = await db.ValidationRuns.SingleAsync(x => x.Id == runId);
            Assert.Equal(ValidationRunStatus.Completed, persisted.Status);
            Assert.Equal(1, await db.ValidationRuns.CountAsync(x => x.ProjectId == project.Id));
            Assert.Equal(1, await db.MeasurementValidationSamples.CountAsync(x => x.ValidationRunId == runId));
            Assert.Equal(2, await db.IdempotencyRecords.CountAsync(x => x.ProjectId == project.Id && x.Operation == "ValidationRunCreated"));
            Assert.Equal(1, await db.AuditLogs.CountAsync(x => x.EventType == "validation_run_created" && x.EntityId == runId));
            Assert.Equal(1, await db.OutboxMessages.CountAsync(x => x.MessageType == "validation_run.dispatch" && x.CorrelationId == runId));
        }
    }

    [Theory]
    [InlineData("root")]
    [InlineData("pair")]
    [InlineData("header")]
    public async Task FrozenValidationSchemaRejectsUnknownMembersAndOversizedKey(string boundary)
    {
        var actor = await sql.CreateUserAsync("validation-schema-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var project = Project.Create(Guid.NewGuid(), "VSC-" + Guid.NewGuid().ToString("N"), "Schema fixture", null, 32648, null, null, DateTimeOffset.UtcNow);
        await using (var db = sql.CreateDbContext())
        {
            db.Add(project);
            db.Add(new ProjectMember { Id = Guid.NewGuid(), ProjectId = project.Id, UserId = actor.Id, RoleCode = UserRoleCode.ProjectManager, Status = ProjectMemberStatus.Active, ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)) });
            await db.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services => services.RemoveAll<IHostedService>());
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await Login(client, actor.UserName!);
        var pair = new Dictionary<string, object> { ["groundTruthId"] = Guid.NewGuid(), ["derivedMeasurementId"] = Guid.NewGuid() };
        var input = new Dictionary<string, object> { ["pairs"] = new[] { pair }, ["modelVersionId"] = Guid.NewGuid(), ["datasetSplitId"] = Guid.NewGuid(), ["measurementType"] = "DEPRESSION_DEPTH", ["unit"] = "mm" };
        if (boundary == "root") input["undeclared"] = "must-reject";
        if (boundary == "pair") pair["undeclared"] = "must-reject";
        var key = boundary == "header" ? new string('k', 201) : Guid.NewGuid().ToString();
        await Problem(await Command(client, $"/api/v1/projects/{project.Id}/validation-runs", input, key), HttpStatusCode.BadRequest, "validation_error");
        await using var verify = sql.CreateDbContext();
        Assert.Equal(0, await verify.ValidationRuns.CountAsync(x => x.ProjectId == project.Id));
        Assert.Equal(0, await verify.IdempotencyRecords.CountAsync(x => x.ProjectId == project.Id));
        Assert.Equal(0, await verify.AuditLogs.CountAsync(x => x.ActorUserId == actor.Id));
    }

    private static async Task Login(HttpClient client, string username)
    {
        client.DefaultRequestHeaders.Authorization = null;
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(username), password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Body(response)).GetProperty("accessToken").GetString());
    }
    private static async Task<HttpResponseMessage> Command(HttpClient client, string path, object body, string key)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", key);
        return await client.SendAsync(request);
    }
    private static async Task<JsonElement> Body(HttpResponseMessage response) => (await response.Content.ReadFromJsonAsync<JsonElement>()).Clone();
    private static void AssertSet(JsonElement body, params string[] expected) => Assert.Equal(expected.Order(), body.EnumerateObject().Select(x => x.Name).Order());
    private static void Correlation(HttpResponseMessage response) => Assert.True(Guid.TryParse(Assert.Single(response.Headers.GetValues("X-Correlation-ID")), out _));
    private static async Task Problem(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var body = await Body(response);
        Assert.Equal((int)status, body.GetProperty("status").GetInt32());
        Assert.Equal(code, body.GetProperty("code").GetString());
        Assert.True(Guid.TryParse(body.GetProperty("correlationId").GetString(), out _));
        Assert.Null(response.Headers.ETag);
        Assert.Null(response.Headers.Location);
    }
}
