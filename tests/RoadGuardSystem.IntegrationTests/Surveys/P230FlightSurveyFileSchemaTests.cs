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
public sealed class P230FlightSurveyFileSchemaTests : IClassFixture<IdentitySqlServerFixture>
{
    private static readonly GeometryFactory Srid32648Factory = new(
        new PrecisionModel(),
        SpatialConstants.UtmZone48NSrid);
    private readonly IdentitySqlServerFixture _fixture;

    public P230FlightSurveyFileSchemaTests(IdentitySqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "P2-30: flight and survey file round-trip through SQL Server")]
    public async Task FlightAndSurveyFile_ValidScope_RoundTrip()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var flight = Flight.Create(
            Guid.NewGuid(),
            scope.SurveyId,
            scope.DroneDeviceId,
            scope.OperatorUserId,
            DateTimeOffset.UtcNow.AddMinutes(-10),
            DateTimeOffset.UtcNow.AddMinutes(-1),
            "P230-FLIGHT-001");
        var surveyFile = SurveyFile.Create(
            Guid.NewGuid(),
            scope.SurveyId,
            flight.Id,
            scope.FileId,
            SurveyFileType.Video,
            DateTimeOffset.UtcNow.AddMinutes(-9),
            DateTimeOffset.UtcNow.AddMinutes(-2),
            SurveyFileSyncStatus.Queued,
            scope.Checksum);

        context.AddRange(flight, surveyFile);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        (await context.Flights.AsNoTracking().SingleAsync(item => item.Id == flight.Id))
            .FlightNo.Should().Be("P230-FLIGHT-001");
        (await context.SurveyFiles.AsNoTracking().SingleAsync(item => item.Id == surveyFile.Id))
            .Checksum.Should().Be(scope.Checksum);
    }

    [Fact(DisplayName = "P2-30: flight and survey file factories reject invalid temporal and checksum values")]
    public void FlightAndSurveyFile_InvalidValues_AreRejectedBeforePersistence()
    {
        var start = DateTimeOffset.UtcNow;

        var invalidFlight = () => Flight.Create(
            Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), start, start.AddMinutes(-1), "P230-INVALID");
        var invalidSurveyFile = () => SurveyFile.Create(
            Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), SurveyFileType.Video,
            start, start.AddMinutes(-1), SurveyFileSyncStatus.Queued, "invalid-checksum");

        invalidFlight.Should().Throw<ArgumentException>();
        invalidSurveyFile.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "P2-30: SQL Server rejects duplicate flight number and mismatched survey file scope")]
    public async Task FlightAndSurveyFile_DuplicateOrMismatchedScope_IsRejectedBySqlServer()
    {
        await using var context = _fixture.CreateDbContext();
        var first = await CreateScopeAsync(context);
        var second = await CreateScopeAsync(context);
        var startedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var firstFlight = Flight.Create(
            Guid.NewGuid(),
            first.SurveyId, first.DroneDeviceId, first.OperatorUserId, startedAt, null, "P230-DUPLICATE");
        context.Flights.Add(firstFlight);
        await context.SaveChangesAsync();

        var duplicateFlight = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [Flights]
                ([Id], [SurveyId], [DroneDeviceId], [OperatorUserId], [StartedAt], [FlightNo])
            VALUES ({Guid.NewGuid()}, {first.SurveyId}, {first.DroneDeviceId}, {first.OperatorUserId}, {startedAt}, {"P230-DUPLICATE"})
            """);
        var mismatchedFlight = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [SurveyFiles]
                ([Id], [SurveyId], [FlightId], [FileId], [FileType], [SyncStatus], [Checksum])
            VALUES ({Guid.NewGuid()}, {second.SurveyId}, {firstFlight.Id}, {second.FileId}, {(byte)SurveyFileType.Video},
                {(byte)SurveyFileSyncStatus.Queued}, {second.Checksum})
            """);
        var mismatchedChecksum = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [SurveyFiles]
                ([Id], [SurveyId], [FileId], [FileType], [SyncStatus], [Checksum])
            VALUES ({Guid.NewGuid()}, {first.SurveyId}, {first.FileId}, {(byte)SurveyFileType.Video},
                {(byte)SurveyFileSyncStatus.Queued}, {new string('a', 64)})
            """);

        await duplicateFlight.Should().ThrowAsync<SqlException>();
        await mismatchedFlight.Should().ThrowAsync<SqlException>();
        await mismatchedChecksum.Should().ThrowAsync<SqlException>();
    }

    [Fact(DisplayName = "P2-30: SQL Server prevents a Flight Survey change after it is referenced by SurveyFile")]
    public async Task Flight_SurveyIdentityUpdate_IsRejectedBySqlServer()
    {
        await using var context = _fixture.CreateDbContext();
        var first = await CreateScopeAsync(context);
        var second = await CreateScopeAsync(context);
        var flight = Flight.Create(Guid.NewGuid(), first.SurveyId, first.DroneDeviceId, first.OperatorUserId, DateTimeOffset.UtcNow, null, "P230-IMMUTABLE");
        var surveyFile = SurveyFile.Create(Guid.NewGuid(), first.SurveyId, flight.Id, first.FileId, SurveyFileType.Video, null, null, SurveyFileSyncStatus.Queued, first.Checksum);
        context.AddRange(flight, surveyFile);
        await context.SaveChangesAsync();

        var moveFlight = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE [Flights] SET [SurveyId] = {second.SurveyId} WHERE [Id] = {flight.Id}
            """);

        await moveFlight.Should().ThrowAsync<SqlException>();
    }

    [Fact(DisplayName = "P2-30: flight and survey file migration downgrades and reapplies")]
    public async Task MigrationLifecycle_DowngradesToP223AndReapplies()
    {
        await using var context = _fixture.CreateDbContext();
        (await CountP230TablesAsync(context)).Should().Be(2);

        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("20260920182623_AddSurveyAssignmentSchema");
        (await CountP230TablesAsync(context)).Should().Be(0);

        await context.Database.MigrateAsync();
        (await CountP230TablesAsync(context)).Should().Be(2);
    }

    private async Task<FlightSurveyFileScope> CreateScopeAsync(RoadGuardDbContext context)
    {
        await _fixture.SeedRolesAsync(context);
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = $"P230-{Guid.NewGuid():N}",
            Name = "P2-30 fixture project",
            EngineeringUtmSrid = SpatialConstants.UtmZone48NSrid,
            Status = ProjectStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var operatorUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"p230_operator_{Guid.NewGuid():N}",
            DisplayName = "P2-30 operator",
            PasswordHash = "fixture-password-hash",
            RoleCode = UserRoleCode.DroneOperator,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var roadSection = RoadSection.Create(Guid.NewGuid(), project.Id, $"P230-ROAD-{Guid.NewGuid():N}");
        var roadSectionVersion = RoadSectionVersion.Create(
            Guid.NewGuid(),
            roadSection.Id,
            1,
            true,
            Srid32648Factory.CreateLineString(new[]
            {
                new Coordinate(588500, 2325000),
                new Coordinate(588600, 2325100)
            }),
            DateTimeOffset.UtcNow,
            "P2-30 fixture version");
        var survey = Survey.Create(
            Guid.NewGuid(),
            null,
            project.Id,
            roadSectionVersion.Id,
            SurveyType.Periodic,
            SurveyStatus.InProgress,
            false,
            null,
            null);
        var device = DroneDevice.Create(Guid.NewGuid(), $"P230-{Guid.NewGuid():N}", DroneDeviceStatus.Active);
        var checksum = new string('b', 64);
        var file = StoredFile.Create(
            Guid.NewGuid(),
            $"objects/p230/{Guid.NewGuid():N}",
            "flight.mp4",
            "video/mp4",
            1024,
            checksum,
            operatorUser.Id,
            DateTimeOffset.UtcNow,
            null);
        context.AddRange(project, operatorUser, roadSection, roadSectionVersion, survey, device, file);
        await context.SaveChangesAsync();
        return new FlightSurveyFileScope(survey.Id, device.Id, operatorUser.Id, file.Id, checksum);
    }

    private static Task<int> CountP230TablesAsync(RoadGuardDbContext context)
        => context.Database.SqlQueryRaw<int>(
                """
                SELECT CAST(COUNT(*) AS int) AS [Value]
                FROM sys.tables
                WHERE [name] IN ('Flights', 'SurveyFiles')
                """)
            .SingleAsync();

    private sealed record FlightSurveyFileScope(
        Guid SurveyId,
        Guid DroneDeviceId,
        Guid OperatorUserId,
        Guid FileId,
        string Checksum);
}
