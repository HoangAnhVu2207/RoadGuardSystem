using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Warranties;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Warranties;

[Trait("TaskId", "P1-20")]
public sealed class P120WarrantyCreationTests : IClassFixture<AuthenticationSqlServerFixture>
{
    private readonly AuthenticationSqlServerFixture _sql;

    public P120WarrantyCreationTests(AuthenticationSqlServerFixture sql)
    {
        _sql = sql;
    }

    [Fact]
    public async Task CreateWarrantyPersistsOneAuditedAggregateAndReplaysExactResponse()
    {
        var setup = await CreateProjectSetupAsync();
        var request = ValidRequest(setup.HandoverDocumentId);

        var created = await setup.Client.PostAsJsonAsync(
            $"/api/v1/projects/{setup.ProjectId}/warranties",
            request);

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Headers.Location.Should().BeNull();
        var bodyText = await created.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(bodyText);
        var warrantyId = body.RootElement.GetProperty("warrantyId").GetGuid();
        body.RootElement.GetProperty("projectId").GetGuid().Should().Be(setup.ProjectId);
        body.RootElement.GetProperty("handoverDocumentId").GetGuid().Should().Be(setup.HandoverDocumentId);
        body.RootElement.GetProperty("scope").GetString().Should().Be("PROJECT");
        body.RootElement.GetProperty("status").GetString().Should().Be("ACTIVE");
        body.RootElement.GetProperty("retainedValue").GetDecimal().Should().Be(125_000_000m);

        await using (var laterState = _sql.CreateDbContext())
        {
            await laterState.Warranties
                .Where(warranty => warranty.Id == warrantyId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    warranty => warranty.Status,
                    WarrantyStatus.Expired));
        }

        var replay = await setup.Client.PostAsJsonAsync(
            $"/api/v1/projects/{setup.ProjectId}/warranties",
            request);
        replay.StatusCode.Should().Be(HttpStatusCode.Created);
        (await replay.Content.ReadAsStringAsync()).Should().Be(bodyText);

        await using var verification = _sql.CreateDbContext();
        (await verification.Warranties.CountAsync(warranty => warranty.Id == warrantyId)).Should().Be(1);
        var audit = await verification.AuditLogs.AsNoTracking().SingleAsync(entry =>
            entry.EntityId == warrantyId && entry.EventType == "warranty_created");
        audit.BeforeSnapshot.Should().BeNull();
        audit.AfterSnapshot.Should().NotBeNull();
        using var snapshot = JsonDocument.Parse(audit.AfterSnapshot!);
        snapshot.RootElement.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(
            "project_id",
            "road_section_id",
            "handover_document_id",
            "handover_date",
            "warranty_start_date",
            "warranty_end_date",
            "scope",
            "source_document_id",
            "status");
        audit.AfterSnapshot.Should().NotContain("terms").And.NotContain("retained_value")
            .And.NotContain("token").And.NotContain("password");
        (await verification.IdempotencyRecords.CountAsync(record =>
            record.ActorUserId == setup.SupervisorId &&
            record.ProjectId == setup.ProjectId &&
            record.Operation == "WarrantyCreated")).Should().Be(1);

