using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using NetTopologySuite;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

[Trait("TaskId", "P1-20")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class P120ProjectUpdateTests
{
    private readonly AuthenticationSqlServerFixture _sql;

    public P120ProjectUpdateTests(AuthenticationSqlServerFixture sql)
    {
        _sql = sql;
    }

    [Fact]
    public async Task UpdateProjectSupervisorSuccessPersistsAuditAndReplays()
    {
        var setup = await CreateProjectSetupAsync();
        var request = ValidUpdate(setup.RowVersion);

        var updated = await setup.Client.PutAsJsonAsync($"/api/v1/projects/{setup.ProjectId}", request);

        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await updated.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("projectId").GetGuid().Should().Be(setup.ProjectId);
        body.GetProperty("projectCode").GetString().Should().Be(setup.ProjectCode);
        body.GetProperty("name").GetString().Should().Be("Updated northern bypass");
        body.GetProperty("description").GetString().Should().Be("Updated management metadata");
        body.GetProperty("engineeringUtmSrid").GetInt32().Should().Be(32649);
        body.GetProperty("status").GetString().Should().Be("ACTIVE");
        var updatedRowVersion = body.GetProperty("rowVersion").GetString();
        updatedRowVersion.Should().NotBeNullOrWhiteSpace().And.NotBe(setup.RowVersion);

        var laterUpdate = ValidUpdate(updatedRowVersion!) with
        {
            Name = "Later accepted update",
            OperationId = Guid.NewGuid()
        };
        (await setup.Client.PutAsJsonAsync($"/api/v1/projects/{setup.ProjectId}", laterUpdate))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var replay = await setup.Client.PutAsJsonAsync($"/api/v1/projects/{setup.ProjectId}", request);
        replay.StatusCode.Should().Be(HttpStatusCode.OK);
        var replayBody = await replay.Content.ReadFromJsonAsync<JsonElement>();
        replayBody.GetProperty("name").GetString().Should().Be("Updated northern bypass");
        replayBody.GetProperty("rowVersion").GetString().Should().Be(updatedRowVersion);

        await using var verification = _sql.CreateDbContext();
        var persisted = await verification.Projects.AsNoTracking().SingleAsync(project => project.Id == setup.ProjectId);
        persisted.Name.Should().Be("Later accepted update");
        persisted.ProjectCode.Should().Be(setup.ProjectCode);
        var audits = await verification.AuditLogs.AsNoTracking()
            .Where(audit => audit.EntityId == setup.ProjectId && audit.EventType == "project_updated")
            .OrderBy(audit => audit.OccurredAtUtc)
            .ToListAsync();
        audits.Should().HaveCount(2);
        using (var before = JsonDocument.Parse(audits[0].BeforeSnapshot!))
        using (var after = JsonDocument.Parse(audits[0].AfterSnapshot!))
        {
            before.RootElement.EnumerateObject().Select(property => property.Name)
                .Should().BeEquivalentTo("name", "description", "engineering_utm_srid", "start_date", "end_date");
            after.RootElement.EnumerateObject().Select(property => property.Name)
                .Should().BeEquivalentTo("name", "description", "engineering_utm_srid", "start_date", "end_date");
            after.RootElement.GetProperty("name").GetString().Should().Be("Updated northern bypass");
            audits[0].BeforeSnapshot.Should().NotContain("project_code").And.NotContain("row_version")
                .And.NotContain("token").And.NotContain("password");
            audits[0].AfterSnapshot.Should().NotContain("project_code").And.NotContain("row_version")
                .And.NotContain("token").And.NotContain("password");
        }
        (await verification.IdempotencyRecords.CountAsync(record =>
            record.ActorUserId == setup.SupervisorId &&
            record.ProjectId == setup.ProjectId &&
            record.Operation == "ProjectUpdated")).Should().Be(2);
    }

    [Fact]
    public async Task UpdateProjectRejectsStaleAndConcurrentWrites()
    {
        var identicalSetup = await CreateProjectSetupAsync();
        var identical = ValidUpdate(identicalSetup.RowVersion);
        var identicalResponses = await Task.WhenAll(
            identicalSetup.Client.PutAsJsonAsync($"/api/v1/projects/{identicalSetup.ProjectId}", identical),
            identicalSetup.Client.PutAsJsonAsync($"/api/v1/projects/{identicalSetup.ProjectId}", identical));
        identicalResponses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.OK);
        var identicalBodies = await Task.WhenAll(identicalResponses.Select(response =>
            response.Content.ReadAsStringAsync()));
        identicalBodies.Distinct().Should().ContainSingle();
        await using (var identicalVerification = _sql.CreateDbContext())
        {
            (await identicalVerification.AuditLogs.CountAsync(audit =>
                audit.EntityId == identicalSetup.ProjectId && audit.EventType == "project_updated")).Should().Be(1);
            (await identicalVerification.IdempotencyRecords.CountAsync(record =>
                record.ProjectId == identicalSetup.ProjectId && record.Operation == "ProjectUpdated")).Should().Be(1);
        }

        var staleSetup = await CreateProjectSetupAsync();
        var first = ValidUpdate(staleSetup.RowVersion);
        (await staleSetup.Client.PutAsJsonAsync($"/api/v1/projects/{staleSetup.ProjectId}", first))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var stale = first with { OperationId = Guid.NewGuid(), Name = "Stale overwrite" };
        var staleResponse = await staleSetup.Client.PutAsJsonAsync(
            $"/api/v1/projects/{staleSetup.ProjectId}",
            stale);
        staleResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var staleProblem = await staleResponse.Content.ReadFromJsonAsync<JsonElement>();
        staleProblem.GetProperty("code").GetString().Should().Be("concurrency_conflict");
        staleProblem.GetProperty("detail").GetString().Should().NotContain("creation");

        var concurrentSetup = await CreateProjectSetupAsync();
        var left = ValidUpdate(concurrentSetup.RowVersion) with
        {
            Name = "Concurrent left",
            OperationId = Guid.NewGuid()
        };
        var right = ValidUpdate(concurrentSetup.RowVersion) with
        {
            Name = "Concurrent right",
            OperationId = Guid.NewGuid()
        };
        var responses = await Task.WhenAll(
            concurrentSetup.Client.PutAsJsonAsync($"/api/v1/projects/{concurrentSetup.ProjectId}", left),
            concurrentSetup.Client.PutAsJsonAsync($"/api/v1/projects/{concurrentSetup.ProjectId}", right));
        responses.Count(response => response.StatusCode == HttpStatusCode.OK).Should().Be(1);
        responses.Count(response => response.StatusCode == HttpStatusCode.Conflict).Should().Be(1);
        var conflict = responses.Single(response => response.StatusCode == HttpStatusCode.Conflict);
        (await ProblemCodeAsync(conflict)).Should().Be("concurrency_conflict");

        await using var verification = _sql.CreateDbContext();
        (await verification.AuditLogs.CountAsync(audit =>
            audit.EntityId == concurrentSetup.ProjectId && audit.EventType == "project_updated")).Should().Be(1);
        (await verification.IdempotencyRecords.CountAsync(record =>
            record.ProjectId == concurrentSetup.ProjectId && record.Operation == "ProjectUpdated")).Should().Be(1);

        var retrySetup = await CreateProjectSetupAsync();
        var failBeforeCommit = new FailFirstCommitInterceptor();
        var retryOptions = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(_sql.ConnectionString, sql => sql.UseNetTopologySuite())
            .ReplaceService<IExecutionStrategyFactory, TestRetryingExecutionStrategyFactory>()
            .AddInterceptors(failBeforeCommit)
            .Options;
        await using (var retryContext = new RoadGuardDbContext(retryOptions))
        {
            var persistence = new ProjectUpdatePersistenceService(retryContext);
            var retryResult = await persistence.UpdateAsync(new ProjectUpdatePersistenceRequest(
                retrySetup.SupervisorId,
                retrySetup.ProjectId,
                "Retry-safe update",
                "Transient retry fixture",
                32648,
                new DateOnly(2026, 3, 1),
                new DateOnly(2027, 3, 1),
                Convert.FromBase64String(retrySetup.RowVersion),
                Guid.NewGuid(),
                null));
            retryResult.Status.Should().Be(ProjectUpdatePersistenceStatus.Success);
        }
        failBeforeCommit.Attempts.Should().Be(2);

        await using var retryVerification = _sql.CreateDbContext();
        (await retryVerification.AuditLogs.CountAsync(audit =>
            audit.EntityId == retrySetup.ProjectId && audit.EventType == "project_updated")).Should().Be(1);
        (await retryVerification.IdempotencyRecords.CountAsync(record =>
            record.ProjectId == retrySetup.ProjectId && record.Operation == "ProjectUpdated")).Should().Be(1);
    }

    [Fact]
    public async Task UpdateProjectEnforcesAuthorizationValidationAndIdempotencyFingerprint()
    {
        var setup = await CreateProjectSetupAsync();
        var projectManager = await _sql.CreateUserAsync(
            $"update_forbidden_{Guid.NewGuid():N}",
            "Current1!",
            UserRoleCode.ProjectManager);
        using var projectManagerClient = CreateClient(setup.Factory);
        await AuthenticateAsync(projectManagerClient, projectManager.UserName!, "Current1!");
        var forbidden = await projectManagerClient.PutAsJsonAsync(
            $"/api/v1/projects/{setup.ProjectId}",
            ValidUpdate(setup.RowVersion));
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ProblemCodeAsync(forbidden)).Should().Be("access_forbidden");

        var malformedVersion = ValidUpdate("not-base64");
        var invalidVersion = await setup.Client.PutAsJsonAsync(
            $"/api/v1/projects/{setup.ProjectId}",
            malformedVersion);
        invalidVersion.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ProblemCodeAsync(invalidVersion)).Should().Be("validation_error");

        var wrongLengthVersion = ValidUpdate(Convert.ToBase64String([1, 2, 3, 4]));
        var wrongLengthResponse = await setup.Client.PutAsJsonAsync(
            $"/api/v1/projects/{setup.ProjectId}",
            wrongLengthVersion);
        wrongLengthResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ProblemCodeAsync(wrongLengthResponse)).Should().Be("validation_error");

        var invalidSrid = ValidUpdate(setup.RowVersion) with { EngineeringUtmSrid = 3857 };
        var invalidSridResponse = await setup.Client.PutAsJsonAsync(
            $"/api/v1/projects/{setup.ProjectId}",
            invalidSrid);
        invalidSridResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var invalidDates = ValidUpdate(setup.RowVersion) with
        {
            StartDate = new DateOnly(2028, 1, 2),
            EndDate = new DateOnly(2028, 1, 1)
        };
        var invalidDateResponse = await setup.Client.PutAsJsonAsync(
            $"/api/v1/projects/{setup.ProjectId}",
            invalidDates);
        invalidDateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var missing = await setup.Client.PutAsJsonAsync(
            $"/api/v1/projects/{Guid.NewGuid()}",
            ValidUpdate(setup.RowVersion));
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ProblemCodeAsync(missing)).Should().Be("project_not_found");

        var accepted = ValidUpdate(setup.RowVersion);
        (await setup.Client.PutAsJsonAsync($"/api/v1/projects/{setup.ProjectId}", accepted))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        var changedPayload = accepted with { Name = "Changed retry payload" };
        var duplicate = await setup.Client.PutAsJsonAsync(
            $"/api/v1/projects/{setup.ProjectId}",
            changedPayload);
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProblemCodeAsync(duplicate)).Should().Be("duplicate_request");
    }

    private async Task<ProjectSetup> CreateProjectSetupAsync()
    {
        var supervisor = await _sql.CreateUserAsync(
            $"update_supervisor_{Guid.NewGuid():N}",
            "Current1!",
            UserRoleCode.Supervisor);
        var projectManager = await _sql.CreateUserAsync(
            $"update_pm_{Guid.NewGuid():N}",
            "Current1!",
            UserRoleCode.ProjectManager);
        var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        var client = CreateClient(factory);
        await AuthenticateAsync(client, supervisor.UserName!, "Current1!");
        var projectCode = $"P-{Guid.NewGuid():N}";
        var create = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            projectCode,
            name = "Project before update",
            description = "Original metadata",
            engineeringUtmSrid = 32648,
            startDate = new DateOnly(2026, 1, 1),
            endDate = new DateOnly(2027, 1, 1),
            primaryProjectManagerUserId = projectManager.Id,
            handover = new
            {
                documentNo = $"HD-{Guid.NewGuid():N}",
                handoverDate = new DateOnly(2025, 12, 31)
            },
            operationId = Guid.NewGuid()
        });
        create.EnsureSuccessStatusCode();
        var body = await create.Content.ReadFromJsonAsync<JsonElement>();
        return new ProjectSetup(
            factory,
            client,
            supervisor.Id,
            body.GetProperty("projectId").GetGuid(),
            projectCode,
            body.GetProperty("rowVersion").GetString()!);
    }

    private static UpdateProjectRequest ValidUpdate(string rowVersion) => new(
        "Updated northern bypass",
        "Updated management metadata",
        32649,
        new DateOnly(2026, 2, 1),
        new DateOnly(2027, 2, 1),
        rowVersion,
        Guid.NewGuid());

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

    private sealed record UpdateProjectRequest(
        string Name,
        string? Description,
        int? EngineeringUtmSrid,
        DateOnly? StartDate,
        DateOnly? EndDate,
        string ExpectedRowVersion,
        Guid OperationId);

    private sealed record ProjectSetup(
        AuthenticationWebApplicationFactory Factory,
        HttpClient Client,
        Guid SupervisorId,
        Guid ProjectId,
        string ProjectCode,
        string RowVersion);

    private sealed class FailFirstCommitInterceptor : DbTransactionInterceptor
    {
        private int _attempts;

        public int Attempts => _attempts;

        public override ValueTask<InterceptionResult> TransactionCommittingAsync(
            DbTransaction transaction,
            TransactionEventData eventData,
            InterceptionResult result,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _attempts) == 1)
            {
                throw new TestTransientException();
            }

            return ValueTask.FromResult(result);
        }
    }

    private sealed class TestRetryingExecutionStrategyFactory : IExecutionStrategyFactory
    {
        private readonly ExecutionStrategyDependencies _dependencies;

        public TestRetryingExecutionStrategyFactory(ExecutionStrategyDependencies dependencies)
        {
            _dependencies = dependencies;
        }

        public IExecutionStrategy Create() => new TestRetryingExecutionStrategy(_dependencies);
    }

    private sealed class TestRetryingExecutionStrategy : ExecutionStrategy
    {
        public TestRetryingExecutionStrategy(ExecutionStrategyDependencies dependencies)
            : base(dependencies, 2, TimeSpan.Zero)
        {
        }

        protected override bool ShouldRetryOn(Exception exception) => exception is TestTransientException;
    }

    private sealed class TestTransientException : Exception;
}
