using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

[Trait("TaskId", "P1-20")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class P120ProjectCreationTests
{
    private readonly AuthenticationSqlServerFixture _sql;

    public P120ProjectCreationTests(AuthenticationSqlServerFixture sql)
    {
        _sql = sql;
    }

    [Fact]
    public async Task CreateProjectSupervisorSuccessPersistsOneAtomicAggregateAndReplays()
    {
        var supervisor = await _sql.CreateUserAsync(
            $"create_supervisor_{Guid.NewGuid():N}",
            "Current1!",
            UserRoleCode.Supervisor);
        var projectManager = await _sql.CreateUserAsync(
            $"create_pm_{Guid.NewGuid():N}",
            "Current1!",
            UserRoleCode.ProjectManager);
        var request = ValidRequest(projectManager.Id);

        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);
        await AuthenticateAsync(client, supervisor.UserName!, "Current1!");

        var created = await client.PostAsJsonAsync("/api/v1/projects", request);

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Headers.Location.Should().NotBeNull();
        var bodyText = await created.Content.ReadAsStringAsync();
        using var bodyDocument = JsonDocument.Parse(bodyText);
        var body = bodyDocument.RootElement;
        var projectId = body.GetProperty("projectId").GetGuid();
        var handoverDocumentId = body.GetProperty("handoverDocumentId").GetGuid();
        body.GetProperty("projectCode").GetString().Should().Be(request.ProjectCode);
        body.GetProperty("status").GetString().Should().Be("ACTIVE");
        body.GetProperty("primaryProjectManagerUserId").GetGuid().Should().Be(projectManager.Id);
        var rowVersion = body.GetProperty("rowVersion").GetString();
        rowVersion.Should().NotBeNullOrWhiteSpace();

        var laterUpdate = await client.PutAsJsonAsync($"/api/v1/projects/{projectId}", new
        {
            name = "Later project name",
            description = "Changed after creation",
            engineeringUtmSrid = 32648,
            startDate = "2026-09-01",
            endDate = "2027-09-01",
            expectedRowVersion = rowVersion,
            operationId = Guid.NewGuid()
        });
        laterUpdate.StatusCode.Should().Be(HttpStatusCode.OK);

        await using (var changedState = _sql.CreateDbContext())
        {
            var persistedProjectManager = await changedState.Users.SingleAsync(user => user.Id == projectManager.Id);
            persistedProjectManager.Status = UserStatus.Suspended;
            persistedProjectManager.SuspendedAt = DateTimeOffset.UtcNow;
            await changedState.SaveChangesAsync();
        }

        var replay = await client.PostAsJsonAsync("/api/v1/projects", request);
        replay.StatusCode.Should().Be(HttpStatusCode.Created);
        (await replay.Content.ReadAsStringAsync()).Should().Be(bodyText);

        await using var verification = _sql.CreateDbContext();
        (await verification.Projects.CountAsync(project => project.Id == projectId)).Should().Be(1);
        (await verification.ProjectMembers.CountAsync(member =>
            member.ProjectId == projectId &&
            member.UserId == projectManager.Id &&
            member.IsPrimary &&
            member.Status == ProjectMemberStatus.Active)).Should().Be(1);
        (await verification.HandoverDocuments.CountAsync(document =>
            document.Id == handoverDocumentId && document.ProjectId == projectId)).Should().Be(1);
        (await verification.AuditLogs.CountAsync(audit =>
            audit.EntityId == projectId && audit.EventType == "project_created")).Should().Be(1);
        (await verification.IdempotencyRecords.CountAsync(record =>
            record.ActorUserId == supervisor.Id && record.Operation == "ProjectCreated")).Should().Be(1);

        await using (var reactivate = _sql.CreateDbContext())
        {
            var persistedProjectManager = await reactivate.Users.SingleAsync(user => user.Id == projectManager.Id);
            persistedProjectManager.Status = UserStatus.Active;
            persistedProjectManager.SuspendedAt = null;
            await reactivate.SaveChangesAsync();
        }

        var concurrentRequest = ValidRequest(projectManager.Id);
        var concurrentResponses = await Task.WhenAll(
            client.PostAsJsonAsync("/api/v1/projects", concurrentRequest),
            client.PostAsJsonAsync("/api/v1/projects", concurrentRequest));
        concurrentResponses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.Created);
        var concurrentIds = await Task.WhenAll(concurrentResponses.Select(async response =>
            (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("projectId").GetGuid()));
        concurrentIds.Distinct().Should().ContainSingle();

        await using var concurrencyVerification = _sql.CreateDbContext();
        (await concurrencyVerification.Projects.CountAsync(project =>
            project.ProjectCode == concurrentRequest.ProjectCode)).Should().Be(1);
        (await concurrencyVerification.AuditLogs.CountAsync(audit =>
            audit.EntityId == concurrentIds[0] && audit.EventType == "project_created")).Should().Be(1);

        await AssertCommitFailureRecoveryAsync(
            supervisor.Id,
            projectManager.Id,
            new FailOnceBeforeCommitInterceptor());
        await AssertCommitFailureRecoveryAsync(
            supervisor.Id,
            projectManager.Id,
            new FailOnceAfterCommitInterceptor());
    }

    [Fact]
    public async Task CreateProjectRequiresAuthenticatedSupervisorWithoutPersistingData()
    {
        var projectManager = await _sql.CreateUserAsync(
            $"forbidden_pm_{Guid.NewGuid():N}",
            "Current1!",
            UserRoleCode.ProjectManager);
        var request = ValidRequest(projectManager.Id);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);

        using (var anonymousClient = CreateClient(factory))
        {
            var anonymous = await anonymousClient.PostAsJsonAsync("/api/v1/projects", request);
            anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await ProblemCodeAsync(anonymous)).Should().Be("auth_unauthorized");
        }

        using (var projectManagerClient = CreateClient(factory))
        {
            await AuthenticateAsync(projectManagerClient, projectManager.UserName!, "Current1!");
            var forbidden = await projectManagerClient.PostAsJsonAsync("/api/v1/projects", request);
            forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await ProblemCodeAsync(forbidden)).Should().Be("access_forbidden");
        }

        await using var verification = _sql.CreateDbContext();
        (await verification.Projects.AnyAsync(project => project.ProjectCode == request.ProjectCode))
            .Should().BeFalse();
    }

    [Fact]
    public async Task CreateProjectInvalidOrChangedRequestReturnsStableErrorsWithoutExtraProject()
    {
        var supervisor = await _sql.CreateUserAsync(
            $"validation_supervisor_{Guid.NewGuid():N}",
            "Current1!",
            UserRoleCode.Supervisor);
        var projectManager = await _sql.CreateUserAsync(
            $"validation_pm_{Guid.NewGuid():N}",
            "Current1!",
            UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);
        await AuthenticateAsync(client, supervisor.UserName!, "Current1!");

        var missingHandoverDateProjectCode = $"P-{Guid.NewGuid():N}";
        var missingHandoverDate = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            projectCode = missingHandoverDateProjectCode,
            name = "Missing handover date",
            engineeringUtmSrid = 32648,
            primaryProjectManagerUserId = projectManager.Id,
            handover = new { documentNo = $"HD-{Guid.NewGuid():N}" },
            operationId = Guid.NewGuid()
        });
        missingHandoverDate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ProblemCodeAsync(missingHandoverDate)).Should().Be("validation_error");

        var missingHandover = ValidRequest(projectManager.Id) with { Handover = null };
        var invalid = await client.PostAsJsonAsync("/api/v1/projects", missingHandover);
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ProblemCodeAsync(invalid)).Should().Be("validation_error");

        var missingProjectManager = ValidRequest(Guid.NewGuid());
        var missing = await client.PostAsJsonAsync("/api/v1/projects", missingProjectManager);
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ProblemCodeAsync(missing)).Should().Be("project_primary_pm_not_found");

        var invalidSrid = ValidRequest(projectManager.Id) with { EngineeringUtmSrid = 3857 };
        var invalidSridResponse = await client.PostAsJsonAsync("/api/v1/projects", invalidSrid);
        invalidSridResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ProblemCodeAsync(invalidSridResponse)).Should().Be("validation_error");

        var invalidDateRange = ValidRequest(projectManager.Id) with
        {
            StartDate = new DateOnly(2027, 1, 2),
            EndDate = new DateOnly(2027, 1, 1)
        };
        var invalidDateResponse = await client.PostAsJsonAsync("/api/v1/projects", invalidDateRange);
        invalidDateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ProblemCodeAsync(invalidDateResponse)).Should().Be("validation_error");

        var missingFile = ValidRequest(projectManager.Id);
        missingFile = missingFile with
        {
            Handover = missingFile.Handover! with { FileId = Guid.NewGuid() }
        };
        var missingFileResponse = await client.PostAsJsonAsync("/api/v1/projects", missingFile);
        missingFileResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ProblemCodeAsync(missingFileResponse)).Should().Be("project_handover_file_not_found");

        var first = ValidRequest(projectManager.Id);
        (await client.PostAsJsonAsync("/api/v1/projects", first))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        var changedPayload = first with { Name = "Changed project name" };
        var conflict = await client.PostAsJsonAsync("/api/v1/projects", changedPayload);
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProblemCodeAsync(conflict)).Should().Be("duplicate_request");

        var duplicateCode = first with
        {
            OperationId = Guid.NewGuid(),
            Handover = first.Handover! with { DocumentNo = $"HD-{Guid.NewGuid():N}" }
        };
        var duplicateCodeConflict = await client.PostAsJsonAsync("/api/v1/projects", duplicateCode);
        duplicateCodeConflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProblemCodeAsync(duplicateCodeConflict)).Should().Be("project_code_conflict");

        var delimiterFirst = ValidRequest(projectManager.Id) with
        {
            ProjectCode = "A|B",
            Name = "C"
        };
        (await client.PostAsJsonAsync("/api/v1/projects", delimiterFirst))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        var delimiterChanged = delimiterFirst with
        {
            ProjectCode = "A",
            Name = "B|C"
        };
        var delimiterConflict = await client.PostAsJsonAsync("/api/v1/projects", delimiterChanged);
        delimiterConflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProblemCodeAsync(delimiterConflict)).Should().Be("duplicate_request");

        await using var verification = _sql.CreateDbContext();
        (await verification.Projects.CountAsync(project => project.ProjectCode == first.ProjectCode))
            .Should().Be(1);
        verification.Projects.Any(project => project.ProjectCode == missingHandover.ProjectCode)
            .Should().BeFalse();
        verification.Projects.Any(project => project.ProjectCode == missingHandoverDateProjectCode)
            .Should().BeFalse();
        verification.Projects.Any(project => project.ProjectCode == missingProjectManager.ProjectCode)
            .Should().BeFalse();
    }

    private static CreateProjectRequest ValidRequest(Guid projectManagerUserId) => new(
        $"P-{Guid.NewGuid():N}",
        "Northern bypass rehabilitation",
        "Initial project created after handover",
        32648,
        new DateOnly(2026, 9, 1),
        new DateOnly(2027, 9, 1),
        projectManagerUserId,
        new HandoverRequest(
            $"HD-{Guid.NewGuid():N}",
            new DateOnly(2026, 8, 31),
            null,
            "Accepted construction handover"),
        Guid.NewGuid());

    private async Task AssertCommitFailureRecoveryAsync(
        Guid supervisorId,
        Guid projectManagerId,
        FailOnceTransactionInterceptor interceptor)
    {
        var projectCode = $"P-RETRY-{Guid.NewGuid():N}";
        var operationId = Guid.NewGuid();
        await using (var context = CommitFailureDbContext.Create(_sql.ConnectionString, interceptor))
        {
            var persistence = new ProjectCreationPersistenceService(
                context,
                new IdempotencyOperationService(context));
            var result = await persistence.CreateAsync(new ProjectCreationPersistenceRequest(
                supervisorId,
                projectCode,
                "Retry-safe project creation",
                null,
                32648,
                new DateOnly(2026, 9, 1),
                new DateOnly(2027, 9, 1),
                projectManagerId,
                $"HD-{Guid.NewGuid():N}",
                new DateOnly(2026, 8, 31),
                null,
                null,
                operationId,
                null));
            result.Status.Should().Be(
                interceptor is FailOnceAfterCommitInterceptor
                    ? ProjectCreationPersistenceStatus.Replayed
                    : ProjectCreationPersistenceStatus.Success);
        }

        interceptor.FailureCount.Should().Be(1);
        await using var verification = _sql.CreateDbContext();
        var projectId = await verification.Projects
            .Where(project => project.ProjectCode == projectCode)
            .Select(project => project.Id)
            .SingleAsync();
        (await verification.AuditLogs.CountAsync(audit =>
            audit.EntityId == projectId && audit.EventType == "project_created")).Should().Be(1);
        (await verification.IdempotencyRecords.CountAsync(record =>
            record.ActorUserId == supervisorId &&
            record.Operation == "ProjectCreated" &&
            record.IdempotencyKey == operationId.ToString("N"))).Should().Be(1);
    }

    private static HttpClient CreateClient(AuthenticationWebApplicationFactory factory) =>
        factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

    private static async Task AuthenticateAsync(HttpClient client, string username, string password)
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { username, password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            body.GetProperty("accessToken").GetString());
    }

    private static async Task<string> ProblemCodeAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("code").GetString()!;
    }

    private sealed record CreateProjectRequest(
        string ProjectCode,
        string Name,
        string? Description,
        int? EngineeringUtmSrid,
        DateOnly? StartDate,
        DateOnly? EndDate,
        Guid PrimaryProjectManagerUserId,
        HandoverRequest? Handover,
        Guid OperationId);

    private sealed record HandoverRequest(
        string DocumentNo,
        DateOnly HandoverDate,
        Guid? FileId,
        string? Notes);
}
