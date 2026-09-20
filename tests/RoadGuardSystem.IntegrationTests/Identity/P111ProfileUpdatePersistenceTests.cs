using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Identity;

[Trait("TaskId", "P1-11")]
public sealed class P111ProfileUpdatePersistenceTests : IClassFixture<IdentitySqlServerFixture>
{
    private readonly IdentitySqlServerFixture _fixture;

    public P111ProfileUpdatePersistenceTests(IdentitySqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "P1-11 S3 SQL: profile update replay does not duplicate audit")]
    public async Task ProfileUpdate_Replay_IsIdempotentWithoutDuplicateAudit()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);
        var user = await CreateUserAsync(context);
        var operationId = Guid.NewGuid();
        var expectedVersion = user.RowVersion.ToArray();
        var repository = new IdentityRepository(context);

        var first = await repository.UpdateUserProfileAtomicAsync(
            user.Id,
            "Updated Name",
            "updated@example.test",
            expectedVersion,
            operationId);
        var replay = await repository.UpdateUserProfileAtomicAsync(
            user.Id,
            "Updated Name",
            "updated@example.test",
            expectedVersion,
            operationId);

        first.Status.Should().Be(UserProfileUpdateStatus.Success);
        replay.Status.Should().Be(UserProfileUpdateStatus.IdempotentReplay);
        (await context.AuditLogs.CountAsync(item =>
            item.EntityId == user.Id && item.EventType == "user_profile_updated"))
            .Should().Be(1);
    }

    [Fact(DisplayName = "P1-11 S3 SQL: duplicate email is rejected without partial profile write")]
    public async Task ProfileUpdate_DuplicateEmail_LeavesTargetUnchanged()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);
        var owner = await CreateUserAsync(context, "owner@example.test");
        var target = await CreateUserAsync(context);
        var repository = new IdentityRepository(context);

        var result = await repository.UpdateUserProfileAtomicAsync(
            target.Id,
            "Should Not Persist",
            owner.Email,
            target.RowVersion.ToArray(),
            Guid.NewGuid());

        result.Status.Should().Be(UserProfileUpdateStatus.EmailConflict);
        var persisted = await context.Users.AsNoTracking().SingleAsync(item => item.Id == target.Id);
        persisted.DisplayName.Should().Be("Original Name");
        persisted.Email.Should().BeNull();
    }

    private static async Task<ApplicationUser> CreateUserAsync(
        RoadGuardSystem.Repositories.RoadGuardDbContext context,
        string? email = null)
    {
        var username = $"p111_sql_{Guid.NewGuid():N}";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant(),
            DisplayName = "Original Name",
            Email = email,
            NormalizedEmail = email?.ToUpperInvariant(),
            PasswordHash = "not-used-by-persistence-test",
            RoleCode = UserRoleCode.DroneOperator,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }
}
