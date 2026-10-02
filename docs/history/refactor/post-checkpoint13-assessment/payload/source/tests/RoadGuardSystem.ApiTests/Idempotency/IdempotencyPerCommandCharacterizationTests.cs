using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Idempotency;

/// <summary>
/// RF-10-08-C01: Per-command idempotency characterization tests.
/// NOT_RUN: All tests in this class are NOT executed in BOX 1.
/// BOX 3 will run these tests after both BOX 1 and BOX 2 stop modifying test source.
/// </summary>
[Trait("TaskId", "RF-10-08-C01")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class IdempotencyPerCommandCharacterizationTests
{
    private readonly AuthenticationSqlServerFixture _sql;

    public IdempotencyPerCommandCharacterizationTests(AuthenticationSqlServerFixture sql)
    {
        _sql = sql;
    }

    /// <summary>
    /// Gap 1: Same key + same fingerprint replay for upload creation.
    /// NOT_RUN: Evidence pending BOX 3 verification.
    /// </summary>
    [Fact]
    public async Task UploadCreate_SameKeySamePayload_ReplaysWithIdenticalOutcome()
    {
        var supervisor = await _sql.CreateUserAsync($"upload-replay-sup-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var manager = await _sql.CreateUserAsync($"upload-replay-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var content = Encoding.UTF8.GetBytes("upload replay fixture");
        var checksum = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        var storage = new CharacterizationUploadStorage(content, checksum);
        await using var factory = new AuthenticationWebApplicationFactory(
            _sql.ConnectionString,
            configureTestServices: services =>
            {
                services.RemoveAll<IUploadObjectStorage>();
                services.AddSingleton<IUploadObjectStorage>(storage);
            });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        await AuthenticateAsync(client, supervisor.UserName!);
        var projectId = await CreateProjectAsync(client, manager.Id);
        await AuthenticateAsync(client, manager.UserName!);

        var payload = new
        {
            purpose = "DOCUMENT",
            projectId,
            targetId = (Guid?)null,
            fileName = "replay-test.pdf",
            mediaType = "application/pdf",
            sizeBytes = content.Length,
            checksumSha256 = checksum
        };
        const string idempotencyKey = "upload-replay-key-001";

        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/uploads")
        {
            Content = JsonContent.Create(payload)
        };
        firstRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        var first = await client.SendAsync(firstRequest);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstBodyText = await first.Content.ReadAsStringAsync();
        var firstBody = JsonDocument.Parse(firstBodyText).RootElement;
        var uploadId = firstBody.GetProperty("id").GetGuid();
        var fileId = firstBody.GetProperty("fileId").GetGuid();
        var status = firstBody.GetProperty("status").GetString();
        status.Should().Be("PENDING");

        Guid firstOperationId;
        string firstOutcomeFingerprint;
        await using (var snapshot1 = _sql.CreateDbContext())
        {
            var sessionCount = await snapshot1.UploadSessions.AsNoTracking().CountAsync(s => s.Id == uploadId);
            sessionCount.Should().Be(1);
            var session = await snapshot1.UploadSessions.AsNoTracking().SingleAsync(s => s.Id == uploadId);
            session.FileId.Should().Be(fileId);
            session.Status.Should().Be(UploadSessionStatus.Pending);

            var scopeCount = await snapshot1.FileScopes.AsNoTracking().CountAsync(sc => sc.FileId == fileId);
            scopeCount.Should().Be(1);
            var scope = await snapshot1.FileScopes.AsNoTracking().SingleAsync(sc => sc.FileId == fileId);
            scope.ProjectId.Should().Be(projectId);
            scope.OwnerUserId.Should().Be(manager.Id);

            var receipt = await snapshot1.IdempotencyRecords.AsNoTracking().SingleAsync(
                r => r.ActorUserId == manager.Id &&
                     r.ProjectId == projectId &&
                     r.Operation == "UploadSessionCreated" &&
                     r.IdempotencyKey == idempotencyKey);
            firstOperationId = receipt.OperationId;
            firstOutcomeFingerprint = receipt.OutcomeJson;
            receipt.RequestFingerprint.Should().NotBeNullOrWhiteSpace();
            receipt.OutcomeJson.Should().Contain(uploadId.ToString());
            receipt.OutcomeJson.Should().Contain(fileId.ToString());

            var auditCount = await snapshot1.AuditLogs.AsNoTracking().CountAsync(
                a => a.EntityId == uploadId && a.EventType == "upload_session_created");
            auditCount.Should().Be(1);
        }

        using var replayRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/uploads")
        {
            Content = JsonContent.Create(payload)
        };
        replayRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        var replay = await client.SendAsync(replayRequest);

        replay.StatusCode.Should().Be(HttpStatusCode.Created);
        var replayBodyText = await replay.Content.ReadAsStringAsync();
        replayBodyText.Should().Be(firstBodyText, "replay must return identical response body");

        await using (var snapshot2 = _sql.CreateDbContext())
        {
            var sessionCount = await snapshot2.UploadSessions.AsNoTracking().CountAsync(
                s => s.OwnerUserId == manager.Id && s.Purpose == "DOCUMENT");
            sessionCount.Should().Be(1, "replay must not create duplicate session - scoped count check");

            var session = await snapshot2.UploadSessions.AsNoTracking().SingleAsync(s => s.Id == uploadId);
            session.FileId.Should().Be(fileId, "replay returns stored uploadId and fileId");

            var scopeCount = await snapshot2.FileScopes.AsNoTracking().CountAsync(
                sc => sc.ProjectId == projectId && sc.OwnerUserId == manager.Id);
            scopeCount.Should().Be(1, "replay must not create duplicate scope - scoped count check");

            var receipt = await snapshot2.IdempotencyRecords.AsNoTracking().SingleAsync(
                r => r.ActorUserId == manager.Id &&
                     r.ProjectId == projectId &&
                     r.Operation == "UploadSessionCreated" &&
                     r.IdempotencyKey == idempotencyKey);
            receipt.OperationId.Should().Be(firstOperationId, "replay returns stored operation ID");
            receipt.OutcomeJson.Should().Be(firstOutcomeFingerprint, "replay returns stored outcome without re-executing handler");

            var idempotencyCount = await snapshot2.IdempotencyRecords.AsNoTracking().CountAsync(
                r => r.ActorUserId == manager.Id &&
                     r.ProjectId == projectId &&
                     r.Operation == "UploadSessionCreated" &&
                     r.IdempotencyKey == idempotencyKey);
            idempotencyCount.Should().Be(1, "replay must not create duplicate idempotency record");

            var auditCount = await snapshot2.AuditLogs.AsNoTracking().CountAsync(
                a => a.EntityId == uploadId && a.EventType == "upload_session_created");
            auditCount.Should().Be(1, "replay must not create duplicate audit log");
        }
    }

    /// <summary>
    /// Gap 1: Same key + same fingerprint replay for survey plan creation.
    /// NOT_RUN: Evidence pending BOX 3 verification.
    /// </summary>
    [Fact]
    public async Task SurveyPlanCreate_SameKeySamePayload_ReplaysWithIdenticalOutcome()
    {
        var supervisor = await _sql.CreateUserAsync($"plan-replay-sup-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var manager = await _sql.CreateUserAsync($"plan-replay-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        await AuthenticateAsync(client, supervisor.UserName!);
        var projectId = await CreateProjectAsync(client, manager.Id);
        var routeVersionId = await CreateRoadSectionAsync(client, projectId);
        await AuthenticateAsync(client, manager.UserName!);

        var segmentSetId = Guid.NewGuid();
        var segmentId = Guid.NewGuid();
        await using (var context = _sql.CreateDbContext())
        {
            context.RoadSegmentSets.Add(RoadSegmentSet.Create(segmentSetId, routeVersionId));
            context.RoadSegments.Add(RoadSegment.Create(segmentId, segmentSetId, routeVersionId, 1));
            await context.SaveChangesAsync();
        }

        var payload = new
        {
            scope = new[] { new { routeVersionId, segmentSetId, segmentIds = new[] { segmentId }, targetBand = "SURFACE" } },
            plannedAt = "2026-10-01T08:00:00Z",
            surveyType = "BASELINE"
        };
        const string idempotencyKey = "plan-replay-key-001";

        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{projectId}/survey-plans")
        {
            Content = JsonContent.Create(payload)
        };
        firstRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        var first = await client.SendAsync(firstRequest);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstBodyText = await first.Content.ReadAsStringAsync();
        var firstBody = JsonDocument.Parse(firstBodyText).RootElement;
        var planId = firstBody.GetProperty("id").GetGuid();

        Guid firstOperationId;
        string firstOutcomeFingerprint;
        await using (var snapshot1 = _sql.CreateDbContext())
        {
            var planCount = await snapshot1.SurveyPlans.AsNoTracking().CountAsync(p => p.Id == planId);
            planCount.Should().Be(1);
            var plan = await snapshot1.SurveyPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
            plan.ProjectId.Should().Be(projectId);
            plan.Status.Should().Be(SurveyPlanStatus.Planned);

            var scopeCount = await snapshot1.SurveyPlanScopes.AsNoTracking().CountAsync(sc => sc.SurveyPlanId == planId);
            scopeCount.Should().Be(1);

            var receipt = await snapshot1.IdempotencyRecords.AsNoTracking().SingleAsync(
                r => r.ActorUserId == manager.Id &&
                     r.ProjectId == projectId &&
                     r.Operation == "SurveyPlanV2Created" &&
                     r.IdempotencyKey == idempotencyKey);
            firstOperationId = receipt.OperationId;
            firstOutcomeFingerprint = receipt.OutcomeJson;
            receipt.RequestFingerprint.Should().NotBeNullOrWhiteSpace();
        }

        using var replayRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{projectId}/survey-plans")
        {
            Content = JsonContent.Create(payload)
        };
        replayRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        var replay = await client.SendAsync(replayRequest);

        replay.StatusCode.Should().Be(HttpStatusCode.Created);
        var replayBodyText = await replay.Content.ReadAsStringAsync();
        replayBodyText.Should().Be(firstBodyText, "replay must return identical response body");

        await using (var snapshot2 = _sql.CreateDbContext())
        {
            var planCount = await snapshot2.SurveyPlans.AsNoTracking().CountAsync(
                p => p.ProjectId == projectId && p.Status == SurveyPlanStatus.Planned);
            planCount.Should().Be(1, "replay must not create duplicate plan - scoped count check");

            var plan = await snapshot2.SurveyPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
            plan.ProjectId.Should().Be(projectId, "replay returns stored planId");

            var scopeCount = await snapshot2.SurveyPlanScopes.AsNoTracking().CountAsync(
                sc => sc.SurveyPlanId == planId);
            scopeCount.Should().Be(1, "replay must not create duplicate scope - scoped count check");

            var receipt = await snapshot2.IdempotencyRecords.AsNoTracking().SingleAsync(
                r => r.ActorUserId == manager.Id &&
                     r.ProjectId == projectId &&
                     r.Operation == "SurveyPlanV2Created" &&
                     r.IdempotencyKey == idempotencyKey);
            receipt.OperationId.Should().Be(firstOperationId, "replay returns stored operation ID");
            receipt.OutcomeJson.Should().Be(firstOutcomeFingerprint, "replay returns stored outcome without re-executing handler");

            var idempotencyCount = await snapshot2.IdempotencyRecords.AsNoTracking().CountAsync(
                r => r.ActorUserId == manager.Id &&
                     r.ProjectId == projectId &&
                     r.Operation == "SurveyPlanV2Created" &&
                     r.IdempotencyKey == idempotencyKey);
            idempotencyCount.Should().Be(1, "replay must not create duplicate idempotency record");
        }
    }

    /// <summary>
    /// Gap 2: Same key + different fingerprint conflict for upload creation.
    /// NOT_RUN: Evidence pending BOX 3 verification.
    /// </summary>
    [Fact]
    public async Task UploadCreate_SameKeyDifferentPayload_ReturnsConflict()
    {
        var supervisor = await _sql.CreateUserAsync($"upload-conflict-sup-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var manager = await _sql.CreateUserAsync($"upload-conflict-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var content = Encoding.UTF8.GetBytes("upload conflict fixture");
        var checksum = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        var storage = new CharacterizationUploadStorage(content, checksum);
        await using var factory = new AuthenticationWebApplicationFactory(
            _sql.ConnectionString,
            configureTestServices: services =>
            {
                services.RemoveAll<IUploadObjectStorage>();
                services.AddSingleton<IUploadObjectStorage>(storage);
            });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        await AuthenticateAsync(client, supervisor.UserName!);
        var projectId = await CreateProjectAsync(client, manager.Id);
        await AuthenticateAsync(client, manager.UserName!);

        int beforeSessions;
        await using (var before = _sql.CreateDbContext())
        {
            beforeSessions = await before.UploadSessions.AsNoTracking().CountAsync();
        }

        var payloadA = new
        {
            purpose = "DOCUMENT",
            projectId,
            targetId = (Guid?)null,
            fileName = "first.pdf",
            mediaType = "application/pdf",
            sizeBytes = content.Length,
            checksumSha256 = checksum
        };
        const string idempotencyKey = "upload-conflict-key-001";

        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/uploads")
        {
            Content = JsonContent.Create(payloadA)
        };
        firstRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        var first = await client.SendAsync(firstRequest);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var uploadId = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var payloadB = new
        {
            purpose = "DOCUMENT",
            projectId,
            targetId = (Guid?)null,
            fileName = "second.pdf",
            mediaType = "application/pdf",
            sizeBytes = content.Length,
            checksumSha256 = checksum
        };

        using var conflictRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/uploads")
        {
            Content = JsonContent.Create(payloadB)
        };
        conflictRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        var conflict = await client.SendAsync(conflictRequest);

        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problemCode = (await conflict.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();
        problemCode.Should().Be("duplicate_request");

        await using var verification = _sql.CreateDbContext();
        var sessionCount = await verification.UploadSessions.AsNoTracking().CountAsync();
        sessionCount.Should().Be(beforeSessions + 1, "conflict must not create second session");

        var idempotencyCount = await verification.IdempotencyRecords.AsNoTracking().CountAsync(
            r => r.ActorUserId == manager.Id &&
                 r.ProjectId == projectId &&
                 r.Operation == "UploadSessionCreated" &&
                 r.IdempotencyKey == idempotencyKey);
        idempotencyCount.Should().Be(1, "conflict must not create second idempotency record");
    }

    /// <summary>
    /// Gap 2: Same key + different fingerprint conflict for survey plan creation.
    /// NOT_RUN: Evidence pending BOX 3 verification.
    /// </summary>
    [Fact]
    public async Task SurveyPlanCreate_SameKeyDifferentPayload_ReturnsConflict()
    {
        var supervisor = await _sql.CreateUserAsync($"plan-conflict-sup-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var manager = await _sql.CreateUserAsync($"plan-conflict-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        await AuthenticateAsync(client, supervisor.UserName!);
        var projectId = await CreateProjectAsync(client, manager.Id);
        var routeVersionId = await CreateRoadSectionAsync(client, projectId);
        await AuthenticateAsync(client, manager.UserName!);

        var segmentSetId1 = Guid.NewGuid();
        var segmentId1 = Guid.NewGuid();
        var segmentSetId2 = Guid.NewGuid();
        var segmentId2 = Guid.NewGuid();
        await using (var context = _sql.CreateDbContext())
        {
            context.RoadSegmentSets.Add(RoadSegmentSet.Create(segmentSetId1, routeVersionId));
            context.RoadSegments.Add(RoadSegment.Create(segmentId1, segmentSetId1, routeVersionId, 1));
            context.RoadSegmentSets.Add(RoadSegmentSet.Create(segmentSetId2, routeVersionId));
            context.RoadSegments.Add(RoadSegment.Create(segmentId2, segmentSetId2, routeVersionId, 2));
            await context.SaveChangesAsync();
        }

        int beforePlans;
        await using (var before = _sql.CreateDbContext())
        {
            beforePlans = await before.SurveyPlans.AsNoTracking().CountAsync();
        }

        var payloadA = new
        {
            scope = new[] { new { routeVersionId, segmentSetId = segmentSetId1, segmentIds = new[] { segmentId1 }, targetBand = "SURFACE" } },
            plannedAt = "2026-10-01T08:00:00Z",
            surveyType = "BASELINE"
        };
        const string idempotencyKey = "plan-conflict-key-001";

        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{projectId}/survey-plans")
        {
            Content = JsonContent.Create(payloadA)
        };
        firstRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        var first = await client.SendAsync(firstRequest);

        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var payloadB = new
        {
            scope = new[] { new { routeVersionId, segmentSetId = segmentSetId2, segmentIds = new[] { segmentId2 }, targetBand = "SURFACE" } },
            plannedAt = "2026-10-01T08:00:00Z",
            surveyType = "BASELINE"
        };

        using var conflictRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{projectId}/survey-plans")
        {
            Content = JsonContent.Create(payloadB)
        };
        conflictRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        var conflict = await client.SendAsync(conflictRequest);

        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problemCode = (await conflict.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();
        problemCode.Should().Be("duplicate_request");

        await using var verification = _sql.CreateDbContext();
        var planCount = await verification.SurveyPlans.AsNoTracking().CountAsync();
        planCount.Should().Be(beforePlans + 1, "conflict must not create second plan");

        var idempotencyCount = await verification.IdempotencyRecords.AsNoTracking().CountAsync(
            r => r.ActorUserId == manager.Id &&
                 r.ProjectId == projectId &&
                 r.Operation == "SurveyPlanV2Created" &&
                 r.IdempotencyKey == idempotencyKey);
        idempotencyCount.Should().Be(1, "conflict must not create second idempotency record");
    }

    /// <summary>
    /// Gap 3: Actor isolation - different actors with same key execute separately.
    /// NOT_RUN: Evidence pending BOX 3 verification.
    /// </summary>
    [Fact]
    public async Task UploadCreate_DifferentActorsSameKey_BothExecute()
    {
        var supervisor = await _sql.CreateUserAsync($"actor-iso-sup-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var manager1 = await _sql.CreateUserAsync($"actor-iso-pm1-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var manager2 = await _sql.CreateUserAsync($"actor-iso-pm2-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var content = Encoding.UTF8.GetBytes("actor isolation fixture");
        var checksum = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        var storage = new CharacterizationUploadStorage(content, checksum);
        await using var factory = new AuthenticationWebApplicationFactory(
            _sql.ConnectionString,
            configureTestServices: services =>
            {
                services.RemoveAll<IUploadObjectStorage>();
                services.AddSingleton<IUploadObjectStorage>(storage);
            });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        await AuthenticateAsync(client, supervisor.UserName!);
        var projectId = await CreateProjectAsync(client, manager1.Id);
        await CreateMembershipAsync(projectId, manager2.Id, UserRoleCode.ProjectManager);

        var payload = new
        {
            purpose = "DOCUMENT",
            projectId,
            targetId = (Guid?)null,
            fileName = "actor-isolation.pdf",
            mediaType = "application/pdf",
            sizeBytes = content.Length,
            checksumSha256 = checksum
        };
        const string idempotencyKey = "actor-isolation-key-001";

        await AuthenticateAsync(client, manager1.UserName!);
        using var actor1Request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/uploads")
        {
            Content = JsonContent.Create(payload)
        };
        actor1Request.Headers.Add("Idempotency-Key", idempotencyKey);
        var actor1Response = await client.SendAsync(actor1Request);

        actor1Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var uploadId1 = (await actor1Response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        await AuthenticateAsync(client, manager2.UserName!);
        using var actor2Request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/uploads")
        {
            Content = JsonContent.Create(payload)
        };
        actor2Request.Headers.Add("Idempotency-Key", idempotencyKey);
        var actor2Response = await client.SendAsync(actor2Request);

        actor2Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var uploadId2 = (await actor2Response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        uploadId1.Should().NotBe(uploadId2, "different actors must create separate uploads");

        await using var verification = _sql.CreateDbContext();
        var session1 = await verification.UploadSessions.AsNoTracking().SingleAsync(s => s.Id == uploadId1);
        var scope1 = await verification.FileScopes.AsNoTracking().SingleAsync(sc => sc.FileId == session1.FileId);
        scope1.OwnerUserId.Should().Be(manager1.Id);

        var session2 = await verification.UploadSessions.AsNoTracking().SingleAsync(s => s.Id == uploadId2);
        var scope2 = await verification.FileScopes.AsNoTracking().SingleAsync(sc => sc.FileId == session2.FileId);
        scope2.OwnerUserId.Should().Be(manager2.Id);

        var idempotency1 = await verification.IdempotencyRecords.AsNoTracking().SingleAsync(
            r => r.ActorUserId == manager1.Id &&
                 r.ProjectId == projectId &&
                 r.Operation == "UploadSessionCreated" &&
                 r.IdempotencyKey == idempotencyKey);
        idempotency1.Should().NotBeNull();

        var idempotency2 = await verification.IdempotencyRecords.AsNoTracking().SingleAsync(
            r => r.ActorUserId == manager2.Id &&
                 r.ProjectId == projectId &&
                 r.Operation == "UploadSessionCreated" &&
                 r.IdempotencyKey == idempotencyKey);
        idempotency2.Should().NotBeNull();
    }

    /// <summary>
    /// Gap 4: Project isolation - same actor/key with different projects execute separately.
    /// NOT_RUN: Evidence pending BOX 3 verification.
    /// </summary>
    [Fact]
    public async Task UploadCreate_SameActorSameKeyDifferentProjects_BothExecute()
    {
        var supervisor = await _sql.CreateUserAsync($"proj-iso-sup-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var manager = await _sql.CreateUserAsync($"proj-iso-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var content = Encoding.UTF8.GetBytes("project isolation fixture");
        var checksum = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        var storage = new CharacterizationUploadStorage(content, checksum);
        await using var factory = new AuthenticationWebApplicationFactory(
            _sql.ConnectionString,
            configureTestServices: services =>
            {
                services.RemoveAll<IUploadObjectStorage>();
                services.AddSingleton<IUploadObjectStorage>(storage);
            });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        await AuthenticateAsync(client, supervisor.UserName!);
        var projectId1 = await CreateProjectAsync(client, manager.Id);
        var projectId2 = await CreateProjectAsync(client, manager.Id);
        await AuthenticateAsync(client, manager.UserName!);

        const string idempotencyKey = "project-isolation-key-001";

        var payload1 = new
        {
            purpose = "DOCUMENT",
            projectId = projectId1,
            targetId = (Guid?)null,
            fileName = "project1.pdf",
            mediaType = "application/pdf",
            sizeBytes = content.Length,
            checksumSha256 = checksum
        };

        using var project1Request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/uploads")
        {
            Content = JsonContent.Create(payload1)
        };
        project1Request.Headers.Add("Idempotency-Key", idempotencyKey);
        var project1Response = await client.SendAsync(project1Request);

        project1Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var uploadId1 = (await project1Response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var payload2 = new
        {
            purpose = "DOCUMENT",
            projectId = projectId2,
            targetId = (Guid?)null,
            fileName = "project2.pdf",
            mediaType = "application/pdf",
            sizeBytes = content.Length,
            checksumSha256 = checksum
        };

        using var project2Request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/uploads")
        {
            Content = JsonContent.Create(payload2)
        };
        project2Request.Headers.Add("Idempotency-Key", idempotencyKey);
        var project2Response = await client.SendAsync(project2Request);

        project2Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var uploadId2 = (await project2Response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        uploadId1.Should().NotBe(uploadId2, "different projects must create separate uploads");

        await using var verification = _sql.CreateDbContext();
        var scope1 = await verification.FileScopes.AsNoTracking()
            .SingleAsync(sc => sc.FileId == (verification.UploadSessions.AsNoTracking().Single(s => s.Id == uploadId1).FileId));
        scope1.ProjectId.Should().Be(projectId1);

        var scope2 = await verification.FileScopes.AsNoTracking()
            .SingleAsync(sc => sc.FileId == (verification.UploadSessions.AsNoTracking().Single(s => s.Id == uploadId2).FileId));
        scope2.ProjectId.Should().Be(projectId2);

        var idempotency1 = await verification.IdempotencyRecords.AsNoTracking().SingleAsync(
            r => r.ActorUserId == manager.Id &&
                 r.ProjectId == projectId1 &&
                 r.Operation == "UploadSessionCreated" &&
                 r.IdempotencyKey == idempotencyKey);
        idempotency1.Should().NotBeNull();

        var idempotency2 = await verification.IdempotencyRecords.AsNoTracking().SingleAsync(
            r => r.ActorUserId == manager.Id &&
                 r.ProjectId == projectId2 &&
                 r.Operation == "UploadSessionCreated" &&
                 r.IdempotencyKey == idempotencyKey);
        idempotency2.Should().NotBeNull();
    }

    /// <summary>
    /// Gap 4: Project isolation for survey task creation.
    /// NOT_RUN: Evidence pending BOX 3 verification.
    /// </summary>
    [Fact]
    public async Task SurveyTaskCreate_SameActorSameKeyDifferentProjects_BothExecute()
    {
        var supervisor = await _sql.CreateUserAsync($"task-proj-iso-sup-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var manager = await _sql.CreateUserAsync($"task-proj-iso-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var operatorUser = await _sql.CreateUserAsync($"task-proj-iso-op-{Guid.NewGuid():N}", "Current1!", UserRoleCode.DroneOperator);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        await AuthenticateAsync(client, supervisor.UserName!);
        var projectId1 = await CreateProjectAsync(client, manager.Id);
        var routeVersionId1 = await CreateRoadSectionAsync(client, projectId1);
        var projectId2 = await CreateProjectAsync(client, manager.Id);
        var routeVersionId2 = await CreateRoadSectionAsync(client, projectId2);
        await AuthenticateAsync(client, manager.UserName!);

        var segmentSetId1 = Guid.NewGuid();
        var segmentId1 = Guid.NewGuid();
        var segmentSetId2 = Guid.NewGuid();
        var segmentId2 = Guid.NewGuid();
        await using (var context = _sql.CreateDbContext())
        {
            context.RoadSegmentSets.Add(RoadSegmentSet.Create(segmentSetId1, routeVersionId1));
            context.RoadSegments.Add(RoadSegment.Create(segmentId1, segmentSetId1, routeVersionId1, 1));
            context.RoadSegmentSets.Add(RoadSegmentSet.Create(segmentSetId2, routeVersionId2));
            context.RoadSegments.Add(RoadSegment.Create(segmentId2, segmentSetId2, routeVersionId2, 1));
            await context.SaveChangesAsync();
        }

        const string idempotencyKey = "task-project-isolation-key-001";

        var payload1 = new
        {
            scope = new[] { new { routeVersionId = routeVersionId1, segmentSetId = segmentSetId1, segmentIds = new[] { segmentId1 }, targetBand = "SURFACE" } },
            surveyType = "BASELINE",
            operatorId = operatorUser.Id,
            dueAt = "2026-10-04T17:00:00Z",
            accessPoint = (object?)null
        };

        using var project1Request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{projectId1}/survey-tasks")
        {
            Content = JsonContent.Create(payload1)
        };
        project1Request.Headers.Add("Idempotency-Key", idempotencyKey);
        var project1Response = await client.SendAsync(project1Request);

        project1Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var taskId1 = (await project1Response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var payload2 = new
        {
            scope = new[] { new { routeVersionId = routeVersionId2, segmentSetId = segmentSetId2, segmentIds = new[] { segmentId2 }, targetBand = "SURFACE" } },
            surveyType = "BASELINE",
            operatorId = operatorUser.Id,
            dueAt = "2026-10-04T17:00:00Z",
            accessPoint = (object?)null
        };

        using var project2Request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{projectId2}/survey-tasks")
        {
            Content = JsonContent.Create(payload2)
        };
        project2Request.Headers.Add("Idempotency-Key", idempotencyKey);
        var project2Response = await client.SendAsync(project2Request);

        project2Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var taskId2 = (await project2Response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        taskId1.Should().NotBe(taskId2, "different projects must create separate tasks");

        await using var verification = _sql.CreateDbContext();
        var task1 = await verification.SurveyRequests.AsNoTracking().SingleAsync(t => t.Id == taskId1);
        task1.ProjectId.Should().Be(projectId1);

        var task2 = await verification.SurveyRequests.AsNoTracking().SingleAsync(t => t.Id == taskId2);
        task2.ProjectId.Should().Be(projectId2);

        var idempotency1 = await verification.IdempotencyRecords.AsNoTracking().SingleAsync(
            r => r.ActorUserId == manager.Id &&
                 r.ProjectId == projectId1 &&
                 r.Operation == "SurveyTaskV2Created" &&
                 r.IdempotencyKey == idempotencyKey);
        idempotency1.Should().NotBeNull();

        var idempotency2 = await verification.IdempotencyRecords.AsNoTracking().SingleAsync(
            r => r.ActorUserId == manager.Id &&
                 r.ProjectId == projectId2 &&
                 r.Operation == "SurveyTaskV2Created" &&
                 r.IdempotencyKey == idempotencyKey);
        idempotency2.Should().NotBeNull();
    }

    /// <summary>
    /// Gap 5: Stored precondition outcome replay (Pattern A).
    /// NOT_RUN: Evidence pending BOX 3 verification.
    /// </summary>
    [Fact]
    public async Task SurveyPlanPostpone_StaleVersionThenReplay_ReturnsStoredPreconditionFailure()
    {
        var supervisor = await _sql.CreateUserAsync($"postpone-replay-sup-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var manager = await _sql.CreateUserAsync($"postpone-replay-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        await AuthenticateAsync(client, supervisor.UserName!);
        var projectId = await CreateProjectAsync(client, manager.Id);
        var routeVersionId = await CreateRoadSectionAsync(client, projectId);
        await AuthenticateAsync(client, manager.UserName!);

        var segmentSetId = Guid.NewGuid();
        var segmentId = Guid.NewGuid();
        await using (var context = _sql.CreateDbContext())
        {
            context.RoadSegmentSets.Add(RoadSegmentSet.Create(segmentSetId, routeVersionId));
            context.RoadSegments.Add(RoadSegment.Create(segmentId, segmentSetId, routeVersionId, 1));
            await context.SaveChangesAsync();
        }

        var planPayload = new
        {
            scope = new[] { new { routeVersionId, segmentSetId, segmentIds = new[] { segmentId }, targetBand = "SURFACE" } },
            plannedAt = "2026-10-01T08:00:00Z",
            plannedEndAt = "2026-12-31T23:59:59Z",
            surveyType = "BASELINE"
        };

        using var createPlanRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{projectId}/survey-plans")
        {
            Content = JsonContent.Create(planPayload)
        };
        createPlanRequest.Headers.Add("Idempotency-Key", $"postpone-replay-create-{Guid.NewGuid():N}");
        var createdPlan = await client.SendAsync(createPlanRequest);
        createdPlan.StatusCode.Should().Be(HttpStatusCode.Created);

        var planResponseText = await createdPlan.Content.ReadAsStringAsync();
        var planBody = JsonDocument.Parse(planResponseText).RootElement;

        if (!planBody.TryGetProperty("id", out var idProperty))
        {
            throw new InvalidOperationException($"Response missing 'id' property. Actual response: {planResponseText}");
        }

        var planId = idProperty.GetGuid();
        var version1 = planBody.GetProperty("version").GetString()!;

        var plannedEndAt = DateTimeOffset.Parse("2026-12-31T23:59:59Z");

        await using (var context = _sql.CreateDbContext())
        {
            var plan = await context.SurveyPlans.SingleAsync(p => p.Id == planId);
            var actualPlannedEnd = plan.PlannedEndAt;
            var safePostpone1 = actualPlannedEnd.AddDays(-10);
            plan.Postpone(safePostpone1);
            await context.SaveChangesAsync();
        }

        string version2;
        DateTimeOffset actualPlannedEndAt;
        await using (var context = _sql.CreateDbContext())
        {
            var plan = await context.SurveyPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
            version2 = Convert.ToBase64String(plan.RowVersion);
            actualPlannedEndAt = plan.PlannedEndAt;
        }

        const string idempotencyKey = "postpone-replay-key-001";
        var safePostpone2 = actualPlannedEndAt.AddDays(-5);
        var postponePayload = new
        {
            newPlannedStartAt = safePostpone2.ToString("o"),
            reason = "Test stale version scenario"
        };

        using var staleRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/survey-plans/{planId}/postpone")
        {
            Content = JsonContent.Create(postponePayload)
        };
        staleRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        staleRequest.Headers.TryAddWithoutValidation("If-Match", $"\"{version1}\"");
        var staleResponse = await client.SendAsync(staleRequest);

        staleResponse.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        using var staleBodyStream = await staleResponse.Content.ReadAsStreamAsync();
        using var staleReader = new StreamReader(staleBodyStream);
        var staleBodyText = await staleReader.ReadToEndAsync();

        Guid firstOperationId;
        string firstRequestFingerprint;
        string firstOutcomeJson;
        await using (var snapshot1 = _sql.CreateDbContext())
        {
            var postponementCount = await snapshot1.SurveyPlanPostponements.AsNoTracking().CountAsync(p => p.SurveyPlanId == planId);
            postponementCount.Should().Be(0, "first postpone was direct mutation, not through API, so no postponement record exists yet");

            var receipt = await snapshot1.IdempotencyRecords.AsNoTracking().SingleAsync(
                r => r.ActorUserId == manager.Id &&
                     r.ProjectId == null &&
                     r.Operation == "SurveyPlanV2Postponed" &&
                     r.IdempotencyKey == idempotencyKey);
            receipt.OutcomeJson.Should().Contain("\"Status\":7", "status 7 = ConcurrencyConflict");
            firstOperationId = receipt.OperationId;
            firstRequestFingerprint = receipt.RequestFingerprint;
            firstOutcomeJson = receipt.OutcomeJson;
            firstRequestFingerprint.Should().NotBeNullOrWhiteSpace("receipt captures request fingerprint");
        }

        await using (var context = _sql.CreateDbContext())
        {
            var plan = await context.SurveyPlans.SingleAsync(p => p.Id == planId);
            var safePostpone3 = actualPlannedEndAt.AddDays(-3);
            plan.Postpone(safePostpone3);
            await context.SaveChangesAsync();
        }

        string version3;
        await using (var context = _sql.CreateDbContext())
        {
            var plan = await context.SurveyPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
            version3 = Convert.ToBase64String(plan.RowVersion);
        }

        version3.Should().NotBe(version2, "plan version should have changed");

        using var replayRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/survey-plans/{planId}/postpone")
        {
            Content = JsonContent.Create(postponePayload)
        };
        replayRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        replayRequest.Headers.TryAddWithoutValidation("If-Match", $"\"{version1}\"");
        var replayResponse = await client.SendAsync(replayRequest);

        replayResponse.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        using var replayBodyStream = await replayResponse.Content.ReadAsStreamAsync();
        using var replayReader = new StreamReader(replayBodyStream);
        var replayBodyText = await replayReader.ReadToEndAsync();

        var staleBody = JsonDocument.Parse(staleBodyText).RootElement;
        var replayBody = JsonDocument.Parse(replayBodyText).RootElement;
        replayBody.GetProperty("status").GetInt32().Should().Be(412);
        replayBody.GetProperty("code").GetString().Should().Be("auth_concurrency_conflict");
        replayBody.GetProperty("instance").GetString().Should().Be(staleBody.GetProperty("instance").GetString(),
            "replay must return stored precondition failure with matching correlation identifier");

        staleBody.GetProperty("status").GetInt32().Should().Be(412, "stale request stored 412 outcome");
        staleBody.GetProperty("code").GetString().Should().Be("auth_concurrency_conflict", "stale request stored concurrency conflict code");

        await using (var snapshot2 = _sql.CreateDbContext())
        {
            var postponementCount = await snapshot2.SurveyPlanPostponements.AsNoTracking().CountAsync(p => p.SurveyPlanId == planId);
            postponementCount.Should().Be(0, "replay must not add postponement, and no API postpone succeeded yet");

            var plan = await snapshot2.SurveyPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
            var currentVersion = Convert.ToBase64String(plan.RowVersion);
            currentVersion.Should().Be(version3, "replay must not mutate plan version");

            var receipt = await snapshot2.IdempotencyRecords.AsNoTracking().SingleAsync(
                r => r.ActorUserId == manager.Id &&
                     r.ProjectId == null &&
                     r.Operation == "SurveyPlanV2Postponed" &&
                     r.IdempotencyKey == idempotencyKey);
            receipt.OperationId.Should().Be(firstOperationId, "replay returns stored operation ID");
            receipt.RequestFingerprint.Should().Be(firstRequestFingerprint, "replay preserves request fingerprint");
            receipt.OutcomeJson.Should().Be(firstOutcomeJson, "replay returns stored outcome without re-executing handler");
            receipt.OutcomeJson.Should().Contain("\"Status\":7", "stored outcome contains ConcurrencyConflict status");

            var idempotencyCount = await snapshot2.IdempotencyRecords.AsNoTracking().CountAsync(
                r => r.ActorUserId == manager.Id &&
                     r.ProjectId == null &&
                     r.Operation == "SurveyPlanV2Postponed" &&
                     r.IdempotencyKey == idempotencyKey);
            idempotencyCount.Should().Be(1, "exactly one receipt persisted for this idempotency key");
        }
    }

    /// <summary>
    /// Gap 6: Authorization change before replay - actor role revoked, replay returns stored outcome.
    /// NOT_RUN: Evidence pending BOX 3 verification.
    /// </summary>
    [Fact]
    public async Task ProjectCreate_ActorRoleRevokedAfterCreate_ReplayReturnsStoredOutcome()
    {
        var supervisor = await _sql.CreateUserAsync($"auth-change-sup-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var manager = await _sql.CreateUserAsync($"auth-change-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        await AuthenticateAsync(client, supervisor.UserName!);

        var operationId = Guid.NewGuid();
        var payload = new
        {
            projectCode = $"AUTH-{Guid.NewGuid():N}",
            name = "Authorization change test project",
            engineeringUtmSrid = 32648,
            startDate = "2026-09-01",
            endDate = "2027-09-01",
            primaryProjectManagerUserId = manager.Id,
            handover = new { documentNo = $"HD-{Guid.NewGuid():N}", handoverDate = "2026-08-31" },
            operationId
        };

        var firstResponse = await client.PostAsJsonAsync("/api/v1/projects", payload);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstBodyText = await firstResponse.Content.ReadAsStringAsync();
        var projectId = (await firstResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("projectId").GetGuid();

        await using (var roleContext = _sql.CreateDbContext())
        {
            var repo = new IdentityRepository(roleContext);
            var supervisorUser = await roleContext.Users.SingleAsync(u => u.Id == supervisor.Id);
            var currentVersion = supervisorUser.RowVersion;

            await repo.ChangeUserRoleAtomicAsync(
                supervisor.Id,
                UserRoleCode.ProjectManager,
                currentVersion,
                supervisor.Id,
                Guid.NewGuid(),
                reason: "Test role revocation");
        }

        await AuthenticateAsync(client, supervisor.UserName!);

        var replayResponse = await client.PostAsJsonAsync("/api/v1/projects", payload);
        replayResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "replay must enforce current authorization, preventing revoked actors from accessing stored outcomes");

        await using var verification = _sql.CreateDbContext();
        var projectCount = await verification.Projects.AsNoTracking().CountAsync(p => p.Id == projectId);
        projectCount.Should().Be(1, "first operation should have succeeded and persisted exactly one project");

        var user = await verification.Users.AsNoTracking().SingleAsync(u => u.Id == supervisor.Id);
        user.RoleCode.Should().Be(UserRoleCode.ProjectManager, "supervisor role should be changed to PM");

        var idempotencyRecord = await verification.IdempotencyRecords.AsNoTracking()
            .SingleOrDefaultAsync(r =>
                r.ActorUserId == supervisor.Id &&
                r.Operation == "ProjectCreated" &&
                r.IdempotencyKey == operationId.ToString("N"));
        idempotencyRecord.Should().NotBeNull("first operation should have stored idempotency record");
        idempotencyRecord!.ProjectId.Should().BeNull("project creation stores null projectId in idempotency record");

        var storedOutcome = JsonSerializer.Deserialize<JsonElement>(idempotencyRecord.OutcomeJson);
        storedOutcome.GetProperty("ProjectId").GetGuid().Should().Be(projectId,
            "idempotency record should contain stored outcome with correct projectId");
    }

    /// <summary>
    /// Gap 9: Stored outcome vs current state - replay returns original status, not mutated state.
    /// NOT_RUN: Evidence pending BOX 3 verification.
    /// </summary>
    [Fact]
    public async Task UploadCreate_StatusMutatedAfterCreate_ReplayReturnsOriginalStatus()
    {
        var supervisor = await _sql.CreateUserAsync($"status-mut-sup-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var manager = await _sql.CreateUserAsync($"status-mut-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var content = Encoding.UTF8.GetBytes("status mutation fixture");
        var checksum = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        var storage = new CharacterizationUploadStorage(content, checksum);
        await using var factory = new AuthenticationWebApplicationFactory(
            _sql.ConnectionString,
            configureTestServices: services =>
            {
                services.RemoveAll<IUploadObjectStorage>();
                services.AddSingleton<IUploadObjectStorage>(storage);
            });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        await AuthenticateAsync(client, supervisor.UserName!);
        var projectId = await CreateProjectAsync(client, manager.Id);
        await AuthenticateAsync(client, manager.UserName!);

        var payload = new
        {
            purpose = "DOCUMENT",
            projectId,
            targetId = (Guid?)null,
            fileName = "status-mutation.pdf",
            mediaType = "application/pdf",
            sizeBytes = content.Length,
            checksumSha256 = checksum
        };
        const string idempotencyKey = "status-mutation-key-001";

        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/uploads")
        {
            Content = JsonContent.Create(payload)
        };
        firstRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        var first = await client.SendAsync(firstRequest);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstBody = await first.Content.ReadFromJsonAsync<JsonElement>();
        var uploadId = firstBody.GetProperty("id").GetGuid();
        var originalStatus = firstBody.GetProperty("status").GetString();
        originalStatus.Should().Be("PENDING");

        await using (var context = _sql.CreateDbContext())
        {
            var session = await context.UploadSessions.SingleAsync(s => s.Id == uploadId);
            session.StartUploading("storage-upload-id", DateTimeOffset.UtcNow);
            await context.SaveChangesAsync();

            session = await context.UploadSessions.SingleAsync(s => s.Id == uploadId);
            session.StartVerification(Convert.ToBase64String(session.RowVersion), DateTimeOffset.UtcNow);
            session.MarkVerified();
            await context.SaveChangesAsync();
        }

        await using (var verification1 = _sql.CreateDbContext())
        {
            var session = await verification1.UploadSessions.AsNoTracking().SingleAsync(s => s.Id == uploadId);
            session.Status.Should().Be(UploadSessionStatus.Verified, "current DB state should show VERIFIED");
        }

        using var replayRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/uploads")
        {
            Content = JsonContent.Create(payload)
        };
        replayRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        var replay = await client.SendAsync(replayRequest);

        replay.StatusCode.Should().Be(HttpStatusCode.Created);
        var replayBody = await replay.Content.ReadFromJsonAsync<JsonElement>();
        var replayStatus = replayBody.GetProperty("status").GetString();
        replayStatus.Should().Be("PENDING", "replay must return original stored status, not current VERIFIED state");

        await using (var verification2 = _sql.CreateDbContext())
        {
            var session = await verification2.UploadSessions.AsNoTracking().SingleAsync(s => s.Id == uploadId);
            session.Status.Should().Be(UploadSessionStatus.Verified, "replay must not mutate current DB state");
        }
    }

    private static async Task<Guid> CreateProjectAsync(HttpClient client, Guid managerId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            projectCode = $"IDEM-{Guid.NewGuid():N}",
            name = "Idempotency test project",
            engineeringUtmSrid = 32648,
            startDate = "2026-09-01",
            endDate = "2027-09-01",
            primaryProjectManagerUserId = managerId,
            handover = new { documentNo = $"HD-{Guid.NewGuid():N}", handoverDate = "2026-08-31" },
            operationId = Guid.NewGuid()
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("projectId").GetGuid();
    }

    private static async Task<Guid> CreateRoadSectionAsync(HttpClient client, Guid projectId)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/road-sections", new
        {
            code = $"RS-{Guid.NewGuid():N}",
            srid = 32648,
            coordinates = new[] { new { x = 500000d, y = 1100000d }, new { x = 500100d, y = 1100100d } },
            effectiveFrom = "2026-09-21T08:00:00+07:00",
            changeReason = "Initial alignment",
            operationId = Guid.NewGuid()
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("roadSectionVersionId").GetGuid();
    }

    private static async Task AuthenticateAsync(HttpClient client, string username)
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = AuthenticationSqlServerFixture.EmailFor(username),
            password = "Current1!"
        });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
    }

    private async Task CreateMembershipAsync(Guid projectId, Guid userId, UserRoleCode role)
    {
        var membership = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            RoleCode = role,
            IsPrimary = false,
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),
            Status = ProjectMemberStatus.Active
        };

        await using var context = _sql.CreateDbContext();
        context.ProjectMembers.Add(membership);
        await context.SaveChangesAsync();
    }
    // <<<RF1008C01_CONTINUATION_003>>>
}

/// <summary>
/// Mock upload storage for characterization tests.
/// </summary>
internal sealed class CharacterizationUploadStorage : IUploadObjectStorage
{
    private readonly byte[] _content;
    private readonly string _checksum;

    public CharacterizationUploadStorage(byte[] content, string checksum)
    {
        _content = content;
        _checksum = checksum;
    }

    public Task<string> InitiateAsync(string objectKey, string mediaType, CancellationToken cancellationToken = default) =>
        Task.FromResult($"upload-{Guid.NewGuid():N}");

    public Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(
        string objectKey,
        string uploadId,
        IReadOnlyList<int> partNumbers,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        var parts = partNumbers.Select(number => new PresignedUploadPart(number, $"https://storage.test/part/{number}", expiresAt)).ToList();
        return Task.FromResult<IReadOnlyList<PresignedUploadPart>>(parts);
    }

    public Task<UploadObjectVerification> CompleteAndVerifyAsync(
        string objectKey,
        string uploadId,
        IReadOnlyList<CompletedStoragePart> parts,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new UploadObjectVerification(_content.Length, _checksum, "application/octet-stream"));

    public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default) =>
        Task.FromResult<Stream>(new MemoryStream(_content));
}
