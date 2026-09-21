using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Devices;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Surveys;
using RoadGuardSystem.Repositories.Spatial;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Surveys;

[Trait("TaskId", "P2-30")]
public sealed class P230SurveyDataValidationAdmissionTests : IClassFixture<IdentitySqlServerFixture>
{
    private static readonly GeometryFactory Srid32648Factory = new(new PrecisionModel(), SpatialConstants.UtmZone48NSrid);
    private readonly IdentitySqlServerFixture _fixture;

    public P230SurveyDataValidationAdmissionTests(IdentitySqlServerFixture fixture) => _fixture = fixture;

    [Fact(DisplayName = "P2-30: validation admission atomically persists dataset audit outbox and replay")]
    public async Task AdmitAsync_ValidRequest_CommitsOneDurableOutcome()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var version = CreateVersion(scope.SurveyId, 1);
        var service = new SurveyDataValidationAdmissionPersistenceService(context, new IdempotencyOperationService(context));
        var request = new SurveyDataValidationAdmissionRequest(
            version, [scope.SurveyFileId], scope.OperatorUserId, "p230-admit-key", new string('a', 64), Guid.NewGuid());

        var first = await service.AdmitAsync(request);
        var replay = await service.AdmitAsync(request);
        context.ChangeTracker.Clear();