        await AssertCommitFailureRecoveryAsync(setup, new FailOnceBeforeCommitInterceptor());
        await AssertCommitFailureRecoveryAsync(setup, new FailOnceAfterCommitInterceptor());
    }

    [Fact]
    public async Task CreateWarrantyRequiresAuthenticatedSupervisorWithoutPersistingData()
    {
        var setup = await CreateProjectSetupAsync();
        var projectManager = await _sql.CreateUserAsync(
            $"warranty_forbidden_{Guid.NewGuid():N}",
            "Current1!",
            UserRoleCode.ProjectManager);
        var request = ValidRequest(setup.HandoverDocumentId);

        using (var anonymousClient = CreateClient(setup.Factory))
        {
            var anonymous = await anonymousClient.PostAsJsonAsync(
                $"/api/v1/projects/{setup.ProjectId}/warranties",
                request);
            anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await ProblemCodeAsync(anonymous)).Should().Be("auth_unauthorized");
        }

        using (var projectManagerClient = CreateClient(setup.Factory))
        {
            await AuthenticateAsync(projectManagerClient, projectManager.UserName!, "Current1!");
            var forbidden = await projectManagerClient.PostAsJsonAsync(
                $"/api/v1/projects/{setup.ProjectId}/warranties",
                request);
            forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await ProblemCodeAsync(forbidden)).Should().Be("access_forbidden");
        }

        await using var verification = _sql.CreateDbContext();
        (await verification.Warranties.AnyAsync(warranty => warranty.ProjectId == setup.ProjectId))
            .Should().BeFalse();
    }

    [Fact]
    public async Task CreateWarrantyRejectsInvalidReferencesDatesAndChangedReplay()
    {
        var setup = await CreateProjectSetupAsync();
        var otherProject = await CreateProjectAsync(setup.Client, setup.ProjectManagerId);
        var currentRoadSectionId = Guid.NewGuid();
        var otherRoadSectionId = Guid.NewGuid();
        await using (var arrange = _sql.CreateDbContext())
        {
            arrange.RoadSections.Add(RoadSection.Create(
                currentRoadSectionId,
                setup.ProjectId,
                $"RS-{Guid.NewGuid():N}"));
            arrange.RoadSections.Add(RoadSection.Create(
                otherRoadSectionId,
                otherProject.ProjectId,
                $"RS-{Guid.NewGuid():N}"));
            await arrange.SaveChangesAsync();
        }

        var invalidDates = ValidRequest(setup.HandoverDocumentId) with
        {
            WarrantyStartDate = new DateOnly(2028, 1, 2),
            WarrantyEndDate = new DateOnly(2028, 1, 1)
        };
        await AssertProblemAsync(setup, invalidDates, HttpStatusCode.BadRequest, "validation_error");

        var retainedValueOverflow = ValidRequest(setup.HandoverDocumentId) with
        {
            RetainedValue = 100_000_000_000_000_000m
        };
        await AssertProblemAsync(
            setup,
            retainedValueOverflow,
            HttpStatusCode.BadRequest,
            "validation_error");

        var retainedValueExcessScale = ValidRequest(setup.HandoverDocumentId) with { RetainedValue = 1.001m };
        await AssertProblemAsync(
            setup,
            retainedValueExcessScale,
            HttpStatusCode.BadRequest,
            "validation_error");

        var wrongRoad = ValidRequest(setup.HandoverDocumentId) with
        {
            Scope = "ROAD_SECTION",
            RoadSectionId = otherRoadSectionId
        };
        await AssertProblemAsync(setup, wrongRoad, HttpStatusCode.NotFound, "warranty_road_section_not_found");

        var wrongHandover = ValidRequest(otherProject.HandoverDocumentId);
        await AssertProblemAsync(
            setup,
            wrongHandover,
            HttpStatusCode.NotFound,
            "warranty_handover_document_not_found");

        var missingSource = ValidRequest(setup.HandoverDocumentId) with { SourceDocumentId = Guid.NewGuid() };
        await AssertProblemAsync(
            setup,
            missingSource,
            HttpStatusCode.NotFound,
            "warranty_source_document_not_found");

        var accepted = ValidRequest(setup.HandoverDocumentId);
        (await setup.Client.PostAsJsonAsync(
            $"/api/v1/projects/{setup.ProjectId}/warranties",
            accepted)).StatusCode.Should().Be(HttpStatusCode.Created);
        var changed = accepted with { Terms = "Changed terms" };
        await AssertProblemAsync(setup, changed, HttpStatusCode.Conflict, "duplicate_request");

        var roadScoped = ValidRequest(setup.HandoverDocumentId) with
        {
            Scope = "ROAD_SECTION",
            RoadSectionId = currentRoadSectionId
        };
        var roadScopedResponse = await setup.Client.PostAsJsonAsync(
            $"/api/v1/projects/{setup.ProjectId}/warranties",
            roadScoped);
        roadScopedResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        (await roadScopedResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("scope").GetString().Should().Be("ROAD_SECTION");

        await using var verification = _sql.CreateDbContext();
        (await verification.Warranties.CountAsync(warranty => warranty.ProjectId == setup.ProjectId))
            .Should().Be(2);
    }

    private static async Task AssertProblemAsync(
        WarrantySetup setup,
        CreateWarrantyRequest request,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        var response = await setup.Client.PostAsJsonAsync(
            $"/api/v1/projects/{setup.ProjectId}/warranties",
            request);
        response.StatusCode.Should().Be(expectedStatus);
        (await ProblemCodeAsync(response)).Should().Be(expectedCode);
    }

    private async Task<WarrantySetup> CreateProjectSetupAsync()
    {
        var supervisor = await _sql.CreateUserAsync(
            $"warranty_supervisor_{Guid.NewGuid():N}",
            "Current1!",
            UserRoleCode.Supervisor);
        var projectManager = await _sql.CreateUserAsync(
            $"warranty_pm_{Guid.NewGuid():N}",
            "Current1!",
            UserRoleCode.ProjectManager);
        var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        var client = CreateClient(factory);
        await AuthenticateAsync(client, supervisor.UserName!, "Current1!");
        var project = await CreateProjectAsync(client, projectManager.Id);
        return new WarrantySetup(
            factory,
            client,
            supervisor.Id,
            projectManager.Id,
            project.ProjectId,
            project.HandoverDocumentId);
    }

    private async Task AssertCommitFailureRecoveryAsync(
        WarrantySetup setup,
        FailOnceTransactionInterceptor interceptor)
    {
        var operationId = Guid.NewGuid();
        await using (var context = CommitFailureDbContext.Create(_sql.ConnectionString, interceptor))
        {
            var persistence = new WarrantyPersistenceService(
                context,
                new IdempotencyOperationService(context));
            var result = await persistence.CreateAsync(new WarrantyCreationPersistenceRequest(
                setup.SupervisorId,
                setup.ProjectId,
                null,
                setup.HandoverDocumentId,
                new DateOnly(2026, 8, 31),
                new DateOnly(2026, 9, 1),
                new DateOnly(2027, 8, 31),
                10_000m,
                WarrantyScope.Project,
                "Retry-safe warranty",
                null,
                WarrantyStatus.Active,
                operationId,
                null));
            result.Status.Should().Be(WarrantyCreationPersistenceStatus.Success);
        }

        interceptor.FailureCount.Should().Be(1);
        await using var verification = _sql.CreateDbContext();
        var record = await verification.IdempotencyRecords.AsNoTracking().SingleAsync(candidate =>
            candidate.ActorUserId == setup.SupervisorId &&
            candidate.ProjectId == setup.ProjectId &&
            candidate.Operation == "WarrantyCreated" &&
            candidate.IdempotencyKey == operationId.ToString("N"));
        (await verification.Warranties.CountAsync(warranty => warranty.Id == record.OperationId)).Should().Be(1);
        (await verification.AuditLogs.CountAsync(audit =>
            audit.EntityId == record.OperationId && audit.EventType == "warranty_created")).Should().Be(1);
    }

    private static async Task<CreatedProject> CreateProjectAsync(HttpClient client, Guid? projectManagerId = null)
    {
        if (projectManagerId is null)
        {
            throw new InvalidOperationException("A project manager is required for project setup.");
        }

        var response = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            projectCode = $"P-{Guid.NewGuid():N}",
            name = "Warranty test project",
            engineeringUtmSrid = 32648,
            startDate = "2026-09-01",
            endDate = "2029-09-01",
            primaryProjectManagerUserId = projectManagerId.Value,
            handover = new
            {
                documentNo = $"HD-{Guid.NewGuid():N}",
                handoverDate = "2026-08-31"
            },
            operationId = Guid.NewGuid()
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new CreatedProject(
            body.GetProperty("projectId").GetGuid(),
            body.GetProperty("handoverDocumentId").GetGuid());
    }

    private static CreateWarrantyRequest ValidRequest(Guid handoverDocumentId) => new(
        RoadSectionId: null,
        HandoverDocumentId: handoverDocumentId,
        HandoverDate: new DateOnly(2026, 8, 31),
        WarrantyStartDate: new DateOnly(2026, 9, 1),
        WarrantyEndDate: new DateOnly(2028, 8, 31),
        RetainedValue: 125_000_000m,
        Scope: "PROJECT",
        Terms: "Twenty-four month workmanship warranty",
        SourceDocumentId: null,
        Status: "ACTIVE",
        OperationId: Guid.NewGuid());

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

    private sealed record WarrantySetup(
        AuthenticationWebApplicationFactory Factory,
        HttpClient Client,
        Guid SupervisorId,
        Guid ProjectManagerId,
        Guid ProjectId,
        Guid HandoverDocumentId);

    private sealed record CreatedProject(Guid ProjectId, Guid HandoverDocumentId);

    private sealed record CreateWarrantyRequest(
        Guid? RoadSectionId,
        Guid? HandoverDocumentId,
        DateOnly HandoverDate,
        DateOnly WarrantyStartDate,
        DateOnly WarrantyEndDate,
        decimal? RetainedValue,
        string Scope,
        string? Terms,
        Guid? SourceDocumentId,
        string Status,
        Guid OperationId);
}
