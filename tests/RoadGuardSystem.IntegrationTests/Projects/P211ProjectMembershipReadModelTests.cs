using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories.Projects;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Projects;

[Trait("TaskId", "P2-11")]
public sealed class P211ProjectMembershipReadModelTests : IClassFixture<IdentitySqlServerFixture>
{
    private static readonly DateOnly EffectiveDate = new(2026, 9, 20);
    private readonly IdentitySqlServerFixture _fixture;

    public P211ProjectMembershipReadModelTests(IdentitySqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "P2-11: active effective memberships return authoritative project roles")]
    public async Task FindActiveEffectiveAsync_AssignedRoles_ReturnsAuthoritativeMembership()
    {
        var data = await ProjectMembershipSqlFixture.CreateAsync(_fixture);
        await using var context = _fixture.CreateDbContext();
        var readModel = new ProjectMembershipReadModel(context);

        var cases = new[]
        {
            (data.SupervisorId, UserRoleCode.Supervisor),
            (data.ProjectManagerId, UserRoleCode.ProjectManager),
            (data.DroneOperatorId, UserRoleCode.DroneOperator),
            (data.RepairCrewId, UserRoleCode.RepairCrew)
        };

        foreach (var (userId, expectedRole) in cases)
        {
            var membership = await readModel.FindActiveEffectiveAsync(userId, data.PrimaryProjectId, EffectiveDate);

            membership.Should().NotBeNull();
            membership!.ProjectId.Should().Be(data.PrimaryProjectId);
            membership.UserId.Should().Be(userId);
            membership.RoleCode.Should().Be(expectedRole);
        }
    }

    [Fact(DisplayName = "P2-11: cross-project and ended memberships return no authorization read model")]
    public async Task FindActiveEffectiveAsync_CrossProjectOrEndedMembership_ReturnsNull()
    {
        var data = await ProjectMembershipSqlFixture.CreateAsync(_fixture);
        await using var context = _fixture.CreateDbContext();
        var readModel = new ProjectMembershipReadModel(context);

        var crossProject = await readModel.FindActiveEffectiveAsync(
            data.DroneOperatorId,
            data.SecondaryProjectId,
            EffectiveDate);
        var ended = await readModel.FindActiveEffectiveAsync(
            data.ExpiredDroneOperatorId,
            data.SecondaryProjectId,
            EffectiveDate);

        crossProject.Should().BeNull();
        ended.Should().BeNull();
    }

    [Fact(DisplayName = "P2-11: membership read model returns membership facts without deciding account role")]
    public async Task FindActiveEffectiveAsync_AccountRoleChanges_ReturnsMembershipFact()
    {
        var data = await ProjectMembershipSqlFixture.CreateAsync(_fixture);
        await using var context = _fixture.CreateDbContext();
        var readModel = new ProjectMembershipReadModel(context);

        (await readModel.FindActiveEffectiveAsync(data.DroneOperatorId, data.PrimaryProjectId, EffectiveDate))
            .Should().NotBeNull();

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [Users] SET [RoleCode] = {"PM"} WHERE [Id] = {data.DroneOperatorId}");

        var membership = await readModel.FindActiveEffectiveAsync(
            data.DroneOperatorId,
            data.PrimaryProjectId,
            EffectiveDate);

        membership.Should().NotBeNull();
        membership!.RoleCode.Should().Be(UserRoleCode.DroneOperator);
    }

    [Fact(DisplayName = "P2-11: membership changes are immediately visible to the read model")]
    public async Task FindActiveEffectiveAsync_EndedMembershipChange_ReturnsNullImmediately()
    {
        var data = await ProjectMembershipSqlFixture.CreateAsync(_fixture);
        await using var context = _fixture.CreateDbContext();
        var readModel = new ProjectMembershipReadModel(context);

        var membership = await context.ProjectMembers.SingleAsync(member =>
            member.ProjectId == data.PrimaryProjectId && member.UserId == data.RepairCrewId);
        membership.Status = ProjectMemberStatus.Ended;
        membership.ValidTo = EffectiveDate;
        await context.SaveChangesAsync();

        (await readModel.FindActiveEffectiveAsync(data.RepairCrewId, data.PrimaryProjectId, EffectiveDate))
            .Should().BeNull();
    }
}