        first.Status.Should().Be(IdempotencyOperationStatus.Executed);
        replay.Status.Should().Be(IdempotencyOperationStatus.Replayed);
        replay.OutboxMessageId.Should().Be(first.OutboxMessageId);
        (await context.SurveyDataVersions.CountAsync(item => item.Id == version.Id)).Should().Be(1);
        (await context.AuditLogs.CountAsync(item => item.EntityId == version.Id && item.EventType == "survey_data_validation.admitted")).Should().Be(1);
        (await context.OutboxMessages.CountAsync(item => item.Id == first.OutboxMessageId)).Should().Be(1);
    }

    [Fact(DisplayName = "P2-30: validation admission rejects a SurveyFile outside the dataset Survey")]
    public async Task AdmitAsync_FileOutsideSurvey_LeavesNoDatasetAuditOrOutbox()
    {
        await using var context = _fixture.CreateDbContext();
        var firstScope = await CreateScopeAsync(context);
        var secondScope = await CreateScopeAsync(context);
        var version = CreateVersion(firstScope.SurveyId, 1);
        var service = new SurveyDataValidationAdmissionPersistenceService(context, new IdempotencyOperationService(context));
        var request = new SurveyDataValidationAdmissionRequest(
            version, [secondScope.SurveyFileId], firstScope.OperatorUserId, "p230-wrong-scope", new string('b', 64), Guid.NewGuid());

        await service.Invoking(item => item.AdmitAsync(request)).Should().ThrowAsync<InvalidOperationException>();
        context.ChangeTracker.Clear();

        (await context.SurveyDataVersions.CountAsync(item => item.Id == version.Id)).Should().Be(0);
        (await context.AuditLogs.CountAsync(item => item.EntityId == version.Id)).Should().Be(0);
        (await context.OutboxMessages.CountAsync(item => item.CorrelationId == version.Id)).Should().Be(0);
    }

    [Fact(DisplayName = "P2-30: validation admission changed-fingerprint replay returns conflict without another effect")]
    public async Task AdmitAsync_ChangedFingerprint_ReturnsConflictWithoutDuplicateEffect()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var version = CreateVersion(scope.SurveyId, 1);
        var service = new SurveyDataValidationAdmissionPersistenceService(context, new IdempotencyOperationService(context));
        var first = new SurveyDataValidationAdmissionRequest(version, [scope.SurveyFileId], scope.OperatorUserId, "p230-conflict-key", new string('c', 64), Guid.NewGuid());
        var changed = first with { RequestFingerprint = new string('d', 64) };

        var executed = await service.AdmitAsync(first);
        var conflict = await service.AdmitAsync(changed);
        context.ChangeTracker.Clear();

        executed.Status.Should().Be(IdempotencyOperationStatus.Executed);
        conflict.Status.Should().Be(IdempotencyOperationStatus.Conflict);
        conflict.OutboxMessageId.Should().Be(executed.OutboxMessageId);
        (await context.SurveyDataVersions.CountAsync(item => item.Id == version.Id)).Should().Be(1);
        (await context.OutboxMessages.CountAsync(item => item.CorrelationId == version.Id)).Should().Be(1);
    }

    [Fact(DisplayName = "P2-30: validation admission persists a canonical manifest from selected SurveyFiles")]
    public async Task AdmitAsync_CanonicalizesManifestFromSelectedFiles()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var version = CreateVersion(scope.SurveyId, 1);
        var service = new SurveyDataValidationAdmissionPersistenceService(context, new IdempotencyOperationService(context));
        var request = new SurveyDataValidationAdmissionRequest(version, [scope.SurveyFileId], scope.OperatorUserId, "p230-manifest-key", new string('e', 64), Guid.NewGuid());

        await service.AdmitAsync(request);
        context.ChangeTracker.Clear();

        var persisted = await context.SurveyDataVersions.AsNoTracking().SingleAsync(item => item.Id == version.Id);
        persisted.SourceManifest.Should().Contain(scope.SurveyFileId.ToString());
        persisted.SourceManifest.Should().Contain(new string('d', 64));
    }

    private async Task<AdmissionScope> CreateScopeAsync(RoadGuardDbContext context)
    {
        await _fixture.SeedRolesAsync(context);
        var project = new Project { Id = Guid.NewGuid(), ProjectCode = $"P230-ADM-{Guid.NewGuid():N}", Name = "P2-30 admission fixture", EngineeringUtmSrid = SpatialConstants.UtmZone48NSrid, Status = ProjectStatus.Active, CreatedAt = DateTimeOffset.UtcNow };
        var operatorUser = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"p230_adm_{Guid.NewGuid():N}", DisplayName = "P2-30 operator", PasswordHash = "fixture-password-hash", RoleCode = UserRoleCode.DroneOperator, Status = UserStatus.Active, CreatedAt = DateTimeOffset.UtcNow };
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, $"P230-ADM-ROAD-{Guid.NewGuid():N}");
        var roadVersion = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true, Srid32648Factory.CreateLineString(new[] { new Coordinate(588500, 2325000), new Coordinate(588600, 2325100) }), DateTimeOffset.UtcNow, "P2-30 admission fixture version");
        var survey = Survey.Create(Guid.NewGuid(), null, project.Id, roadVersion.Id, SurveyType.Periodic, SurveyStatus.InProgress, false, null, null);
        var device = DroneDevice.Create(Guid.NewGuid(), $"P230-ADM-{Guid.NewGuid():N}", DroneDeviceStatus.Active);
        var checksum = new string('d', 64);
        var file = StoredFile.Create(Guid.NewGuid(), $"objects/p230-admission/{Guid.NewGuid():N}", "admission.mp4", "video/mp4", 1024, checksum, operatorUser.Id, DateTimeOffset.UtcNow, null);
        var flight = Flight.Create(Guid.NewGuid(), survey.Id, device.Id, operatorUser.Id, DateTimeOffset.UtcNow.AddMinutes(-1), null, "P230-ADM-1");
        var surveyFile = SurveyFile.Create(Guid.NewGuid(), survey.Id, flight.Id, file.Id, SurveyFileType.Video, null, null, SurveyFileSyncStatus.Queued, checksum);
        context.AddRange(project, operatorUser, road, roadVersion, survey, device, file, flight, surveyFile);
        await context.SaveChangesAsync();
        return new AdmissionScope(survey.Id, surveyFile.Id, operatorUser.Id);
    }

    private static SurveyDataVersion CreateVersion(Guid surveyId, int versionNo)
        => SurveyDataVersion.Create(Guid.NewGuid(), surveyId, versionNo, SurveyDataVersionStatus.Draft, SurveyDataIntegrityStatus.Pending, null, null, "[]");

    private sealed record AdmissionScope(Guid SurveyId, Guid SurveyFileId, Guid OperatorUserId);
}
