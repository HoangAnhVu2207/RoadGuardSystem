using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using System.Text.Json;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Processing;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Processing;

[Trait("Finding", "A08-02")]
public sealed class A0802ValidationReplayTests : IClassFixture<IdentitySqlServerFixture>
{
    private readonly IdentitySqlServerFixture _fixture;

    public A0802ValidationReplayTests(IdentitySqlServerFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task StoredConflict_FirstAndReplayRemainConflict_AndReceiptIsStable()
    {
        var scope = await SeedAsync(AIModelVersionStatus.Draft, validPair: false);
        var request = Request(scope, "a0802-conflict");

        var first = await ExecuteAsync(request);
        var replay = await ExecuteAsync(request);

        first.Status.Should().Be(ProcessingJobPersistenceStatus.Conflict);
        replay.Status.Should().Be(ProcessingJobPersistenceStatus.Conflict);
        await AssertReceiptAsync(scope, request, first.Status);
    }

    [Fact]
    public async Task StoredInvalidInput_FirstAndReplayRemainInvalidInput_AndReceiptIsStable()
    {
        var scope = await SeedAsync(AIModelVersionStatus.Released, validPair: false);
        var request = Request(scope, "a0802-invalid");

        var first = await ExecuteAsync(request);
        var replay = await ExecuteAsync(request);

        first.Status.Should().Be(ProcessingJobPersistenceStatus.InvalidInput);
        replay.Status.Should().Be(ProcessingJobPersistenceStatus.InvalidInput);
        await AssertReceiptAsync(scope, request, first.Status);
    }

    [Fact]
    public async Task StoredSuccess_ReplayReturnsReplayed_AndDoesNotDuplicateRunOrOutbox()
    {
        var scope = await SeedAsync(AIModelVersionStatus.Released, validPair: true);
        var request = Request(scope, "a0802-success");

        var first = await ExecuteAsync(request);
        var before = await CountsAsync(scope, request);
        var replay = await ExecuteAsync(request);
        var after = await CountsAsync(scope, request);

        first.Status.Should().Be(ProcessingJobPersistenceStatus.Success);
        replay.Status.Should().Be(ProcessingJobPersistenceStatus.Replayed);
        after.Should().BeEquivalentTo(before);
        await AssertReceiptAsync(scope, request, first.Status);
    }

    private async Task<ValidationRunPersistenceResult> ExecuteAsync(ValidationRunCreateRequest request)
    {
        await using var context = _fixture.CreateDbContext();
        return await new ProcessingV2PersistenceService(context, new IdempotencyOperationService(context)).CreateValidationAsync(request);
    }

    private async Task AssertReceiptAsync(Scope scope, ValidationRunCreateRequest request, ProcessingJobPersistenceStatus status)
    {
        await using var context = _fixture.CreateDbContext();
        var receipts = await context.IdempotencyRecords.AsNoTracking()
            .Where(value => value.ActorUserId == scope.ActorId && value.ProjectId == scope.ProjectId &&
                            value.Operation == "ValidationRunCreated" && value.IdempotencyKey == request.IdempotencyKey)
            .ToListAsync();
        receipts.Should().ContainSingle();
        receipts[0].OutcomeJson.Should().Contain($"\"Status\":{(int)status}");
        receipts[0].RequestFingerprint.Should().Be(request.RequestFingerprint);
    }

    private async Task<(int Runs, int Outbox)> CountsAsync(Scope scope, ValidationRunCreateRequest request)
    {
        await using var context = _fixture.CreateDbContext();
        var runs = await context.ValidationRuns.CountAsync(value => value.ProjectId == scope.ProjectId);
        var outbox = await context.OutboxMessages.CountAsync(value => value.MessageType == "validation_run.dispatch" && value.PayloadJson.Contains(request.ModelVersionId.ToString()));
        return (runs, outbox);
    }

    private async Task<Scope> SeedAsync(AIModelVersionStatus modelStatus, bool validPair)
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);
        var now = DateTimeOffset.UtcNow;
        var actor = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"a0802_{Guid.NewGuid():N}", DisplayName = "A08-02", PasswordHash = "fixture", RoleCode = UserRoleCode.ProjectManager, Status = UserStatus.Active, CreatedAt = now };
        var project = Project.Create(Guid.NewGuid(), $"A0802-{Guid.NewGuid():N}", "A08-02 validation replay", null, 32648, null, null, now);
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, $"A0802-R-{Guid.NewGuid():N}");
        var roadVersion = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true, new GeometryFactory(new PrecisionModel(), 32648).CreateLineString([new Coordinate(1, 1), new Coordinate(2, 2)]), now, "fixture");
        var survey = Survey.Create(Guid.NewGuid(), null, project.Id, roadVersion.Id, SurveyType.Periodic, SurveyStatus.InProgress, false, null, null);
        var dataset = SurveyDataVersion.Create(Guid.NewGuid(), survey.Id, 1, SurveyDataVersionStatus.ServerConfirmed, SurveyDataIntegrityStatus.Passed, now, SurveyDataConfirmationActor.Backend, "[]");
        var model = AIModelVersion.Create(Guid.NewGuid(), "a0802-model", Guid.NewGuid().ToString("N"), "file:///a0802", null, null, modelStatus, modelStatus == AIModelVersionStatus.Released ? now : null, modelStatus == AIModelVersionStatus.Released ? actor.Id : null);
        context.AddRange(actor, project, road, roadVersion, survey, dataset, model);

        Guid groundTruthId = Guid.NewGuid();
        Guid derivedId = Guid.NewGuid();
        if (validPair)
        {
            var session = FieldInspectionSession.Create(Guid.NewGuid(), FieldInspectionPurpose.ResearchValidation, null, project.Id, roadVersion.Id, null, $"A0802-{Guid.NewGuid():N}", null, "fixture", now, null, "fixture", FieldInspectionSessionStatus.Imported, null);
            var truth = GroundTruthMeasurement.Create(groundTruthId, session.Id, "sample-1", roadVersion.Id, null, null, MeasurementType.DepressionDepth, 10m, "mm", new GeometryFactory(new PrecisionModel(), 4326).CreatePoint(new Coordinate(106, 10)), "gauge", null, "fixture", "fixture", now, null, "fixture");
            var derived = DerivedMeasurement.Create(derivedId, dataset.Id, roadVersion.Id, "sample-1", MeasurementType.DepressionDepth, 11m, "mm", null, DerivedMeasurementSourceType.ManualDerived, "fixture", now, DerivedMeasurementStatus.Published);
            context.AddRange(session, truth, derived);
        }
        await context.SaveChangesAsync();
        return new(actor.Id, project.Id, model.Id, validPair ? groundTruthId : Guid.NewGuid(), validPair ? derivedId : Guid.NewGuid());
    }

    private static ValidationRunCreateRequest Request(Scope scope, string suffix)
        => new(scope.ActorId, scope.ProjectId, scope.ModelId, "split-1", "DEPRESSION_DEPTH", "mm",
            JsonSerializer.Serialize(new[] { new { groundTruthId = scope.GroundTruthId, derivedMeasurementId = scope.DerivedId } }),
            suffix, new string('a', 64), null);

    private sealed record Scope(Guid ActorId, Guid ProjectId, Guid ModelId, Guid GroundTruthId, Guid DerivedId);
}
