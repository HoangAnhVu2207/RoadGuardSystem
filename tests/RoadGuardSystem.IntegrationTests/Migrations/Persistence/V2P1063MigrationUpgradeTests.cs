using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace RoadGuardSystem.IntegrationTests.Persistence;

[Trait("TaskId", "V2-P1-063")]
public sealed class V2P1063MigrationUpgradeTests
{
    private readonly ITestOutputHelper _output;

    public V2P1063MigrationUpgradeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(DisplayName = "V2-P1-063 upgrades baseline data without recreating unrelated tables")]
    public async Task Upgrade_FromRoadSegmentBaseline_PreservesTaskOnboardingAndRoadSegments()
    {
        var fixture = new SqlServerTestFixture(createSpatialProbeSchema: false);
        await fixture.InitializeAsync();
        try
        {
            await using var context = CreateContext(fixture.ConnectionString);
            var migrator = context.GetService<IMigrator>();

            await migrator.MigrateAsync();

            var ids = await InsertBaselineRowsAsync(fixture.ConnectionString);
            _output.WriteLine($"baseline fixture ids: crew={ids.CrewUserId}; project={ids.ProjectId}; roadSection={ids.RoadSectionId}; roadSectionVersion={ids.RoadSectionVersionId}; defect={ids.DefectId}; survey={ids.SurveyId}; task={ids.TaskId}; assignment={ids.AssignmentId}; registrationIntent={ids.RegistrationIntentId}; invitation={ids.InvitationId}; segmentSet={ids.SegmentSetId}; segment={ids.SegmentId}");
            (await ScalarAsync<int>(fixture.ConnectionString,
                "SELECT CAST(COL_LENGTH('dbo.FieldInspectionTasks', 'RowVersion') AS int)"))
                .Should().Be(8);
            (await ScalarAsync<int>(fixture.ConnectionString,
                "SELECT COUNT(*) FROM FieldInspectionTasks WHERE Id = @id",
                ("@id", ids.TaskId))).Should().Be(1);
            (await ScalarAsync<int>(fixture.ConnectionString,
                "SELECT COUNT(*) FROM ReporterRegistrationIntents WHERE Id = @id",
                ("@id", ids.RegistrationIntentId))).Should().Be(1);
            (await ScalarAsync<int>(fixture.ConnectionString,
                "SELECT COUNT(*) FROM StaffInvitations WHERE Id = @id",
                ("@id", ids.InvitationId))).Should().Be(1);
            (await ScalarAsync<int>(fixture.ConnectionString,
                "SELECT COUNT(*) FROM StaffInvitationProjects WHERE InvitationId = @id AND ProjectId = @projectId",
                ("@id", ids.InvitationId), ("@projectId", ids.ProjectId))).Should().Be(1);
            (await ScalarAsync<int>(fixture.ConnectionString,
                "SELECT COUNT(*) FROM RoadSegmentSets WHERE Id = @id",
                ("@id", ids.SegmentSetId))).Should().Be(1);
            (await ScalarAsync<int>(fixture.ConnectionString,
                "SELECT COUNT(*) FROM RoadSegments WHERE Id = @id AND SegmentSetId = @segmentSetId",
                ("@id", ids.SegmentId), ("@segmentSetId", ids.SegmentSetId))).Should().Be(1);

            var originalTaskVersion = await ScalarBytesAsync(fixture.ConnectionString,
                "SELECT RowVersion FROM FieldInspectionTasks WHERE Id = @id", ("@id", ids.TaskId));
            originalTaskVersion.Should().NotBeNullOrEmpty();

            var repository = new InspectionTaskReadRepository(context);
            var projection = await repository.ListAssignedAsync(ids.CrewUserId, null, null, 10);
            projection.Items.Should().ContainSingle(item =>
                item.Id == ids.TaskId && item.RowVersion.SequenceEqual(originalTaskVersion!));

            var updatedTaskVersion = await ScalarBytesAsync(fixture.ConnectionString,
                "DECLARE @versions TABLE (RowVersion binary(8)); UPDATE FieldInspectionTasks SET Instructions = @instructions OUTPUT INSERTED.RowVersion INTO @versions WHERE Id = @id; SELECT RowVersion FROM @versions;",
                ("@instructions", "updated during V2-P1-063 upgrade"), ("@id", ids.TaskId));
            updatedTaskVersion.Should().NotBeEquivalentTo(originalTaskVersion);

            var stableTaskVersion = await ScalarBytesAsync(fixture.ConnectionString,
                "SELECT RowVersion FROM FieldInspectionTasks WHERE Id = @id", ("@id", ids.TaskId));
            await ExecuteAsync(fixture.ConnectionString,
                "UPDATE Projects SET Name = @name WHERE Id = @id",
                ("@name", "upgrade project renamed"), ("@id", ids.ProjectId));
            var afterProjectVersion = await ScalarBytesAsync(fixture.ConnectionString,
                "SELECT RowVersion FROM FieldInspectionTasks WHERE Id = @id", ("@id", ids.TaskId));
            afterProjectVersion.Should().Equal(stableTaskVersion);

            var upgradedProjection = await new InspectionTaskReadRepository(context)
                .ListAssignedAsync(ids.CrewUserId, null, null, 10);
            upgradedProjection.Items.Single(item => item.Id == ids.TaskId).RowVersion
                .Should().Equal(updatedTaskVersion);
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    private static RoadGuardDbContext CreateContext(string connectionString)
        => new(new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(connectionString, sql => sql.UseNetTopologySuite())
            .Options);

    private static async Task<FixtureIds> InsertBaselineRowsAsync(string connectionString)
    {
        var ids = new FixtureIds(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await ExecuteAsync(connectionString, "INSERT INTO Roles (Code, Name, NormalizedName, IsActive) VALUES (@code, @name, @normalized, 1)",
            ("@code", "REPAIR_CREW"), ("@name", "Repair Crew"), ("@normalized", "REPAIR_CREW"));
        await ExecuteAsync(connectionString, "INSERT INTO Users (Id, DisplayName, RoleCode, Status, MustChangePassword, CreatedAt, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount) VALUES (@id, @displayName, 'REPAIR_CREW', 1, 0, @createdAt, @userName, @normalizedUserName, @email, @normalizedEmail, 1, 'baseline-hash', @securityStamp, 0, 0, 1, 0)",
            ("@id", ids.CrewUserId), ("@displayName", "Baseline Crew"), ("@createdAt", DateTimeOffset.UtcNow),
            ("@userName", "baseline-crew"), ("@normalizedUserName", "BASELINE-CREW"), ("@email", "baseline-crew@example.test"), ("@normalizedEmail", "BASELINE-CREW@EXAMPLE.TEST"), ("@securityStamp", Guid.NewGuid().ToString("N")));
        await ExecuteAsync(connectionString, "INSERT INTO Projects (Id, ProjectCode, Name, Status, CreatedAt) VALUES (@id, 'V2P1063', 'baseline project', 1, @createdAt)",
            ("@id", ids.ProjectId), ("@createdAt", DateTimeOffset.UtcNow));
        await ExecuteAsync(connectionString, "INSERT INTO RoadSections (Id, ProjectId, Code, Name) VALUES (@id, @projectId, 'BASE-SEC', 'baseline section')",
            ("@id", ids.RoadSectionId), ("@projectId", ids.ProjectId));
        await ExecuteAsync(connectionString, "INSERT INTO RoadSectionVersions (Id, RoadSectionId, VersionNo, IsCurrent, Geometry, EffectiveFrom, ChangeReason) VALUES (@id, @roadSectionId, 1, 1, geometry::STGeomFromText('LINESTRING (0 0, 1 1)', 32648), @effectiveFrom, 'baseline')",
            ("@id", ids.RoadSectionVersionId), ("@roadSectionId", ids.RoadSectionId), ("@effectiveFrom", DateTimeOffset.UtcNow));
        await ExecuteAsync(connectionString, "INSERT INTO DefectTypes (Code, Name, IsActive) VALUES ('BASE-DEFECT', 'Baseline defect', 1)");
        await ExecuteAsync(connectionString, "INSERT INTO Defects (Id, DefectTypeCode, Severity, Status) VALUES (@id, 'BASE-DEFECT', 1, 1)", ("@id", ids.DefectId));
        await ExecuteAsync(connectionString, "INSERT INTO Surveys (Id, ProjectId, RoadSectionVersionId, SurveyType, IsBaselineConfirmed, Status) VALUES (@id, @projectId, @roadSectionVersionId, 1, 0, 1)",
            ("@id", ids.SurveyId), ("@projectId", ids.ProjectId), ("@roadSectionVersionId", ids.RoadSectionVersionId));
        await ExecuteAsync(connectionString, "INSERT INTO FieldInspectionTasks (Id, TaskCode, ProjectId, DefectId, SurveyId, RoadSectionVersionId, RequiredMeasurementType, MeasurementScope, Instructions, DueAt, Status, AssignedByUserId) VALUES (@id, 'V2P1063-BASE', @projectId, @defectId, @surveyId, @roadSectionVersionId, 1, '{\"kind\":\"baseline\"}', 'before upgrade', @dueAt, 1, @assignedByUserId)",
            ("@id", ids.TaskId), ("@projectId", ids.ProjectId), ("@defectId", ids.DefectId), ("@surveyId", ids.SurveyId), ("@roadSectionVersionId", ids.RoadSectionVersionId), ("@dueAt", DateTimeOffset.UtcNow.AddDays(1)), ("@assignedByUserId", ids.CrewUserId));
        await ExecuteAsync(connectionString, "INSERT INTO FieldInspectionAssignments (Id, FieldInspectionTaskId, AssignedToUserId, AssignedByUserId, AssignedAt, Status) VALUES (@id, @taskId, @assignedToUserId, @assignedByUserId, @assignedAt, 1)",
            ("@id", ids.AssignmentId), ("@taskId", ids.TaskId), ("@assignedToUserId", ids.CrewUserId), ("@assignedByUserId", ids.CrewUserId), ("@assignedAt", DateTimeOffset.UtcNow));
        await ExecuteAsync(connectionString, "INSERT INTO ReporterRegistrationIntents (Id, NormalizedEmail, ReporterType, OtpHash, OtpGeneration, FailedAttempts, ExpiresAt, ResendAvailableAt, CreatedAt) VALUES (@id, 'BASELINE@EXAMPLE.TEST', 1, REPLICATE('0', 64), 1, 0, @expiresAt, @resendAt, @createdAt)",
            ("@id", ids.RegistrationIntentId), ("@expiresAt", DateTimeOffset.UtcNow.AddHours(1)), ("@resendAt", DateTimeOffset.UtcNow), ("@createdAt", DateTimeOffset.UtcNow));
        await ExecuteAsync(connectionString, "INSERT INTO StaffInvitations (Id, DisplayName, Email, NormalizedEmail, RoleCode, TokenHash, CreatedByUserId, CreatedAt, ExpiresAt) VALUES (@id, 'Baseline Invite', 'invite@example.test', 'INVITE@EXAMPLE.TEST', 'REPAIR_CREW', REPLICATE('1', 64), @createdByUserId, @createdAt, @expiresAt)",
            ("@id", ids.InvitationId), ("@createdByUserId", ids.CrewUserId), ("@createdAt", DateTimeOffset.UtcNow), ("@expiresAt", DateTimeOffset.UtcNow.AddDays(1)));
        await ExecuteAsync(connectionString, "INSERT INTO StaffInvitationProjects (InvitationId, ProjectId) VALUES (@invitationId, @projectId)", ("@invitationId", ids.InvitationId), ("@projectId", ids.ProjectId));
        await ExecuteAsync(connectionString, "INSERT INTO RoadSegmentSets (Id, RoadSectionVersionId, Status) VALUES (@id, @roadSectionVersionId, 'PUBLISHED')", ("@id", ids.SegmentSetId), ("@roadSectionVersionId", ids.RoadSectionVersionId));
        await ExecuteAsync(connectionString, "INSERT INTO RoadSegments (Id, SegmentSetId, RoadSectionVersionId, Sequence) VALUES (@id, @segmentSetId, @roadSectionVersionId, 1)", ("@id", ids.SegmentId), ("@segmentSetId", ids.SegmentSetId), ("@roadSectionVersionId", ids.RoadSectionVersionId));
        return ids;
    }

    private static async Task ExecuteAsync(string connectionString, string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<T> ScalarAsync<T>(string connectionString, string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        return (T)(await command.ExecuteScalarAsync() ?? throw new InvalidOperationException("Expected scalar result."));
    }

    private static async Task<byte[]?> ScalarBytesAsync(string connectionString, string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        return await command.ExecuteScalarAsync() as byte[];
    }

    private sealed record FixtureIds(Guid CrewUserId, Guid ProjectId, Guid RoadSectionId, Guid RoadSectionVersionId, Guid DefectId, Guid SurveyId, Guid TaskId, Guid AssignmentId, Guid RegistrationIntentId, Guid InvitationId, Guid SegmentSetId, Guid SegmentId = default);
}
