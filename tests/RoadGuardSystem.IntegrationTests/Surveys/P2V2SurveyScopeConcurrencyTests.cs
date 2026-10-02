using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Surveys;
using RoadGuardSystem.Repositories.Surveys;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Surveys;

[Trait("TaskId", "P2-054/P2-055/P2-011/P2-012")]
public sealed class P2V2SurveyScopeConcurrencyTests : IClassFixture<IdentitySqlServerFixture>
{
    private readonly IdentitySqlServerFixture _fixture;

    public P2V2SurveyScopeConcurrencyTests(IdentitySqlServerFixture fixture) => _fixture = fixture;

    [Fact(DisplayName = "P2 V2: plan persists every route-version scope row and SQL rowversion")]
    public async Task SurveyPlan_MultiRouteScopeAndRowVersion_RoundTrips()
    {
        await using var context = _fixture.CreateDbContext();
        var fixture = await CreateFixtureAsync(context, 2);
        var plan = SurveyPlan.Create(
            Guid.NewGuid(),
            fixture.ProjectId,
            fixture.RoadSectionIds[0],
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
            SurveyType.Original,
            SurveyPlanStatus.Planned,
            "[]",
            fixture.RouteVersionIds[0]);
        context.SurveyPlans.Add(plan);
        context.SurveyPlanScopes.AddRange(
            fixture.RouteVersionIds.Select((routeVersionId, index) => SurveyPlanScope.Create(
                Guid.NewGuid(),
                plan.Id,
                routeVersionId,
                Guid.NewGuid(),
                $"[\"{Guid.NewGuid()}\"]",
                index == 0 ? "SURFACE" : "LEFT_EDGE")));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var persisted = await context.SurveyPlans.AsNoTracking().SingleAsync(item => item.Id == plan.Id);
        var scopes = await context.SurveyPlanScopes.AsNoTracking().Where(item => item.SurveyPlanId == plan.Id).ToListAsync();

        persisted.RowVersion.Should().NotBeEmpty();
        scopes.Should().HaveCount(2);
        scopes.Select(item => item.RouteSectionVersionId).Should().BeEquivalentTo(fixture.RouteVersionIds);
    }

    [Fact(DisplayName = "P2 V2: stale survey plan rowversion rejects concurrent postpone")]
    public async Task SurveyPlan_StaleRowVersion_IsRejected()
    {
        await using var setupContext = _fixture.CreateDbContext();
        var fixture = await CreateFixtureAsync(setupContext, 1);
        var plan = SurveyPlan.Create(
            Guid.NewGuid(),
            fixture.ProjectId,
            fixture.RoadSectionIds[0],
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
            SurveyType.Original,
            SurveyPlanStatus.Planned,
            "[]",
            fixture.RouteVersionIds[0]);
        setupContext.SurveyPlans.Add(plan);
        await setupContext.SaveChangesAsync();
        setupContext.ChangeTracker.Clear();

        await using var firstContext = _fixture.CreateDbContext();
        await using var secondContext = _fixture.CreateDbContext();
        var first = await firstContext.SurveyPlans.SingleAsync(item => item.Id == plan.Id);
        var second = await secondContext.SurveyPlans.SingleAsync(item => item.Id == plan.Id);
        var firstVersion = first.RowVersion.ToArray();
        second.RowVersion.Should().Equal(firstVersion);

        first.Postpone(null);
        firstContext.SurveyPlanPostponements.Add(SurveyPlanPostponement.Create(Guid.NewGuid(), first.Id, DateTimeOffset.UtcNow, "First update", null));
        await firstContext.SaveChangesAsync();

        second.Postpone(null);
        secondContext.SurveyPlanPostponements.Add(SurveyPlanPostponement.Create(Guid.NewGuid(), second.Id, DateTimeOffset.UtcNow, "Stale update", null));
        var staleSave = () => secondContext.SaveChangesAsync();

        await staleSave.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact(DisplayName = "P2 V2: plan rejects a root route version absent from requested scope")]
    public async Task SurveyPlan_RootRouteVersionOutsideScope_ReturnsConflict()
    {
        await using var context = _fixture.CreateDbContext();
        var repository = new SurveyV2PersistenceService(context, new IdempotencyOperationService(context));
        var rootRouteVersionId = Guid.NewGuid();
        var scopedRouteVersionId = Guid.NewGuid();

        var result = await repository.CreatePlanAsync(new SurveyV2PlanCreationRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            rootRouteVersionId,
            DateTimeOffset.UtcNow.AddDays(1),
            SurveyType.Original,
            "{}",
            $"scope-root-mismatch-{Guid.NewGuid():N}",
            new string('a', 64),
            Guid.NewGuid(),
            [new SurveyV2ScopeRequest(scopedRouteVersionId, Guid.NewGuid(), $"[\"{Guid.NewGuid()}\"]", "SURFACE")]));

