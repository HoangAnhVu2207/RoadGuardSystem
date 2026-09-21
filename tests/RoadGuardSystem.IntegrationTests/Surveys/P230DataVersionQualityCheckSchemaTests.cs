using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Devices;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Spatial;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Surveys;

[Trait("TaskId", "P2-30")]
public sealed class P230DataVersionQualityCheckSchemaTests : IClassFixture<IdentitySqlServerFixture>
{
    private static readonly GeometryFactory Srid32648Factory = new(
        new PrecisionModel(),
        SpatialConstants.UtmZone48NSrid);
    private readonly IdentitySqlServerFixture _fixture;

    public P230DataVersionQualityCheckSchemaTests(IdentitySqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "P2-30: dataset version and quality checks round-trip through SQL Server")]
    public async Task SurveyDataVersionAndQualityCheck_ValidTargets_RoundTrip()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var dataVersion = SurveyDataVersion.Create(
            Guid.NewGuid(),
            scope.SurveyId,
            1,
            SurveyDataVersionStatus.Draft,
            SurveyDataIntegrityStatus.Pending,
            null,
            null,
            "[{\"file_id\":\"fixture\",\"checksum\":\"fixture\"}]");
        var fileCheck = QualityCheck.Create(
            Guid.NewGuid(),
            QualityCheckScope.SurveyFile,
            QualityCheckExecutionStage.ClientPrecheck,
            scope.SurveyFileId,
            null,
            QualityCheckType.Format,
            QualityCheckStatus.Passed,
            "{\"mime\":\"video/mp4\"}",
            null,
            "Client format check passed.",
            DateTimeOffset.UtcNow,
            QualityCheckActor.DroneApp,
            scope.OperatorUserId);
        var datasetCheck = QualityCheck.Create(
            Guid.NewGuid(),
            QualityCheckScope.SurveyDataset,
            QualityCheckExecutionStage.ServerValidation,
            null,
            dataVersion.Id,
            QualityCheckType.Completeness,
            QualityCheckStatus.Pending,
            null,
            null,
            null,
            DateTimeOffset.UtcNow,
            QualityCheckActor.Backend,
            null);

        context.AddRange(dataVersion, fileCheck, datasetCheck);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        (await context.SurveyDataVersions.AsNoTracking().SingleAsync(item => item.Id == dataVersion.Id))
            .SourceManifest.Should().Contain("file_id");
        (await context.QualityChecks.AsNoTracking().CountAsync(item => item.SurveyDataVersionId == dataVersion.Id))
            .Should().Be(1);
    }

    [Fact(DisplayName = "P2-30: SQL Server rejects invalid confirmation and QualityCheck target authority")]
    public async Task SurveyDataVersionAndQualityCheck_InvalidState_IsRejectedBySqlServer()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var dataVersion = SurveyDataVersion.Create(
            Guid.NewGuid(),
            scope.SurveyId,
            1,
            SurveyDataVersionStatus.Draft,
            SurveyDataIntegrityStatus.Pending,
            null,
            null,
            "[]");
        context.SurveyDataVersions.Add(dataVersion);
        await context.SaveChangesAsync();

        var invalidConfirmation = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [SurveyDataVersions]
                ([Id], [SurveyId], [VersionNo], [Status], [IntegrityStatus], [SourceManifest])
            VALUES ({Guid.NewGuid()}, {scope.SurveyId}, {2}, {(byte)SurveyDataVersionStatus.ServerConfirmed},
                {(byte)SurveyDataIntegrityStatus.Pending}, {"[]"})
            """);
        var duplicateVersion = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [SurveyDataVersions]
                ([Id], [SurveyId], [VersionNo], [Status], [IntegrityStatus], [SourceManifest])
            VALUES ({Guid.NewGuid()}, {scope.SurveyId}, {1}, {(byte)SurveyDataVersionStatus.Draft},
                {(byte)SurveyDataIntegrityStatus.Pending}, {"[]"})
            """);
        var missingTarget = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [QualityChecks]
                ([Id], [Scope], [ExecutionStage], [CheckType], [Status], [CheckedAt], [CheckedBy])
            VALUES ({Guid.NewGuid()}, {(byte)QualityCheckScope.SurveyFile}, {(byte)QualityCheckExecutionStage.ClientPrecheck},
                {(byte)QualityCheckType.Format}, {(byte)QualityCheckStatus.Passed}, {DateTimeOffset.UtcNow},
                {(byte)QualityCheckActor.DroneApp})
            """);
        var wrongAuthority = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [QualityChecks]
                ([Id], [Scope], [ExecutionStage], [SurveyDataVersionId], [CheckType], [Status], [CheckedAt], [CheckedBy])
            VALUES ({Guid.NewGuid()}, {(byte)QualityCheckScope.SurveyDataset}, {(byte)QualityCheckExecutionStage.ServerValidation},
                {dataVersion.Id}, {(byte)QualityCheckType.Completeness}, {(byte)QualityCheckStatus.Passed},
                {DateTimeOffset.UtcNow}, {(byte)QualityCheckActor.DroneApp})
            """);
        var bothTargets = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [QualityChecks]
                ([Id], [Scope], [ExecutionStage], [SurveyFileId], [SurveyDataVersionId], [CheckType], [Status], [CheckedAt], [CheckedBy])
            VALUES ({Guid.NewGuid()}, {(byte)QualityCheckScope.SurveyFile}, {(byte)QualityCheckExecutionStage.ClientPrecheck},
                {scope.SurveyFileId}, {dataVersion.Id}, {(byte)QualityCheckType.Format}, {(byte)QualityCheckStatus.Passed},
                {DateTimeOffset.UtcNow}, {(byte)QualityCheckActor.DroneApp})
            """);
        var serverInitiator = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [QualityChecks]
                ([Id], [Scope], [ExecutionStage], [SurveyDataVersionId], [CheckType], [Status], [CheckedAt], [CheckedBy], [InitiatedByUserId])
            VALUES ({Guid.NewGuid()}, {(byte)QualityCheckScope.SurveyDataset}, {(byte)QualityCheckExecutionStage.ServerValidation},
                {dataVersion.Id}, {(byte)QualityCheckType.Completeness}, {(byte)QualityCheckStatus.Passed},
                {DateTimeOffset.UtcNow}, {(byte)QualityCheckActor.Backend}, {scope.OperatorUserId})
            """);

        await invalidConfirmation.Should().ThrowAsync<SqlException>();
        await duplicateVersion.Should().ThrowAsync<SqlException>();
        await missingTarget.Should().ThrowAsync<SqlException>();
        await wrongAuthority.Should().ThrowAsync<SqlException>();
        await bothTargets.Should().ThrowAsync<SqlException>();
        await serverInitiator.Should().ThrowAsync<SqlException>();
    }

    [Fact(DisplayName = "P2-30: SQL Server prevents a confirmed dataset manifest from being overwritten")]
    public async Task SurveyDataVersion_ConfirmedManifestUpdate_IsRejectedBySqlServer()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var confirmed = SurveyDataVersion.Create(
            Guid.NewGuid(),
            scope.SurveyId,
            1,
            SurveyDataVersionStatus.ServerConfirmed,
            SurveyDataIntegrityStatus.Passed,
            DateTimeOffset.UtcNow,
            SurveyDataConfirmationActor.Backend,
            "[]");
        context.SurveyDataVersions.Add(confirmed);
        await context.SaveChangesAsync();

        var overwrite = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE [SurveyDataVersions] SET [SourceManifest] = {"[{\"unexpected\":true}]"}
            WHERE [Id] = {confirmed.Id}
            """);

        await overwrite.Should().ThrowAsync<SqlException>();

        var rollback = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE [SurveyDataVersions] SET [Status] = {(byte)SurveyDataVersionStatus.Draft}, [IntegrityStatus] = {(byte)SurveyDataIntegrityStatus.Pending},
                [ConfirmedAt] = NULL, [ConfirmedBy] = NULL WHERE [Id] = {confirmed.Id}
            """);

        await rollback.Should().ThrowAsync<SqlException>();
    }

    [Fact(DisplayName = "P2-30: dataset and quality schema migration downgrades and reapplies")]
    public async Task MigrationLifecycle_DowngradesToFlightSurveyFileAndReapplies()
    {
        await using var context = _fixture.CreateDbContext();
        (await CountP230TablesAsync(context)).Should().Be(2);

        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("20260921125553_AddP230FlightSurveyFileSchema");
        (await CountP230TablesAsync(context)).Should().Be(0);

        await context.Database.MigrateAsync();
        (await CountP230TablesAsync(context)).Should().Be(2);
    }

    private async Task<QualityCheckScopeFixture> CreateScopeAsync(RoadGuardDbContext context)
    {
        await _fixture.SeedRolesAsync(context);
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = $"P230-QC-{Guid.NewGuid():N}",
            Name = "P2-30 quality fixture project",
            EngineeringUtmSrid = SpatialConstants.UtmZone48NSrid,
            Status = ProjectStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var operatorUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"p230_quality_operator_{Guid.NewGuid():N}",
            DisplayName = "P2-30 quality operator",
            PasswordHash = "fixture-password-hash",
            RoleCode = UserRoleCode.DroneOperator,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var roadSection = RoadSection.Create(Guid.NewGuid(), project.Id, $"P230-QC-ROAD-{Guid.NewGuid():N}");
        var roadSectionVersion = RoadSectionVersion.Create(
            Guid.NewGuid(), roadSection.Id, 1, true,
            Srid32648Factory.CreateLineString(new[]
            {
                new Coordinate(588500, 2325000),
                new Coordinate(588600, 2325100)
            }),
            DateTimeOffset.UtcNow,
            "P2-30 quality fixture version");
        var survey = Survey.Create(
            Guid.NewGuid(), null, project.Id, roadSectionVersion.Id, SurveyType.Periodic,
            SurveyStatus.InProgress, false, null, null);
        var device = DroneDevice.Create(Guid.NewGuid(), $"P230-QC-{Guid.NewGuid():N}", DroneDeviceStatus.Active);
        var checksum = new string('c', 64);
        var storedFile = StoredFile.Create(
            Guid.NewGuid(), $"objects/p230-quality/{Guid.NewGuid():N}", "quality.mp4", "video/mp4", 1024,
            checksum, operatorUser.Id, DateTimeOffset.UtcNow, null);
        var flight = Flight.Create(
            Guid.NewGuid(), survey.Id, device.Id, operatorUser.Id, DateTimeOffset.UtcNow.AddMinutes(-2), null, "P230-QC-1");
        var surveyFile = SurveyFile.Create(
            Guid.NewGuid(), survey.Id, flight.Id, storedFile.Id, SurveyFileType.Video, null, null,
            SurveyFileSyncStatus.Queued, checksum);
        context.AddRange(project, operatorUser, roadSection, roadSectionVersion, survey, device, storedFile, flight, surveyFile);
        await context.SaveChangesAsync();
        return new QualityCheckScopeFixture(survey.Id, surveyFile.Id, operatorUser.Id);
    }

    private static Task<int> CountP230TablesAsync(RoadGuardDbContext context)
        => context.Database.SqlQueryRaw<int>(
                """
                SELECT CAST(COUNT(*) AS int) AS [Value]
                FROM sys.tables
                WHERE [name] IN ('SurveyDataVersions', 'QualityChecks')
                """)
            .SingleAsync();

    private sealed record QualityCheckScopeFixture(Guid SurveyId, Guid SurveyFileId, Guid OperatorUserId);
}
