using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;

namespace RoadGuardSystem.IntegrationTests.Infrastructure;

public sealed record ProjectMembershipFixtureData(
    Guid PrimaryProjectId,
    Guid SecondaryProjectId,
    Guid ProjectManagerId,
    Guid DroneOperatorId,
    Guid RepairCrewId,
    Guid ExpiredDroneOperatorId);

public static class ProjectMembershipSqlFixture
{
    public static async Task<ProjectMembershipFixtureData> CreateAsync(IdentitySqlServerFixture fixture)
    {
        await using var context = fixture.CreateDbContext();
        await fixture.SeedRolesAsync(context);
        var primaryProjectId = Guid.NewGuid();
        var secondaryProjectId = Guid.NewGuid();
        var projectManagerId = Guid.NewGuid();
        var droneOperatorId = Guid.NewGuid();
        var repairCrewId = Guid.NewGuid();
        var expiredDroneOperatorId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        context.Users.AddRange(
            User(projectManagerId, UserRoleCode.ProjectManager),
            User(droneOperatorId, UserRoleCode.DroneOperator),
            User(repairCrewId, UserRoleCode.RepairCrew),
            User(expiredDroneOperatorId, UserRoleCode.DroneOperator));
        context.Projects.AddRange(
            Project(primaryProjectId, "P220-PRIMARY", now),
            Project(secondaryProjectId, "P220-SECONDARY", now));
        context.ProjectMembers.AddRange(
            Member(primaryProjectId, projectManagerId, UserRoleCode.ProjectManager, true, ProjectMemberStatus.Active, new DateOnly(2026, 1, 1), null),
            Member(primaryProjectId, droneOperatorId, UserRoleCode.DroneOperator, false, ProjectMemberStatus.Active, new DateOnly(2026, 1, 1), null),
            Member(primaryProjectId, repairCrewId, UserRoleCode.RepairCrew, false, ProjectMemberStatus.Active, new DateOnly(2026, 1, 1), null),
            Member(secondaryProjectId, expiredDroneOperatorId, UserRoleCode.DroneOperator, false, ProjectMemberStatus.Ended, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)));
        await context.SaveChangesAsync();

        return new ProjectMembershipFixtureData(primaryProjectId, secondaryProjectId, projectManagerId, droneOperatorId, repairCrewId, expiredDroneOperatorId);
    }

    private static ApplicationUser User(Guid id, UserRoleCode role) => new()
    {
        Id = id, UserName = $"p220_fixture_{id:N}", DisplayName = "P2-20 fixture user",
        PasswordHash = "fixture-password-hash", RoleCode = role, Status = UserStatus.Active, CreatedAt = DateTimeOffset.UtcNow
    };

    private static Project Project(Guid id, string code, DateTimeOffset createdAt) => new()
    {
        Id = id, ProjectCode = $"{code}-{id:N}", Name = code, Status = ProjectStatus.Active, CreatedAt = createdAt
    };

    private static ProjectMember Member(Guid projectId, Guid userId, UserRoleCode role, bool primary, ProjectMemberStatus status, DateOnly validFrom, DateOnly? validTo) => new()
    {
        Id = Guid.NewGuid(), ProjectId = projectId, UserId = userId, RoleCode = role, IsPrimary = primary,
        Status = status, ValidFrom = validFrom, ValidTo = validTo
    };
}
