using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using System.Reflection;
using Microsoft.Extensions.Options;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Repositories.Seeding;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Identity;

[Trait("TaskId", "P2-10")]
public sealed class IdentityPersistenceNegativeTests : IClassFixture<IdentitySqlServerFixture>
{
    private readonly IdentitySqlServerFixture _fixture;

    public IdentityPersistenceNegativeTests(IdentitySqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "P2-10 Negative: Duplicate username differing only by case is rejected")]
    public async Task DuplicateUsername_DifferingOnlyByCase_IsRejected()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var usernameLower = $"user_{Guid.NewGuid():N}";
        var usernameUpper = usernameLower.ToUpperInvariant();

        var user1 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = usernameLower,
            NormalizedUserName = usernameLower.ToUpperInvariant(),
            DisplayName = "User One",
            PasswordHash = "hash1",
            RoleCode = UserRoleCode.ProjectManager,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var user2 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = usernameUpper,
            NormalizedUserName = usernameUpper.ToUpperInvariant(),
            DisplayName = "User Two",
            PasswordHash = "hash2",
            RoleCode = UserRoleCode.ProjectManager,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Users.Add(user1);
        await context.SaveChangesAsync();

        context.Users.Add(user2);
        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact(DisplayName = "P2-10 Negative: Duplicate non-null email is rejected")]
    public async Task DuplicateNonNullEmail_IsRejected()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var sharedEmail = $"dup_{Guid.NewGuid():N}@example.com";

        var name1 = $"user1_{Guid.NewGuid():N}";
        var user1 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = name1,
            NormalizedUserName = name1.ToUpperInvariant(),
            Email = sharedEmail,
            NormalizedEmail = sharedEmail.ToUpperInvariant(),
            DisplayName = "User One",
            PasswordHash = "hash1",
            RoleCode = UserRoleCode.DroneOperator,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var name2 = $"user2_{Guid.NewGuid():N}";
        var user2 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = name2,
            NormalizedUserName = name2.ToUpperInvariant(),
            Email = sharedEmail,
            NormalizedEmail = sharedEmail.ToUpperInvariant(),
            DisplayName = "User Two",
            PasswordHash = "hash2",
            RoleCode = UserRoleCode.DroneOperator,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Users.Add(user1);
        await context.SaveChangesAsync();

        context.Users.Add(user2);
        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact(DisplayName = "P2-10 Negative: Unknown role code or unknown status is rejected")]
    public async Task UnknownRoleOrStatus_IsRejected()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var userWithUnknownRole = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"unknown_role_{Guid.NewGuid():N}",
            DisplayName = "Unknown Role",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.Unknown,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Users.Add(userWithUnknownRole);
        var actRole = () => context.SaveChangesAsync();
        await actRole.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*RoleCode cannot be Unknown*");

        context.Entry(userWithUnknownRole).State = EntityState.Detached;

        var userWithUnknownStatus = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"unknown_status_{Guid.NewGuid():N}",
            DisplayName = "Unknown Status",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.Supervisor,
            Status = UserStatus.Unknown,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Users.Add(userWithUnknownStatus);
        var actStatus = () => context.SaveChangesAsync();
        await actStatus.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Status cannot be Unknown*");
    }

    [Fact(DisplayName = "P2-10 Negative: Direct SQL insert with invalid foreign key role code is rejected")]
    public async Task DirectSqlInsert_InvalidRoleCode_IsRejectedByForeignKey()
    {
        await using var context = _fixture.CreateDbContext();
        var userId = Guid.NewGuid();
        var username = $"invalid_fk_{Guid.NewGuid():N}";
        var normalized = username.ToUpperInvariant();

        var act = () => context.Database.ExecuteSqlAsync(
            $"INSERT INTO [Users] ([Id], [UserName], [NormalizedUserName], [DisplayName], [PasswordHash], [RoleCode], [Status], [MustChangePassword], [CreatedAt]) VALUES ({userId}, {username}, {normalized}, 'Invalid Role', 'hash', 'NONEXISTENT_ROLE', 1, 0, SYSDATETIMEOFFSET());");

        await act.Should().ThrowAsync<SqlException>();
    }

    [Fact(DisplayName = "P2-10 Negative: Session invalid temporal invariants are rejected")]
    public async Task Session_InvalidTemporal_IsRejected()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"sess_temp_{Guid.NewGuid():N}",
            DisplayName = "Session User",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.RepairCrew,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;

        // 1. ExpiresAt <= IssuedAt
        var sessionExpiresBeforeIssued = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IssuedAt = now,
            ExpiresAt = now.AddMinutes(-5)
        };
        context.Sessions.Add(sessionExpiresBeforeIssued);
        var actExpires = () => context.SaveChangesAsync();
        await actExpires.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Session ExpiresAt must be after IssuedAt*");

        context.Entry(sessionExpiresBeforeIssued).State = EntityState.Detached;

        // 2. RevokedAt < IssuedAt
        var sessionRevokedBeforeIssued = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IssuedAt = now,
            ExpiresAt = now.AddDays(7),
            RevokedAt = now.AddMinutes(-10)
        };
        context.Sessions.Add(sessionRevokedBeforeIssued);
        var actRevoked = () => context.SaveChangesAsync();
        await actRevoked.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Session RevokedAt cannot be before IssuedAt*");

        context.Entry(sessionRevokedBeforeIssued).State = EntityState.Detached;

        // 3. Database Check Constraint CK_Sessions_ExpiresAt via direct SQL
        var sessionId = Guid.NewGuid();
        var issued = now;
        var expired = now.AddMinutes(-1);
        var actSql = () => context.Database.ExecuteSqlAsync(
            $"INSERT INTO [Sessions] ([Id], [UserId], [IssuedAt], [ExpiresAt]) VALUES ({sessionId}, {user.Id}, {issued}, {expired});");
        await actSql.Should().ThrowAsync<SqlException>()
            .WithMessage("*CK_Sessions_ExpiresAt*");
    }

    [Fact(DisplayName = "P2-10 Negative: Session invalid metadata is rejected")]
    public async Task Session_InvalidMetadata_IsRejected()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"sess_meta_{Guid.NewGuid():N}",
            DisplayName = "Session Meta User",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.Supervisor,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;

        // 1. Wrong schema version
        var sessionWrongVersion = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IssuedAt = now,
            ExpiresAt = now.AddDays(7),
            DeviceMetadataJson = "{\"schema_version\":2}"
        };
        context.Sessions.Add(sessionWrongVersion);
        var actVersion = () => context.SaveChangesAsync();
        await actVersion.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid Session DeviceMetadataJson*");

        context.Entry(sessionWrongVersion).State = EntityState.Detached;

        // 2. Unknown properties rejected
        var sessionUnknownProp = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IssuedAt = now,
            ExpiresAt = now.AddDays(7),
            DeviceMetadataJson = "{\"schema_version\":1,\"unknown_token\":\"abc\"}"
        };
        context.Sessions.Add(sessionUnknownProp);
        var actProp = () => context.SaveChangesAsync();
        await actProp.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid Session DeviceMetadataJson*");

        context.Entry(sessionUnknownProp).State = EntityState.Detached;

        // 3. Direct SQL insert with non-JSON string violates ISJSON check constraint CK_Sessions_DeviceMetadataJson_Json
        var sessionId = Guid.NewGuid();
        var expires = now.AddDays(1);
        var actSql = () => context.Database.ExecuteSqlAsync(
            $"INSERT INTO [Sessions] ([Id], [UserId], [IssuedAt], [ExpiresAt], [DeviceMetadataJson]) VALUES ({sessionId}, {user.Id}, {now}, {expires}, 'not-valid-json');");
        await actSql.Should().ThrowAsync<SqlException>()
            .WithMessage("*CK_Sessions_DeviceMetadataJson_Json*");
    }

    [Fact(DisplayName = "P2-10 Negative: Session write-once fields cannot be modified")]
    public async Task Session_WriteOnceFields_ModificationIsRejected()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"sess_wo_{Guid.NewGuid():N}",
            DisplayName = "Session WO User",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.Supervisor,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IssuedAt = now,
            ExpiresAt = now.AddDays(7),
            DeviceMetadataJson = "{\"schema_version\":1,\"platform\":\"Android\"}"
        };
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        // Try modifying UserId
        session.UserId = Guid.NewGuid();
        var actUserId = () => context.SaveChangesAsync();
        await actUserId.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*UserSession.UserId is write-once*");

        // Fresh context for IssuedAt
        await using var context2 = _fixture.CreateDbContext();
        var session2 = await context2.Sessions.SingleAsync(s => s.Id == session.Id);
        session2.IssuedAt = now.AddHours(-1);
        var actIssuedAt = () => context2.SaveChangesAsync();
        await actIssuedAt.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*UserSession.IssuedAt is write-once*");

        // Fresh context for DeviceMetadataJson
        await using var context3 = _fixture.CreateDbContext();
        var session3 = await context3.Sessions.SingleAsync(s => s.Id == session.Id);
        session3.DeviceMetadataJson = "{\"schema_version\":1,\"platform\":\"iOS\"}";
        var actMeta = () => context3.SaveChangesAsync();
        await actMeta.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*UserSession.DeviceMetadataJson is write-once*");
    }

    [Fact(DisplayName = "P2-10 Negative: PasswordResetLog update and delete are rejected by DbContext and DB trigger")]
    public async Task PasswordResetLog_UpdateOrDelete_IsRejected()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var targetUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"target_pr_{Guid.NewGuid():N}",
            DisplayName = "PR Target User",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.Supervisor,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.Add(targetUser);
        await context.SaveChangesAsync();

        var log = new PasswordResetLog
        {
            Id = Guid.NewGuid(),
            PerformedByUserId = null,
            TargetUserId = targetUser.Id,
            OccurredAt = DateTimeOffset.UtcNow,
            Source = SecurityLogSafeValueCodes.Sources.AdminApi,
            Result = PasswordResetResult.Success
        };
        context.PasswordResetLogs.Add(log);
        await context.SaveChangesAsync();

        // 1. DbContext update rejected
        log.Source = "ModifiedSource";
        var actUpdate = () => context.SaveChangesAsync();
        await actUpdate.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*PasswordResetLogs are append-only*");

        // 2. DbContext delete rejected
        context.Entry(log).State = EntityState.Deleted;
        var actDelete = () => context.SaveChangesAsync();
        await actDelete.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*PasswordResetLogs are append-only*");

        // 3. Direct SQL update blocked by database trigger TR_PasswordResetLogs_AppendOnly
        var logId = log.Id;
        var actSqlUpdate = () => context.Database.ExecuteSqlAsync(
            $"UPDATE [PasswordResetLogs] SET [Source] = 'DirectSql' WHERE [Id] = {logId};");
        await actSqlUpdate.Should().ThrowAsync<SqlException>()
            .WithMessage("*PasswordResetLogs are append-only*");

        // 4. Direct SQL delete blocked by database trigger TR_PasswordResetLogs_AppendOnly
        var actSqlDelete = () => context.Database.ExecuteSqlAsync(
            $"DELETE FROM [PasswordResetLogs] WHERE [Id] = {logId};");
        await actSqlDelete.Should().ThrowAsync<SqlException>()
            .WithMessage("*PasswordResetLogs are append-only*");
    }

    [Fact(DisplayName = "P2-10 Negative: AccountStatusChangeLog update, delete, and invalid transitions are rejected")]
    public async Task AccountStatusChangeLog_UpdateDeleteAndInvalidTransition_IsRejected()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var targetUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"target_asc_{Guid.NewGuid():N}",
            DisplayName = "ASC Target User",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.Supervisor,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.Add(targetUser);
        await context.SaveChangesAsync();

        var log = new AccountStatusChangeLog
        {
            Id = Guid.NewGuid(),
            ChangedByUserId = null,
            TargetUserId = targetUser.Id,
            OccurredAt = DateTimeOffset.UtcNow,
            FromStatus = UserStatus.Active,
            ToStatus = UserStatus.Suspended,
            Reason = SecurityLogSafeValueCodes.Reasons.SafetyPolicyViolation,
            Source = SecurityLogSafeValueCodes.Sources.ComplianceReview
        };
        context.AccountStatusChangeLogs.Add(log);
        await context.SaveChangesAsync();

        // 1. DbContext update rejected
        log.Reason = "Modified Reason";
        var actUpdate = () => context.SaveChangesAsync();
        await actUpdate.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*AccountStatusChangeLogs are append-only*");

        // 2. DbContext delete rejected
        context.Entry(log).State = EntityState.Deleted;
        var actDelete = () => context.SaveChangesAsync();
        await actDelete.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*AccountStatusChangeLogs are append-only*");

        context.Entry(log).State = EntityState.Detached;

        // 3. Direct SQL update blocked by database trigger TR_AccountStatusChangeLogs_AppendOnly
        var logId = log.Id;
        var actSqlUpdate = () => context.Database.ExecuteSqlAsync(
            $"UPDATE [AccountStatusChangeLogs] SET [Reason] = 'DirectSql' WHERE [Id] = {logId};");
        await actSqlUpdate.Should().ThrowAsync<SqlException>()
            .WithMessage("*AccountStatusChangeLogs are append-only*");

        // 4. Direct SQL delete blocked by database trigger TR_AccountStatusChangeLogs_AppendOnly
        var actSqlDelete = () => context.Database.ExecuteSqlAsync(
            $"DELETE FROM [AccountStatusChangeLogs] WHERE [Id] = {logId};");
        await actSqlDelete.Should().ThrowAsync<SqlException>()
            .WithMessage("*AccountStatusChangeLogs are append-only*");

        // 5. FromStatus == ToStatus rejected by DbContext
        var invalidLog = new AccountStatusChangeLog
        {
            Id = Guid.NewGuid(),
            ChangedByUserId = null,
            TargetUserId = targetUser.Id,
            OccurredAt = DateTimeOffset.UtcNow,
            FromStatus = UserStatus.Active,
            ToStatus = UserStatus.Active,
            Reason = SecurityLogSafeValueCodes.Reasons.NoStatusChange,
            Source = SecurityLogSafeValueCodes.Sources.System
        };
        context.AccountStatusChangeLogs.Add(invalidLog);
        var actSameStatus = () => context.SaveChangesAsync();
        await actSameStatus.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*FromStatus and ToStatus must be different*");

        context.Entry(invalidLog).State = EntityState.Detached;

        // 6. Direct SQL insert with FromStatus == ToStatus blocked by CHECK constraint
        var badLogId = Guid.NewGuid();
        var actSqlCheck = () => context.Database.ExecuteSqlAsync(
            $"INSERT INTO [AccountStatusChangeLogs] ([Id], [TargetUserId], [OccurredAt], [FromStatus], [ToStatus], [Reason], [Source]) VALUES ({badLogId}, {targetUser.Id}, SYSDATETIMEOFFSET(), 1, 1, 'No change', 'DirectSql');");
        await actSqlCheck.Should().ThrowAsync<SqlException>();
    }

    [Fact(DisplayName = "P2-10 Negative: UserRoleChanged with stale rowversion returns StaleConcurrency")]
    public async Task UserRoleChanged_StaleRowVersion_ReturnsStaleConcurrency()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"stale_user_{Guid.NewGuid():N}",
            DisplayName = "Stale User",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.DroneOperator,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var staleRowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
        var repo = _fixture.CreateRepository(context);

        var result = await repo.ChangeUserRoleAtomicAsync(
            userId: user.Id,
            newRoleCode: UserRoleCode.RepairCrew,
            expectedRowVersion: staleRowVersion,
            actorUserId: Guid.NewGuid(),
            operationId: Guid.NewGuid());

        result.Status.Should().Be(UserRoleChangeStatus.StaleConcurrency);
    }

    [Fact(DisplayName = "P2-10 Negative: UserRoleChanged with same operationId but different target role returns IdempotentConflict")]
    public async Task UserRoleChanged_SameOperationId_DifferentTargetRole_ReturnsIdempotentConflict()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var actorUserId = Guid.NewGuid();
        var actor = new ApplicationUser
        {
            Id = actorUserId,
            UserName = $"actor_{Guid.NewGuid():N}",
            DisplayName = "Actor User",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.Supervisor,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"target_user_{Guid.NewGuid():N}",
            DisplayName = "Target User",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.DroneOperator,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.AddRange(actor, user);
        await context.SaveChangesAsync();

        var repo = _fixture.CreateRepository(context);
        var operationId = Guid.NewGuid();

        // First attempt: changes to RepairCrew
        var firstResult = await repo.ChangeUserRoleAtomicAsync(
            userId: user.Id,
            newRoleCode: UserRoleCode.RepairCrew,
            expectedRowVersion: user.RowVersion,
            actorUserId: actorUserId,
            operationId: operationId);

        firstResult.Status.Should().Be(UserRoleChangeStatus.Success);

        // Reload user to get fresh rowversion
        await context.Entry(user).ReloadAsync();

        // Second attempt with same operationId but different role: PM
        var conflictResult = await repo.ChangeUserRoleAtomicAsync(
            userId: user.Id,
            newRoleCode: UserRoleCode.ProjectManager,
            expectedRowVersion: user.RowVersion,
            actorUserId: actorUserId,
            operationId: operationId);

        conflictResult.Status.Should().Be(UserRoleChangeStatus.IdempotentConflict);
    }

    [Fact(DisplayName = "P2-10 Negative: Concurrent refresh token rotation rejects loser with StaleConcurrency or AlreadyRevoked")]
    public async Task ConcurrentRefreshTokenRotation_RejectsLoser()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"rot_user_{Guid.NewGuid():N}",
            DisplayName = "Rotation User",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.Supervisor,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IssuedAt = now,
            ExpiresAt = now.AddDays(7)
        };
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        var oldToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TokenHash = $"hash_{Guid.NewGuid():N}",
            ExpiresAt = now.AddDays(7)
        };
        context.RefreshTokens.Add(oldToken);
        await context.SaveChangesAsync();

        // Two callers with the exact same initial rowversion
        var rowVersion = oldToken.RowVersion.ToArray();

        await using var context1 = _fixture.CreateDbContext();
        await using var context2 = _fixture.CreateDbContext();

        var repo1 = _fixture.CreateRepository(context1);
        var repo2 = _fixture.CreateRepository(context2);

        var newToken1 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TokenHash = $"hash1_{Guid.NewGuid():N}",
            ExpiresAt = now.AddDays(7)
        };

        var newToken2 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TokenHash = $"hash2_{Guid.NewGuid():N}",
            ExpiresAt = now.AddDays(7)
        };

        var task1 = Task.Run(() => repo1.RotateRefreshTokenAsync(oldToken.Id, rowVersion, newToken1));
        var task2 = Task.Run(() => repo2.RotateRefreshTokenAsync(oldToken.Id, rowVersion, newToken2));
        var results = await Task.WhenAll(task1, task2);

        var statuses = new[] { results[0].Status, results[1].Status };
        statuses.Should().ContainSingle(s => s == RotateRefreshTokenStatus.Success);
        statuses.Should().Contain(s => s == RotateRefreshTokenStatus.StaleConcurrency || s == RotateRefreshTokenStatus.AlreadyRevoked);
    }

    [Fact(DisplayName = "P2-10 Negative: Direct role modification and UserStore role mutation bypass are rejected")]
    public async Task UserRole_DirectModificationOrUserStoreBypass_IsRejected()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"role_bypass_{Guid.NewGuid():N}",
            DisplayName = "Bypass User",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.DroneOperator,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // 1. Direct modification in DbContext without atomic role scope is rejected
        user.RoleCode = UserRoleCode.Supervisor;
        var actDirect = () => context.SaveChangesAsync();
        await actDirect.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Direct modification of ApplicationUser.RoleCode is forbidden*");

        context.Entry(user).State = EntityState.Unchanged;

        // 2. IUserRoleStore.AddToRoleAsync on existing user is rejected
        using var userStore = new RoadGuardUserStore(context);
        var actStoreAdd = () => userStore.AddToRoleAsync(user, "SUPERVISOR", default);
        await actStoreAdd.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Role changes for existing users cannot be performed via IUserRoleStore.AddToRoleAsync*");

        // 3. IUserRoleStore.RemoveFromRoleAsync on existing user is rejected
        var actStoreRemove = () => userStore.RemoveFromRoleAsync(user, "DRONE_OPERATOR", default);
        await actStoreRemove.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Role changes for existing users cannot be performed via IUserRoleStore.RemoveFromRoleAsync*");
    }

    [Fact(DisplayName = "P2-10 Negative: RefreshToken expired old token or invalid/empty hash are rejected")]
    public async Task RefreshToken_ExpiredOrInvalidHash_IsRejected()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"tok_inv_{Guid.NewGuid():N}",
            DisplayName = "Token Inv User",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.RepairCrew,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IssuedAt = now.AddDays(-2),
            ExpiresAt = now.AddDays(5)
        };
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        // 1. Old token whose ExpiresAt is in the past cannot be rotated
        var expiredToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TokenHash = $"exp_hash_{Guid.NewGuid():N}_000000000000",
            ExpiresAt = now.AddDays(1)
        };
        context.RefreshTokens.Add(expiredToken);
        await context.SaveChangesAsync();

        // Simulate elapsed time by updating ExpiresAt into the past via direct SQL
        await context.Database.ExecuteSqlAsync(
            $"UPDATE [RefreshTokens] SET [ExpiresAt] = {now.AddMinutes(-5)} WHERE [Id] = {expiredToken.Id};");
        await context.Entry(expiredToken).ReloadAsync();

        var repo = _fixture.CreateRepository(context);
        var validNewToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TokenHash = $"valid_new_hash_{Guid.NewGuid():N}_0000000",
            ExpiresAt = now.AddDays(7)
        };

        var expiredResult = await repo.RotateRefreshTokenAsync(expiredToken.Id, expiredToken.RowVersion, validNewToken);
        expiredResult.Status.Should().Be(RotateRefreshTokenStatus.Expired);

        // 2. Rotation with empty or short TokenHash (< 32 chars) is rejected
        var activeToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TokenHash = $"active_hash_{Guid.NewGuid():N}_0000000",
            ExpiresAt = now.AddDays(7)
        };
        context.RefreshTokens.Add(activeToken);
        await context.SaveChangesAsync();

        var emptyHashToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TokenHash = "",
            ExpiresAt = now.AddDays(7)
        };
        var emptyResult = await repo.RotateRefreshTokenAsync(activeToken.Id, activeToken.RowVersion, emptyHashToken);
        emptyResult.Status.Should().Be(RotateRefreshTokenStatus.InvalidToken);

        var shortHashToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TokenHash = "too_short_hash",
            ExpiresAt = now.AddDays(7)
        };
        var shortResult = await repo.RotateRefreshTokenAsync(activeToken.Id, activeToken.RowVersion, shortHashToken);
        shortResult.Status.Should().Be(RotateRefreshTokenStatus.InvalidToken);

        // 3. Direct SQL insert of empty TokenHash is rejected by database check constraint
        var actSqlEmpty = () => context.Database.ExecuteSqlAsync(
            $"INSERT INTO [RefreshTokens] ([Id], [SessionId], [TokenHash], [ExpiresAt]) VALUES ({Guid.NewGuid()}, {session.Id}, '', {now.AddDays(1)});");
        await actSqlEmpty.Should().ThrowAsync<SqlException>()
            .WithMessage("*CK_RefreshTokens_TokenHash_NotEmpty*");

        // 4. DbContext save of empty TokenHash is rejected by validation
        var invalidEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TokenHash = "   ",
            ExpiresAt = now.AddDays(1)
        };
        context.RefreshTokens.Add(invalidEntity);
        var actDb = () => context.SaveChangesAsync();
        await actDb.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*RefreshToken.TokenHash must be a valid non-empty cryptographic hash*");

        context.Entry(invalidEntity).State = EntityState.Detached;
    }

    [Fact(DisplayName = "P2-10 Negative: PasswordResetLog and AccountStatusChangeLog reject sensitive data in Reason or Source")]
    public async Task SecurityLogs_SensitiveDataInReasonOrSource_ThrowsInvalidOperationException()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var targetUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"sens_log_{Guid.NewGuid():N}",
            DisplayName = "Sens User",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.Supervisor,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.Add(targetUser);
        await context.SaveChangesAsync();

        // 1. PasswordResetLog with password in Reason
        var prLogReason = new PasswordResetLog
        {
            Id = Guid.NewGuid(),
            TargetUserId = targetUser.Id,
            OccurredAt = DateTimeOffset.UtcNow,
            Source = SecurityLogSafeValueCodes.Sources.AdminApi,
            Reason = "User password changed to temp_password_123",
            Result = PasswordResetResult.Success
        };
        context.PasswordResetLogs.Add(prLogReason);
        var actPrReason = () => context.SaveChangesAsync();
        await actPrReason.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*PasswordResetLog.Reason contains forbidden sensitive data*");

        context.Entry(prLogReason).State = EntityState.Detached;

        // 2. PasswordResetLog with secret token in Source
        var prLogSource = new PasswordResetLog
        {
            Id = Guid.NewGuid(),
            TargetUserId = targetUser.Id,
            OccurredAt = DateTimeOffset.UtcNow,
            Source = "bearer eyJhbGciOi...",
            Reason = SecurityLogSafeValueCodes.Reasons.SelfServiceAccountRecovery,
            Result = PasswordResetResult.Success
        };
        context.PasswordResetLogs.Add(prLogSource);
        var actPrSource = () => context.SaveChangesAsync();
        await actPrSource.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*PasswordResetLog.Source contains forbidden sensitive data*");

        context.Entry(prLogSource).State = EntityState.Detached;

        // 3. AccountStatusChangeLog with access_token in Reason
        var ascLogReason = new AccountStatusChangeLog
        {
            Id = Guid.NewGuid(),
            TargetUserId = targetUser.Id,
            OccurredAt = DateTimeOffset.UtcNow,
            FromStatus = UserStatus.Active,
            ToStatus = UserStatus.Suspended,
            Reason = "Revoked due to compromised access_token",
            Source = SecurityLogSafeValueCodes.Sources.ComplianceReview
        };
        context.AccountStatusChangeLogs.Add(ascLogReason);
        var actAscReason = () => context.SaveChangesAsync();
        await actAscReason.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*AccountStatusChangeLog.Reason contains forbidden sensitive data*");

        context.Entry(ascLogReason).State = EntityState.Detached;

        // 4. AccountStatusChangeLog with credential keyword in Source
        var ascLogSource = new AccountStatusChangeLog
        {
            Id = Guid.NewGuid(),
            TargetUserId = targetUser.Id,
            OccurredAt = DateTimeOffset.UtcNow,
            FromStatus = UserStatus.Active,
            ToStatus = UserStatus.Suspended,
            Reason = SecurityLogSafeValueCodes.Reasons.AdministrativeLock,
            Source = "credential_provider_api"
        };
        context.AccountStatusChangeLogs.Add(ascLogSource);
        var actAscSource = () => context.SaveChangesAsync();
        await actAscSource.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*AccountStatusChangeLog.Source contains forbidden sensitive data*");

        context.Entry(ascLogSource).State = EntityState.Detached;
    }

    [Fact(DisplayName = "P2-10 Negative: Staged failure during atomic role change rolls back all modifications")]
    public async Task UserRoleChanged_StagedFailure_RollsBackAllModifications()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var actorUserId = Guid.NewGuid();
        var actor = new ApplicationUser
        {
            Id = actorUserId,
            UserName = $"actor_rb_{Guid.NewGuid():N}",
            DisplayName = "Actor User",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.Supervisor,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"target_rb_{Guid.NewGuid():N}",
            DisplayName = "Target User",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.DroneOperator,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.AddRange(actor, user);
        await context.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IssuedAt = now,
            ExpiresAt = now.AddDays(7)
        };
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TokenHash = $"rb_tok_{Guid.NewGuid():N}_0000000000000",
            ExpiresAt = now.AddDays(7)
        };
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();

        // Stage failure: add an entity to the tracked context that will violate database uniqueness on SaveChangesAsync inside the transaction.
        // Specifically, a RefreshToken colliding with the existing token's TokenHash on UX_RefreshTokens_TokenHash.
        var collidingToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TokenHash = token.TokenHash, // Duplicate key on UX_RefreshTokens_TokenHash!
            ExpiresAt = now.AddDays(14)
        };
        context.RefreshTokens.Add(collidingToken);

        var repo = _fixture.CreateRepository(context);

        // Act: attempt atomic role change; SaveChangesAsync within transaction triggers DbUpdateException due to UX_RefreshTokens_TokenHash
        var stagedOpId = Guid.NewGuid();
        var act = () => repo.ChangeUserRoleAtomicAsync(
            userId: user.Id,
            newRoleCode: UserRoleCode.ProjectManager,
            expectedRowVersion: user.RowVersion,
            actorUserId: actorUserId,
            operationId: stagedOpId);

        await act.Should().ThrowAsync<DbUpdateException>();

        // Assert rollback in a fresh context:
        await using var verifyContext = _fixture.CreateDbContext();
        var rolledBackUser = await verifyContext.Users.SingleAsync(u => u.Id == user.Id);
        rolledBackUser.RoleCode.Should().Be(UserRoleCode.DroneOperator); // Unchanged!

        var rolledBackSession = await verifyContext.Sessions.SingleAsync(s => s.Id == session.Id);
        rolledBackSession.RevokedAt.Should().BeNull(); // Not revoked!

        var rolledBackToken = await verifyContext.RefreshTokens.SingleAsync(t => t.Id == token.Id);
        rolledBackToken.RevokedAt.Should().BeNull(); // Not revoked!

        var auditLogCount = await verifyContext.AuditLogs
            .CountAsync(a => a.EntityId == user.Id && a.EventType == "UserRoleChanged");
        auditLogCount.Should().Be(0); // 0 audit logs inserted!

        var idempCount = await verifyContext.IdempotencyRecords
            .CountAsync(r => r.OperationId == stagedOpId || (r.Operation == "UserRoleChanged" && r.ActorUserId == actorUserId));
        idempCount.Should().Be(0); // 0 idempotency records inserted!
    }

    [Fact(DisplayName = "P2-10 Negative: IdentityRoleSeedStep preserves existing custom roles without rename or reactivation")]
    public async Task IdentityRoleSeedStep_ExistingRolePreserved_NoMutationNoRenameNoReactivation()
    {
        await using var context = _fixture.CreateDbContext();

        var existingRole = await context.Roles.SingleOrDefaultAsync(r => r.Code == UserRoleCode.DroneOperator);
        if (existingRole is null)
        {
            existingRole = new ApplicationRole(UserRoleCode.DroneOperator, "Custom Drone Operator")
            {
                IsActive = false
            };
            context.Roles.Add(existingRole);
            await context.SaveChangesAsync();
        }
        else
        {
            existingRole.Name = "Custom Drone Operator";
            existingRole.IsActive = false;
            await context.SaveChangesAsync();
        }

        var seedStep = new IdentityRoleSeedStep();
        await seedStep.SeedAsync(context);

        // Reload role: must remain customized and inactive
        await context.Entry(existingRole).ReloadAsync();
        existingRole.Name.Should().Be("Custom Drone Operator");
        existingRole.IsActive.Should().BeFalse();

        // Restore standard state for other tests
        existingRole.Name = "Drone Operator";
        existingRole.IsActive = true;
        await context.SaveChangesAsync();
    }

    [Fact(DisplayName = "P2-10 Negative: IdentityRoleSeedStep concurrent execution from independent contexts succeeds")]
    public async Task IdentityRoleSeedStep_ConcurrentExecutions_SucceedWithoutDuplicateKeyErrors()
    {
        await using var context1 = _fixture.CreateDbContext();
        await using var context2 = _fixture.CreateDbContext();

        var step1 = new IdentityRoleSeedStep();
        var step2 = new IdentityRoleSeedStep();

        var task1 = Task.Run(() => step1.SeedAsync(context1));
        var task2 = Task.Run(() => step2.SeedAsync(context2));

        await Task.WhenAll(task1, task2);

        await using var verifyContext = _fixture.CreateDbContext();
        var roleCount = await verifyContext.Roles.CountAsync();
        roleCount.Should().Be(4);
    }

    [Fact(DisplayName = "P2-10 Negative: PermitRoleMutationScope is non-public and cannot be called by external callers")]
    public void PermitRoleMutationScope_IsNonPublic()
    {
        var method = typeof(RoadGuardDbContext).GetMethod("PermitRoleMutationScope", BindingFlags.Public | BindingFlags.Instance);
        method.Should().BeNull("PermitRoleMutationScope must be internal to Repositories and not exposed publicly on RoadGuardDbContext");
    }

    [Fact(DisplayName = "P2-10 Negative: Specialized logs reject opaque base64url secrets without deny-list keywords")]
    public async Task SecurityLogs_OpaqueBase64UrlSecretInReason_ThrowsInvalidOperationException()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var targetUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"opaque_sens_{Guid.NewGuid():N}",
            DisplayName = "Opaque Sens User",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.Supervisor,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.Add(targetUser);
        await context.SaveChangesAsync();

        const string opaqueSecret = "Abcd_Efgh-Ijkl_Mnop-Qrst_Uvwx-Yz0123456789";

        var prLogOpaque = new PasswordResetLog
        {
            Id = Guid.NewGuid(),
            TargetUserId = targetUser.Id,
            OccurredAt = DateTimeOffset.UtcNow,
            Source = SecurityLogSafeValueCodes.Sources.AdminApi,
            Reason = opaqueSecret,
            Result = PasswordResetResult.Success
        };
        context.PasswordResetLogs.Add(prLogOpaque);

        var actPr = () => context.SaveChangesAsync();
        await actPr.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*contains forbidden sensitive data*");

        context.Entry(prLogOpaque).State = EntityState.Detached;

        var directSqlLogId = Guid.NewGuid();
        var safeSource = SecurityLogSafeValueCodes.Sources.AdminApi;
        var actSql = () => context.Database.ExecuteSqlAsync(
            $"INSERT INTO [PasswordResetLogs] ([Id], [TargetUserId], [OccurredAt], [Reason], [Result], [Source]) VALUES ({directSqlLogId}, {targetUser.Id}, SYSDATETIMEOFFSET(), {opaqueSecret}, 1, {safeSource});");
        await actSql.Should().ThrowAsync<SqlException>()
            .WithMessage("*CK_PasswordResetLogs_Reason_SafeCode*");
    }

    [Fact(DisplayName = "P2-10 Negative: Mismatched NormalizedUserName throws InvalidOperationException")]
    public async Task Users_MismatchedNormalizedUserName_ThrowsInvalidOperationException()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"Alice_{Guid.NewGuid():N}",
            NormalizedUserName = "MISMATCHED_BOB",
            DisplayName = "Alice",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.Supervisor,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.Add(user);

        var act = () => context.SaveChangesAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*must match the canonical upper-invariant normalization of UserName*");
    }

    [Fact(DisplayName = "P2-10 Negative: RoadGuardDbContext with configured metadata options rejects payloads exceeding configured limits")]
    public async Task SessionDeviceMetadata_ConfiguredLimits_RejectsExceedingPayloads()
    {
        var customOptions = Options.Create(new SessionDeviceMetadataOptions
        {
            MaxDeviceIdLength = 5 // Custom configured limit (default is 100)
        });

        var dbOptions = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(_fixture.ConnectionString, x => x.UseNetTopologySuite())
            .Options;

        await using var context = new RoadGuardDbContext(dbOptions, customOptions);
        await _fixture.SeedRolesAsync(context);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"opt_user_{Guid.NewGuid():N}",
            DisplayName = "Opt User",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.DroneOperator,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // device_id has length 10 ("device_123"). Valid under default 100, invalid under custom 5.
        var metadataJson = """{"schema_version":1,"device_id":"device_123","platform":"android"}""";

        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IssuedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            DeviceMetadataJson = metadataJson
        };
        context.Sessions.Add(session);

        var act = () => context.SaveChangesAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Field 'device_id' exceeds maximum length 5*");
    }

    [Fact(DisplayName = "P2-10 Negative: Controlled competing refresh token rotation causes loser to receive StaleConcurrency")]
    public async Task RefreshToken_ControlledCompetingUpdate_LoserReceivesStaleConcurrency()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"comp_tok_{Guid.NewGuid():N}",
            DisplayName = "Comp User",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.DroneOperator,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.Add(user);

        var now = DateTimeOffset.UtcNow;
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IssuedAt = now,
            ExpiresAt = now.AddDays(1)
        };
        context.Sessions.Add(session);

        var oldToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TokenHash = $"hash_orig_{Guid.NewGuid():N}_00000000000",
            ExpiresAt = now.AddDays(7)
        };
        context.RefreshTokens.Add(oldToken);
        await context.SaveChangesAsync();

        var initialRowVersion = oldToken.RowVersion.ToArray();

        var barrier = new SqlCommandBarrierInterceptor(
            commandText => commandText.Contains("UPDATE [RefreshTokens]", StringComparison.OrdinalIgnoreCase));

        await using var context1 = _fixture.CreateDbContext(barrier);
        var repo1 = _fixture.CreateRepository(context1);

        await using var context2 = _fixture.CreateDbContext(barrier);
        var repo2 = _fixture.CreateRepository(context2);

        var newToken1 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TokenHash = $"hash_new1_{Guid.NewGuid():N}_00000000000",
            ExpiresAt = now.AddDays(7)
        };

        var newToken2 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TokenHash = $"hash_new2_{Guid.NewGuid():N}_00000000000",
            ExpiresAt = now.AddDays(7)
        };

        var task1 = repo1.RotateRefreshTokenAsync(oldToken.Id, initialRowVersion, newToken1);
        var task2 = repo2.RotateRefreshTokenAsync(oldToken.Id, initialRowVersion, newToken2);
        var results = await Task.WhenAll(task1, task2);

        barrier.ArrivalCount.Should().Be(2, "both contexts must reach the token UPDATE before either is released");
        results.Select(result => result.Status).Should().ContainSingle(status => status == RotateRefreshTokenStatus.Success);
        results.Select(result => result.Status).Should().ContainSingle(status => status == RotateRefreshTokenStatus.StaleConcurrency);
    }

    [Fact(DisplayName = "P2-10 Negative: Concurrent same-payload role change race resolves loser to IdempotentReplay")]
    public async Task UserRoleChanged_ConcurrentSamePayloadRace_LoserReturnsIdempotentReplay()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"same_race_{Guid.NewGuid():N}",
            DisplayName = "Race User",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.DroneOperator,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var sharedOpId = Guid.NewGuid();
        var actorUserId = user.Id;
        var rowVersion = user.RowVersion.ToArray();

        var barrier = new SqlCommandBarrierInterceptor(
            commandText => commandText.Contains("UPDATE [Users]", StringComparison.OrdinalIgnoreCase));

        await using var context1 = _fixture.CreateDbContext(barrier);
        await using var context2 = _fixture.CreateDbContext(barrier);
        var repo1 = _fixture.CreateRepository(context1);
        var repo2 = _fixture.CreateRepository(context2);

        var task1 = repo1.ChangeUserRoleAtomicAsync(
            user.Id, UserRoleCode.ProjectManager, rowVersion, actorUserId, sharedOpId);
        var task2 = repo2.ChangeUserRoleAtomicAsync(
            user.Id, UserRoleCode.ProjectManager, rowVersion, actorUserId, sharedOpId);

        var results = await Task.WhenAll(task1, task2);

        barrier.ArrivalCount.Should().Be(2, "both contexts must reach the user UPDATE before either is released");
        var statuses = new[] { results[0].Status, results[1].Status };
        statuses.Should().ContainSingle(s => s == UserRoleChangeStatus.Success);
        statuses.Should().ContainSingle(s => s == UserRoleChangeStatus.IdempotentReplay);
    }

    [Fact(DisplayName = "P2-10 Negative: Concurrent changed-payload role change race resolves loser to IdempotentConflict")]
    public async Task UserRoleChanged_ConcurrentChangedPayloadRace_LoserReturnsIdempotentConflict()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"diff_race_{Guid.NewGuid():N}",
            DisplayName = "Diff Race User",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.DroneOperator,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var sharedOpId = Guid.NewGuid();
        var actorUserId = user.Id;
        var rowVersion = user.RowVersion.ToArray();

        var barrier = new SqlCommandBarrierInterceptor(
            commandText => commandText.Contains("UPDATE [Users]", StringComparison.OrdinalIgnoreCase));

        await using var context1 = _fixture.CreateDbContext(barrier);
        await using var context2 = _fixture.CreateDbContext(barrier);
        var repo1 = _fixture.CreateRepository(context1);
        var repo2 = _fixture.CreateRepository(context2);

        var task1 = repo1.ChangeUserRoleAtomicAsync(
            user.Id, UserRoleCode.ProjectManager, rowVersion, actorUserId, sharedOpId);
        var task2 = repo2.ChangeUserRoleAtomicAsync(
            user.Id, UserRoleCode.Supervisor, rowVersion, actorUserId, sharedOpId);

        var results = await Task.WhenAll(task1, task2);

        barrier.ArrivalCount.Should().Be(2, "both contexts must reach the user UPDATE before either is released");
        var statuses = new[] { results[0].Status, results[1].Status };
        statuses.Should().ContainSingle(s => s == UserRoleChangeStatus.Success);
        statuses.Should().ContainSingle(s => s == UserRoleChangeStatus.IdempotentConflict);
    }

    [Fact(DisplayName = "P2-10 Negative: Migration fails closed when legacy AuditLog contains orphan ActorUserId and requires verified remediation")]
    public async Task MigrationUpgrade_WithLegacyAuditLogActors_FailsClosed_EnforcesRemediation()
    {
        var fixture = new SqlServerTestFixture();
        await fixture.InitializeAsync();
        try
        {
            await using (var baseline = fixture.CreateDbContext())
            {
                await baseline.Database.EnsureDeletedAsync();
            }

            var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
                .UseSqlServer(fixture.ConnectionString, x => x.UseNetTopologySuite())
                .Options;

            await using var context = new RoadGuardDbContext(options);

            // Step 1: Migrate ONLY to P2-02 migration
            var migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync("20260918065914_AddAuditOutboxIdempotencyConcurrencyPrimitives");

            // Step 2: Insert a legacy AuditLog with non-null ActorUserId (no Users table exists in P2-02!)
            var legacyActorId = Guid.NewGuid();
            var auditId = Guid.NewGuid();
            await context.Database.ExecuteSqlAsync(
                $"INSERT INTO [AuditLogs] ([Id], [ActorUserId], [OccurredAtUtc], [EventType], [EntityType], [EntityId], [Source]) VALUES ({auditId}, {legacyActorId}, SYSDATETIMEOFFSET(), 'ProjectCreated', 'Project', {Guid.NewGuid()}, 'LegacyP202');");

            // Verify AuditLog has the orphan ActorUserId and no Users table exists
            var orphanAuditCount = await context.Database.SqlQuery<int>(
                $"SELECT CAST(COUNT(*) AS int) AS [Value] FROM [AuditLogs] WHERE [ActorUserId] = {legacyActorId}")
                .SingleAsync();
            orphanAuditCount.Should().Be(1);

            // Step 3: Apply the identity-schema stage. It must not add the deferred AuditLog actor FK.
            await migrator.MigrateAsync("20260918152126_AddIdentitySessionSecurityLogs");

            var identityTableCount = await context.Database.SqlQueryRaw<int>(
                """
                SELECT CAST(COUNT(*) AS int) AS [Value]
                FROM sys.tables
                WHERE [name] IN ('Roles', 'Users', 'Sessions', 'RefreshTokens', 'PasswordResetLogs', 'AccountStatusChangeLogs')
                """)
                .SingleAsync();
            identityTableCount.Should().Be(6);

            var deferredFkCount = await context.Database.SqlQueryRaw<int>(
                """
                SELECT CAST(COUNT(*) AS int) AS [Value]
                FROM sys.foreign_keys
                WHERE [name] = 'FK_AuditLogs_Users_ActorUserId'
                """)
                .SingleAsync();
            deferredFkCount.Should().Be(0);

            // Step 4: The separate FK stage fails closed while attribution has no matching verified User.
            var actMigrate = () => context.Database.MigrateAsync();
            await actMigrate.Should().ThrowAsync<SqlException>()
                .Where(ex => ex.Number == 51000 && ex.Message.Contains("Migration precondition failed: AuditLogs contains legacy ActorUserId values"));

            var actorAfterFailedFk = await context.Database.SqlQuery<Guid>(
                $"SELECT [ActorUserId] AS [Value] FROM [AuditLogs] WHERE [Id] = {auditId}")
                .SingleAsync();
            actorAfterFailedFk.Should().Be(legacyActorId);

            // Step 5: Preserve attribution by provisioning the verified legacy actor with the exact same ID.
            await new IdentityRoleSeedStep().SeedAsync(context, CancellationToken.None);
            context.Users.Add(new ApplicationUser
            {
                Id = legacyActorId,
                UserName = $"verified_legacy_actor_{legacyActorId:N}",
                DisplayName = "Verified legacy audit actor",
                PasswordHash = "verified-import-disabled-login",
                RoleCode = UserRoleCode.Supervisor,
                Status = UserStatus.Pending,
                MustChangePassword = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await context.SaveChangesAsync();

            // Step 6: Retry the FK stage; it now succeeds without changing historical AuditLog data.
            await context.Database.MigrateAsync();

            var tableCountP210 = await context.Database.SqlQueryRaw<int>(
                """
                SELECT CAST(COUNT(*) AS int) AS [Value]
                FROM sys.tables
                WHERE [name] IN ('Roles', 'Users', 'Sessions', 'RefreshTokens', 'PasswordResetLogs', 'AccountStatusChangeLogs')
                """)
                .SingleAsync();
            tableCountP210.Should().Be(6);

            var preservedActor = await context.Database.SqlQuery<Guid>(
                $"SELECT [ActorUserId] AS [Value] FROM [AuditLogs] WHERE [Id] = {auditId}")
                .SingleAsync();
            preservedActor.Should().Be(legacyActorId);

            var enforcedFkCount = await context.Database.SqlQueryRaw<int>(
                """
                SELECT CAST(COUNT(*) AS int) AS [Value]
                FROM sys.foreign_keys
                WHERE [name] = 'FK_AuditLogs_Users_ActorUserId'
                """)
                .SingleAsync();
            enforcedFkCount.Should().Be(1);

            // Step 7: Verify downgrade back to P2-02 drops identity tables cleanly
            await migrator.MigrateAsync("20260918065914_AddAuditOutboxIdempotencyConcurrencyPrimitives");

            var tableCountP202 = await context.Database.SqlQueryRaw<int>(
                """
                SELECT CAST(COUNT(*) AS int) AS [Value]
                FROM sys.tables
                WHERE [name] IN ('Roles', 'Users', 'Sessions', 'RefreshTokens', 'PasswordResetLogs', 'AccountStatusChangeLogs')
                """)
                .SingleAsync();
            tableCountP202.Should().Be(0);
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }
}