        result.Status.Should().Be(SurveyV2PersistenceStatus.Conflict);
    }

    [Fact(DisplayName = "P2 V2: dataset cannot claim a segment outside its assigned scope")]
    public async Task SubmitDataset_OutsideAssignedScope_LeavesNoDataset()
    {
        await using var context = _fixture.CreateDbContext();
        var fixture = await CreateDatasetFixtureAsync(context, "SURVEY_VIDEO");
        var repository = new SurveyV2PersistenceService(context, new IdempotencyOperationService(context));
        var requestedScope = $"[{{\"routeVersionId\":\"{fixture.RouteVersionId}\",\"segmentSetId\":\"{fixture.SegmentSetId}\",\"segmentIds\":[\"{Guid.NewGuid()}\"],\"targetBand\":\"SURFACE\"}}]";

        var result = await repository.SubmitDatasetAsync(new SurveyDatasetSubmissionRequest(
            fixture.OperatorId, fixture.TaskId, [fixture.FileId], [], DateTimeOffset.UtcNow,
            Guid.NewGuid(), requestedScope, fixture.TaskVersion, $"dataset-scope-{Guid.NewGuid():N}",
            new string('a', 64), Guid.NewGuid()));

        result.Status.Should().Be(SurveyDatasetPersistenceStatus.Conflict);
        (await context.SurveyDataVersions.CountAsync()).Should().Be(0);
        (await context.SurveyFiles.CountAsync()).Should().Be(0);
    }

    [Fact(DisplayName = "P2 V2: dataset cannot use a verified document as a survey video")]
    public async Task SubmitDataset_WrongFilePurpose_LeavesNoDataset()
    {
        await using var context = _fixture.CreateDbContext();
        var fixture = await CreateDatasetFixtureAsync(context, "DOCUMENT");
        var repository = new SurveyV2PersistenceService(context, new IdempotencyOperationService(context));
        var requestedScope = $"[{{\"routeVersionId\":\"{fixture.RouteVersionId}\",\"segmentSetId\":\"{fixture.SegmentSetId}\",\"segmentIds\":[\"{fixture.SegmentId}\"],\"targetBand\":\"SURFACE\"}}]";

        var result = await repository.SubmitDatasetAsync(new SurveyDatasetSubmissionRequest(
            fixture.OperatorId, fixture.TaskId, [fixture.FileId], [], DateTimeOffset.UtcNow,
            Guid.NewGuid(), requestedScope, fixture.TaskVersion, $"dataset-purpose-{Guid.NewGuid():N}",
            new string('b', 64), Guid.NewGuid()));

        result.Status.Should().Be(SurveyDatasetPersistenceStatus.Conflict);
        (await context.SurveyDataVersions.CountAsync()).Should().Be(0);
        (await context.SurveyFiles.CountAsync()).Should().Be(0);
    }

    [Fact(DisplayName = "P2 V2: assigned scope and verified survey video persist once on replay")]
    public async Task SubmitDataset_AssignedScopeAndSurveyVideo_CommitsOnce()
    {
        await using var context = _fixture.CreateDbContext();
        var fixture = await CreateDatasetFixtureAsync(context, "SURVEY_VIDEO");
        var repository = new SurveyV2PersistenceService(context, new IdempotencyOperationService(context));
        var requestedScope = $"[{{\"routeVersionId\":\"{fixture.RouteVersionId}\",\"segmentSetId\":\"{fixture.SegmentSetId}\",\"segmentIds\":[\"{fixture.SegmentId}\"],\"targetBand\":\"SURFACE\"}}]";
        var request = new SurveyDatasetSubmissionRequest(
            fixture.OperatorId, fixture.TaskId, [fixture.FileId], [], DateTimeOffset.UtcNow,
            Guid.NewGuid(), requestedScope, fixture.TaskVersion, $"dataset-valid-{Guid.NewGuid():N}",
            new string('d', 64), Guid.NewGuid());

        var first = await repository.SubmitDatasetAsync(request);
        var replay = await repository.SubmitDatasetAsync(request);
        context.ChangeTracker.Clear();

        first.Status.Should().Be(SurveyDatasetPersistenceStatus.Success);
        replay.Status.Should().Be(SurveyDatasetPersistenceStatus.Replayed);
        replay.Dataset!.Id.Should().Be(first.Dataset!.Id);
        (await context.SurveyDataVersions.CountAsync()).Should().Be(1);
        (await context.SurveyFiles.CountAsync()).Should().Be(1);
        (await context.AuditLogs.CountAsync(row => row.EventType == "survey_dataset_submitted")).Should().Be(1);
    }

    private async Task<DatasetFixture> CreateDatasetFixtureAsync(RoadGuardDbContext context, string filePurpose)
    {
        await _fixture.SeedRolesAsync(context);
        var route = await CreateFixtureAsync(context, 1);
        var now = DateTimeOffset.UtcNow;
        var operatorId = Guid.NewGuid();
        var userName = $"p2-v2-dataset-{operatorId:N}";
        var user = new ApplicationUser
        {
            Id = operatorId,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            DisplayName = "Dataset SQL fixture operator",
            PasswordHash = "fixture-password-hash",
            RoleCode = UserRoleCode.DroneOperator,
            Status = UserStatus.Active,
            CreatedAt = now
        };
        var segmentSet = RoadSegmentSet.Create(Guid.NewGuid(), route.RouteVersionIds[0]);
        var segment = RoadSegment.Create(Guid.NewGuid(), segmentSet.Id, route.RouteVersionIds[0], 1);
        var task = SurveyRequest.Create(Guid.NewGuid(), route.ProjectId, route.RoadSectionIds[0], null,
            operatorId, SurveyType.Original, SurveyRequestStatus.Accepted, now, now.AddDays(1), "{}", route.RouteVersionIds[0]);
        var assignment = SurveyAssignment.Create(Guid.NewGuid(), task.Id, operatorId, operatorId,
            now, now, null, null, null, null);
        var scope = SurveyRequestScope.Create(Guid.NewGuid(), task.Id, route.RouteVersionIds[0],
            segmentSet.Id, $"[\"{segment.Id}\"]", "SURFACE");
        var checksum = new string('c', 64);
        var file = StoredFile.Create(Guid.NewGuid(), $"objects/dataset/{Guid.NewGuid():N}", "source.mp4",
            "video/mp4", 16, checksum, operatorId, now, null);
        var fileScope = FileScope.Create(Guid.NewGuid(), file.Id, route.ProjectId, task.Id, operatorId, filePurpose, now);
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, operatorId, file.StorageUri,
            filePurpose, "video/mp4", 16, checksum, 16, now.AddHours(1));
        context.AddRange(user, segmentSet, segment, task, assignment, scope, file, fileScope, upload);
        await context.SaveChangesAsync();
        upload.StartUploading("fixture-upload", now);
        await context.SaveChangesAsync();
        upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now);
        await context.SaveChangesAsync();
        upload.MarkVerified();
        await context.SaveChangesAsync();
        return new DatasetFixture(operatorId, task.Id, Convert.ToBase64String(task.RowVersion),
            route.RouteVersionIds[0], segmentSet.Id, segment.Id, file.Id);
    }

    private async Task<FixtureData> CreateFixtureAsync(RoadGuardDbContext context, int routeCount)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = $"P2V2-{Guid.NewGuid():N}",
            Name = "P2 V2 scope fixture",
            Status = ProjectStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Projects.Add(project);
        var sections = Enumerable.Range(0, routeCount)
            .Select(index => RoadSection.Create(Guid.NewGuid(), project.Id, $"P2V2-ROAD-{index}-{Guid.NewGuid():N}"))
            .ToArray();
        context.RoadSections.AddRange(sections);
        await context.SaveChangesAsync();

        var geometryFactory = new GeometryFactory(new PrecisionModel(), 32648);
        var versions = sections.Select((section, index) => RoadSectionVersion.Create(
            Guid.NewGuid(),
            section.Id,
            1,
            true,
            geometryFactory.CreateLineString([
                new Coordinate(500000 + index * 100, 1200000),
                new Coordinate(500100 + index * 100, 1200000)]),
            DateTimeOffset.UtcNow,
            "P2 V2 scope fixture")).ToArray();
        context.RoadSectionVersions.AddRange(versions);
        await context.SaveChangesAsync();
        return new FixtureData(project.Id, sections.Select(item => item.Id).ToArray(), versions.Select(item => item.Id).ToArray());
    }

    private sealed record FixtureData(Guid ProjectId, IReadOnlyList<Guid> RoadSectionIds, IReadOnlyList<Guid> RouteVersionIds);

    private sealed record DatasetFixture(Guid OperatorId, Guid TaskId, string TaskVersion,
        Guid RouteVersionId, Guid SegmentSetId, Guid SegmentId, Guid FileId);
}
