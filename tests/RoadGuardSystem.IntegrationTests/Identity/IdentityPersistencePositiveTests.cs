using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Extensions;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories.Seeding;
using RoadGuardSystem.Seeder;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Identity;

[Trait("TaskId", "P2-10")]
public sealed class IdentityPersistencePositiveTests : IClassFixture<IdentitySqlServerFixture>
{
    private readonly IdentitySqlServerFixture _fixture;

    public IdentityPersistencePositiveTests(IdentitySqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "P2-10 Positive: Four canonical roles seeded deterministically and idempotently")]
    public async Task RoleSeed_Idempotent_SeedsFourCanonicalRoles()
    {
        await using var context = _fixture.CreateDbContext();

        var step = new IdentityRoleSeedStep();
        await step.SeedAsync(context, CancellationToken.None);

        var roles = await context.Roles.AsNoTracking().ToListAsync();
        roles.Should().HaveCount(4);

        var supervisor = roles.Single(r => r.Code == UserRoleCode.Supervisor);
        supervisor.Name.Should().Be("Supervisor");
        supervisor.IsActive.Should().BeTrue();

        var pm = roles.Single(r => r.Code == UserRoleCode.ProjectManager);
        pm.Name.Should().Be("Project Manager");
        pm.IsActive.Should().BeTrue();

        var drone = roles.Single(r => r.Code == UserRoleCode.DroneOperator);
        drone.Name.Should().Be("Drone Operator");
        drone.IsActive.Should().BeTrue();

        var repair = roles.Single(r => r.Code == UserRoleCode.RepairCrew);
        repair.Name.Should().Be("Repair Crew");
        repair.IsActive.Should().BeTrue();

        // Idempotency: execute seed again
        await step.SeedAsync(context, CancellationToken.None);
        var rolesAfterSecondRun = await context.Roles.AsNoTracking().ToListAsync();
        rolesAfterSecondRun.Should().HaveCount(4);
    }

    [Fact(DisplayName = "P2-10 Positive: User creation with null and non-null email round-trips and advances rowversion")]
    public async Task User_NullAndNonNullEmail_RoundTrip()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        // Multiple users with NULL email allowed by filtered unique index
        var user1 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"null_email_1_{Guid.NewGuid():N}",
            DisplayName = "Null Email 1",
            Email = null,
            NormalizedEmail = null,
            PasswordHash = "hash1",
            RoleCode = UserRoleCode.DroneOperator,
            Status = UserStatus.Active,
            MustChangePassword = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var user2 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"null_email_2_{Guid.NewGuid():N}",
            DisplayName = "Null Email 2",
            Email = null,
            NormalizedEmail = null,
            PasswordHash = "hash2",
            RoleCode = UserRoleCode.RepairCrew,
            Status = UserStatus.Pending,
            MustChangePassword = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var user3Email = $"user3_{Guid.NewGuid():N}@example.com";
        var user3 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"has_email_{Guid.NewGuid():N}",
            DisplayName = "Has Email",
            Email = user3Email,
            NormalizedEmail = user3Email.ToUpperInvariant(),
            PasswordHash = "hash3",
            RoleCode = UserRoleCode.Supervisor,
            Status = UserStatus.Active,
            MustChangePassword = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Users.AddRange(user1, user2, user3);
        await context.SaveChangesAsync();

        user1.RowVersion.Should().NotBeEmpty();
        var initialVersion = user1.RowVersion.ToArray();

        // Advance rowversion on update
        user1.DisplayName = "Updated Null Email 1";
        await context.SaveChangesAsync();

        user1.RowVersion.Should().NotEqual(initialVersion);

        // Query back
        var fetched = await context.Users.AsNoTracking().SingleAsync(u => u.Id == user3.Id);
        fetched.Email.Should().Be(user3Email);
        fetched.RoleCode.Should().Be(UserRoleCode.Supervisor);
        fetched.Status.Should().Be(UserStatus.Active);
    }

    [Fact(DisplayName = "P2-10 Positive: Session with and without device metadata round-trips and derives state")]
    public async Task Session_WithAndWithoutMetadata_RoundTripAndDerivedState()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"session_user_{Guid.NewGuid():N}",
            DisplayName = "Session User",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.Supervisor,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;

        // Session 1: No metadata, active
        var activeSession = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IssuedAt = now,
            ExpiresAt = now.AddDays(7),
            DeviceMetadataJson = null
        };

        // Session 2: Valid schema-v1 metadata, revoked
        var revokedSession = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IssuedAt = now.AddHours(-2),
            ExpiresAt = now.AddDays(5),
            RevokedAt = now.AddHours(-1),
            DeviceMetadataJson = "{\"schema_version\":1,\"device_id\":\"phone-01\",\"platform\":\"Android\",\"app_version\":\"2.1.0\"}"
        };

        // Session 3: Expired session
        var expiredSession = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IssuedAt = now.AddDays(-10),
            ExpiresAt = now.AddDays(-3),
            RevokedAt = null,
            DeviceMetadataJson = "{\"schema_version\":1}"
        };

        context.Sessions.AddRange(activeSession, revokedSession, expiredSession);
        await context.SaveChangesAsync();

        activeSession.IsActiveAt(DateTimeOffset.UtcNow).Should().BeTrue();
        activeSession.IsRevoked.Should().BeFalse();
        activeSession.IsExpiredAt(DateTimeOffset.UtcNow).Should().BeFalse();

        revokedSession.IsActiveAt(DateTimeOffset.UtcNow).Should().BeFalse();
        revokedSession.IsRevoked.Should().BeTrue();
        revokedSession.IsExpiredAt(DateTimeOffset.UtcNow).Should().BeFalse();

        expiredSession.IsActiveAt(DateTimeOffset.UtcNow).Should().BeFalse();
        expiredSession.IsRevoked.Should().BeFalse();
        expiredSession.IsExpiredAt(DateTimeOffset.UtcNow).Should().BeTrue();

        var fetched = await context.Sessions.AsNoTracking().SingleAsync(s => s.Id == revokedSession.Id);
        fetched.DeviceMetadataJson.Should().Contain("\"platform\":\"Android\"");
    }

    [Fact(DisplayName = "P2-10 Positive: Refresh token rotation and family revocation persist accurately")]
    public async Task RefreshToken_RotationAndFamilyRevocation()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"token_user_{Guid.NewGuid():N}",
            DisplayName = "Token User",
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

        var token1 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TokenHash = $"hash1_{Guid.NewGuid():N}",
            ExpiresAt = now.AddDays(7)
        };
        context.RefreshTokens.Add(token1);
        await context.SaveChangesAsync();

        var repo = _fixture.CreateRepository(context);

        // Rotate token 1 -> token 2
        var token2 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TokenHash = $"hash2_{Guid.NewGuid():N}",
            ExpiresAt = now.AddDays(7)
        };

        var rotateResult = await repo.RotateRefreshTokenAsync(token1.Id, token1.RowVersion.ToArray(), token2);
        rotateResult.Status.Should().Be(RotateRefreshTokenStatus.Success);

        // Verify token 1 is revoked and token 2 is active
        await context.Entry(token1).ReloadAsync();
        token1.RevokedAt.Should().NotBeNull();
        token1.IsActiveAt(DateTimeOffset.UtcNow).Should().BeFalse();
        token2.IsActiveAt(DateTimeOffset.UtcNow).Should().BeTrue();

        // Revoke family
        await repo.RevokeSessionAndFamilyAsync(session.Id);

        await context.Entry(session).ReloadAsync();
        session.RevokedAt.Should().NotBeNull();

        await context.Entry(token2).ReloadAsync();
        token2.RevokedAt.Should().NotBeNull();
        token2.IsActiveAt(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact(DisplayName = "P2-10 Positive: Specialized security logs append and query correctly")]
    public async Task SecurityLogs_AppendAndQuery()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var actor = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"log_actor_{Guid.NewGuid():N}",
            DisplayName = "Log Actor",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.Supervisor,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var target = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"log_target_{Guid.NewGuid():N}",
            DisplayName = "Log Target",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.ProjectManager,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.AddRange(actor, target);
        await context.SaveChangesAsync();

        var actorUserId = actor.Id;
        var targetUserId = target.Id;
        var now = DateTimeOffset.UtcNow;

        var resetLog = new PasswordResetLog
        {
            Id = Guid.NewGuid(),
            PerformedByUserId = actorUserId,
            TargetUserId = targetUserId,
            OccurredAt = now,
            Source = SecurityLogSafeValueCodes.Sources.AdminApi,
            Result = PasswordResetResult.Success,
            CorrelationId = Guid.NewGuid(),
            Reason = SecurityLogSafeValueCodes.Reasons.SelfServiceAccountRecovery
        };

        var handoverRef = Guid.NewGuid();
        var statusLog = new AccountStatusChangeLog
        {
            Id = Guid.NewGuid(),
            ChangedByUserId = actorUserId,
            TargetUserId = targetUserId,
            OccurredAt = now,
            FromStatus = UserStatus.Pending,
            ToStatus = UserStatus.Active,
            Reason = SecurityLogSafeValueCodes.Reasons.RegistrationApproved,
            Source = SecurityLogSafeValueCodes.Sources.AdminApi,
            CorrelationId = Guid.NewGuid(),
            HandoverReference = handoverRef
        };

        context.PasswordResetLogs.Add(resetLog);
        context.AccountStatusChangeLogs.Add(statusLog);
        await context.SaveChangesAsync();

        var fetchedReset = await context.PasswordResetLogs.AsNoTracking().SingleAsync(l => l.Id == resetLog.Id);
        fetchedReset.Source.Should().Be(SecurityLogSafeValueCodes.Sources.AdminApi);
        fetchedReset.Result.Should().Be(PasswordResetResult.Success);

        var fetchedStatus = await context.AccountStatusChangeLogs.AsNoTracking().SingleAsync(l => l.Id == statusLog.Id);
        fetchedStatus.FromStatus.Should().Be(UserStatus.Pending);
        fetchedStatus.ToStatus.Should().Be(UserStatus.Active);
        fetchedStatus.HandoverReference.Should().Be(handoverRef);
    }

    [Fact(DisplayName = "P2-10 Positive: Atomic UserRoleChanged updates role, revokes credentials, appends audit, and supports idempotent replay")]
    public async Task UserRoleChanged_AtomicExecutionAndIdempotentReplay()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var actorUserId = Guid.NewGuid();
        var actor = new ApplicationUser
        {
            Id = actorUserId,
            UserName = $"atomic_actor_{Guid.NewGuid():N}",
            DisplayName = "Atomic Actor",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.Supervisor,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var targetUserId = Guid.NewGuid();
        var targetUser = new ApplicationUser
        {
            Id = targetUserId,
            UserName = $"atomic_target_{Guid.NewGuid():N}",
            DisplayName = "Atomic Target",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.DroneOperator,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Users.AddRange(actor, targetUser);
        await context.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var session1 = new UserSession { Id = Guid.NewGuid(), UserId = targetUserId, IssuedAt = now, ExpiresAt = now.AddDays(7) };
        var session2 = new UserSession { Id = Guid.NewGuid(), UserId = targetUserId, IssuedAt = now, ExpiresAt = now.AddDays(7) };
        context.Sessions.AddRange(session1, session2);
        await context.SaveChangesAsync();

        var token1 = new RefreshToken { Id = Guid.NewGuid(), SessionId = session1.Id, TokenHash = $"tok1_{Guid.NewGuid():N}", ExpiresAt = now.AddDays(7) };
        var token2 = new RefreshToken { Id = Guid.NewGuid(), SessionId = session2.Id, TokenHash = $"tok2_{Guid.NewGuid():N}", ExpiresAt = now.AddDays(7) };
        context.RefreshTokens.AddRange(token1, token2);
        await context.SaveChangesAsync();

        var repo = _fixture.CreateRepository(context);
        var operationId = Guid.NewGuid();

        // Act: change role from DroneOperator to RepairCrew
        var result = await repo.ChangeUserRoleAtomicAsync(
            userId: targetUserId,
            newRoleCode: UserRoleCode.RepairCrew,
            expectedRowVersion: targetUser.RowVersion,
            actorUserId: actorUserId,
            operationId: operationId,
            reason: "Promoted to RepairCrew");

        result.Status.Should().Be(UserRoleChangeStatus.Success);
        result.RoleCode.Should().Be(UserRoleCode.RepairCrew);

        // Assert user updated
        await context.Entry(targetUser).ReloadAsync();
        targetUser.RoleCode.Should().Be(UserRoleCode.RepairCrew);

        // Assert sessions revoked
        await context.Entry(session1).ReloadAsync();
        await context.Entry(session2).ReloadAsync();
        session1.RevokedAt.Should().NotBeNull();
        session2.RevokedAt.Should().NotBeNull();

        // Assert refresh tokens revoked
        await context.Entry(token1).ReloadAsync();
        await context.Entry(token2).ReloadAsync();
        token1.RevokedAt.Should().NotBeNull();
        token2.RevokedAt.Should().NotBeNull();

        // Assert AuditLog created
        var audit = await context.AuditLogs.AsNoTracking()
            .SingleOrDefaultAsync(a => a.EntityId == targetUserId && a.EventType == "UserRoleChanged");
        audit.Should().NotBeNull();
        audit!.ActorUserId.Should().Be(actorUserId);
        audit.BeforeSnapshot.Should().Contain("\"role_code\":\"DRONE_OPERATOR\"");
        audit.AfterSnapshot.Should().Contain("\"role_code\":\"REPAIR_CREW\"");

        // Assert Idempotent Replay
        var replayResult = await repo.ChangeUserRoleAtomicAsync(
            userId: targetUserId,
            newRoleCode: UserRoleCode.RepairCrew,
            expectedRowVersion: targetUser.RowVersion,
            actorUserId: actorUserId,
            operationId: operationId,
            reason: "Promoted to RepairCrew");

        replayResult.Status.Should().Be(UserRoleChangeStatus.IdempotentReplay);

        // Assert no duplicate audit log added
        var auditCount = await context.AuditLogs.AsNoTracking()
            .CountAsync(a => a.EntityId == targetUserId && a.EventType == "UserRoleChanged");
        auditCount.Should().Be(1);
    }

    [Fact(DisplayName = "P2-10 Positive: IIdentityRepository authoritative reads exclude sensitive data")]
    public async Task IdentityRepository_AuthoritativeReads_NoSecretsExposed()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var username = $"sec_read_{Guid.NewGuid():N}";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant(),
            DisplayName = "Security Read User",
            PasswordHash = "super-secret-hash",
            RoleCode = UserRoleCode.ProjectManager,
            Status = UserStatus.Active,
            MustChangePassword = false,
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
            DeviceMetadataJson = "{\"schema_version\":1,\"platform\":\"Web\"}"
        };
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        var tokenHash = $"thash_{Guid.NewGuid():N}";
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TokenHash = tokenHash,
            ExpiresAt = now.AddDays(7)
        };
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();

        var repo = _fixture.CreateRepository(context);

        // 1. GetUserSecurityStateAsync
        var userState = await repo.GetUserSecurityStateAsync(user.Id);
        userState.Should().NotBeNull();
        userState!.RoleCode.Should().Be(UserRoleCode.ProjectManager);
        userState.Status.Should().Be(UserStatus.Active);

        // 2. GetUserSecurityStateByUsernameAsync (case-insensitive)
        var userStateByLower = await repo.GetUserSecurityStateByUsernameAsync(username.ToLowerInvariant());
        userStateByLower.Should().NotBeNull();
        userStateByLower!.Id.Should().Be(user.Id);

        // 3. GetSessionSecurityStateAsync
        var sessionState = await repo.GetSessionSecurityStateAsync(session.Id);
        sessionState.Should().NotBeNull();
        sessionState!.IsActive.Should().BeTrue();
        sessionState.UserId.Should().Be(user.Id);

        // 4. FindRefreshTokenByHashAsync
        var tokenState = await repo.FindRefreshTokenByHashAsync(tokenHash);
        tokenState.Should().NotBeNull();
        tokenState!.IsActive.Should().BeTrue();
        tokenState.SessionId.Should().Be(session.Id);
    }

    [Fact(DisplayName = "P2-10 Positive: RoadGuardUserStore and RoadGuardRoleStore satisfy Identity store operations")]
    public async Task IdentityStores_UserStoreAndRoleStore_BasicOperations()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var roleStore = new RoadGuardRoleStore(context);
        var userStore = new RoadGuardUserStore(context);

        // RoleStore operations
        var pmRole = await roleStore.FindByNameAsync("PM", CancellationToken.None);
        pmRole.Should().NotBeNull();
        pmRole!.Code.Should().Be(UserRoleCode.ProjectManager);
        pmRole.Name.Should().Be("Project Manager");

        var roleName = await roleStore.GetRoleNameAsync(pmRole, CancellationToken.None);
        roleName.Should().Be("Project Manager");

        var roleId = await roleStore.GetRoleIdAsync(pmRole, CancellationToken.None);
        roleId.Should().Be("PM");

        // UserStore operations
        var username = $"store_user_{Guid.NewGuid():N}";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant(),
            DisplayName = "Store User",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.Supervisor,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var createResult = await userStore.CreateAsync(user, CancellationToken.None);
        createResult.Succeeded.Should().BeTrue();

        var foundUser = await userStore.FindByNameAsync(username, CancellationToken.None);
        foundUser.Should().NotBeNull();
        foundUser!.Id.Should().Be(user.Id);

        var roles = await userStore.GetRolesAsync(foundUser, CancellationToken.None);
        roles.Should().ContainSingle(r => r == "SUPERVISOR");

        var isInRole = await userStore.IsInRoleAsync(foundUser, "SUPERVISOR", CancellationToken.None);
        isInRole.Should().BeTrue();

        var isNotInRole = await userStore.IsInRoleAsync(foundUser, "PM", CancellationToken.None);
        isNotInRole.Should().BeFalse();
    }

    [Fact(DisplayName = "P2-10 Positive: Migration applies to empty database downgrades to P2-02 and reapplies")]
    public async Task MigrationLifecycle_UpDowngradeReapply()
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

            // Step 1: Migrate to latest
            await context.Database.MigrateAsync();

            var tableCountLatest = await context.Database.SqlQueryRaw<int>(
                """
                SELECT CAST(COUNT(*) AS int) AS [Value]
                FROM sys.tables
                WHERE [name] IN (
                    'AuditLogs', 'IdempotencyRecords', 'OutboxMessages', 'ConsumerEffectReceipts',
                    'Roles', 'Users', 'Sessions', 'RefreshTokens', 'PasswordResetLogs', 'AccountStatusChangeLogs'
                )
                """)
                .SingleAsync();

            tableCountLatest.Should().Be(10);

            // Step 2: Downgrade to P2-02 migration
            var migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync("20260918065914_AddAuditOutboxIdempotencyConcurrencyPrimitives");

            var tableCountP202 = await context.Database.SqlQueryRaw<int>(
                """
                SELECT CAST(COUNT(*) AS int) AS [Value]
                FROM sys.tables
                WHERE [name] IN (
                    'AuditLogs', 'IdempotencyRecords', 'OutboxMessages', 'ConsumerEffectReceipts'
                )
                """)
                .SingleAsync();

            tableCountP202.Should().Be(4);

            var identityTableCount = await context.Database.SqlQueryRaw<int>(
                """
                SELECT CAST(COUNT(*) AS int) AS [Value]
                FROM sys.tables
                WHERE [name] IN (
                    'Roles', 'Users', 'Sessions', 'RefreshTokens', 'PasswordResetLogs', 'AccountStatusChangeLogs'
                )
                """)
                .SingleAsync();

            identityTableCount.Should().Be(0);

            // Step 3: Reapply latest
            await context.Database.MigrateAsync();

            var tableCountReapplied = await context.Database.SqlQueryRaw<int>(
                """
                SELECT CAST(COUNT(*) AS int) AS [Value]
                FROM sys.tables
                WHERE [name] IN (
                    'AuditLogs', 'IdempotencyRecords', 'OutboxMessages', 'ConsumerEffectReceipts',
                    'Roles', 'Users', 'Sessions', 'RefreshTokens', 'PasswordResetLogs', 'AccountStatusChangeLogs'
                )
                """)
                .SingleAsync();

            tableCountReapplied.Should().Be(10);
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    [Fact(DisplayName = "P2-10 Positive: Role change only revokes active sessions and tokens, preserving expired and already-revoked")]
    public async Task UserRoleChanged_OnlyRevokesActiveCredentials_PreservesExpiredAndAlreadyRevoked()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var actor = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"actor_hist_{Guid.NewGuid():N}",
            DisplayName = "Actor Hist",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.Supervisor,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"user_hist_{Guid.NewGuid():N}",
            DisplayName = "Target Hist",
            PasswordHash = "hash",
            RoleCode = UserRoleCode.DroneOperator,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.AddRange(actor, user);
        await context.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;

        // 1. Active session + active token
        var activeSession = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IssuedAt = now,
            ExpiresAt = now.AddDays(7),
            RevokedAt = null
        };
        var activeToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = activeSession.Id,
            TokenHash = $"active_tok_{Guid.NewGuid():N}_000000000000",
            ExpiresAt = now.AddDays(7),
            RevokedAt = null
        };

        // 2. Already revoked session + revoked token (historical revocation)
        var priorRevocationTime = now.AddHours(-2);
        var revokedSession = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IssuedAt = now.AddDays(-1),
            ExpiresAt = now.AddDays(6),
            RevokedAt = priorRevocationTime
        };
        var revokedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = revokedSession.Id,
            TokenHash = $"revoked_tok_{Guid.NewGuid():N}_00000000000",
            ExpiresAt = now.AddDays(6),
            RevokedAt = priorRevocationTime
        };

        // 3. Expired session + expired token (historical expiration, never revoked)
        var expiredTime = now.AddHours(-1);
        var expiredSession = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IssuedAt = now.AddDays(-2),
            ExpiresAt = now.AddDays(5),
            RevokedAt = null
        };
        var expiredToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = expiredSession.Id,
            TokenHash = $"expired_tok_{Guid.NewGuid():N}_00000000000",
            ExpiresAt = now.AddDays(5),
            RevokedAt = null
        };

        context.Sessions.AddRange(activeSession, revokedSession, expiredSession);
        context.RefreshTokens.AddRange(activeToken, revokedToken, expiredToken);
        await context.SaveChangesAsync();

        // Simulate elapsed time for expired credentials via direct SQL
        await context.Database.ExecuteSqlAsync(
            $"UPDATE [Sessions] SET [ExpiresAt] = {expiredTime} WHERE [Id] = {expiredSession.Id};");
        await context.Database.ExecuteSqlAsync(
            $"UPDATE [RefreshTokens] SET [ExpiresAt] = {expiredTime} WHERE [Id] = {expiredToken.Id};");
        await context.Entry(expiredSession).ReloadAsync();
        await context.Entry(expiredToken).ReloadAsync();

        var repo = _fixture.CreateRepository(context);
        var opId = Guid.NewGuid();

        var result = await repo.ChangeUserRoleAtomicAsync(
            userId: user.Id,
            newRoleCode: UserRoleCode.RepairCrew,
            expectedRowVersion: user.RowVersion,
            actorUserId: actor.Id,
            operationId: opId);

        result.Status.Should().Be(UserRoleChangeStatus.Success);

        // Verify in fresh DbContext:
        await using var verifyContext = _fixture.CreateDbContext();

        var verifiedActiveSession = await verifyContext.Sessions.SingleAsync(s => s.Id == activeSession.Id);
        verifiedActiveSession.RevokedAt.Should().NotBeNull();
        verifiedActiveSession.RevokedAt.Should().BeOnOrAfter(now);

        var verifiedActiveToken = await verifyContext.RefreshTokens.SingleAsync(t => t.Id == activeToken.Id);
        verifiedActiveToken.RevokedAt.Should().NotBeNull();
        verifiedActiveToken.RevokedAt.Should().BeOnOrAfter(now);

        var verifiedRevokedSession = await verifyContext.Sessions.SingleAsync(s => s.Id == revokedSession.Id);
        verifiedRevokedSession.RevokedAt.Should().Be(priorRevocationTime);

        var verifiedRevokedToken = await verifyContext.RefreshTokens.SingleAsync(t => t.Id == revokedToken.Id);
        verifiedRevokedToken.RevokedAt.Should().Be(priorRevocationTime);

        var verifiedExpiredSession = await verifyContext.Sessions.SingleAsync(s => s.Id == expiredSession.Id);
        verifiedExpiredSession.RevokedAt.Should().BeNull();

        var verifiedExpiredToken = await verifyContext.RefreshTokens.SingleAsync(t => t.Id == expiredToken.Id);
        verifiedExpiredToken.RevokedAt.Should().BeNull();
    }

    [Fact(DisplayName = "P2-10 Positive: Seeder CLI executes successfully and seeds canonical roles idempotently")]
    public async Task SeederCli_ExecutesSuccessfully_AndSeedsCanonicalRolesIdempotently()
    {
        var envLookup = (string varName) =>
            varName == RoadGuardSystem.Seeder.Program.ConnectionStringEnvVarName
                ? _fixture.ConnectionString
                : null;

        var exitCode1 = await RoadGuardSystem.Seeder.Program.RunAsync(Array.Empty<string>(), envLookup);
        exitCode1.Should().Be(0);

        await using var verifyContext = _fixture.CreateDbContext();
        var rolesCount = await verifyContext.Roles.CountAsync();
        rolesCount.Should().Be(4);

        // Second execution: idempotent replay
        var exitCode2 = await RoadGuardSystem.Seeder.Program.RunAsync(Array.Empty<string>(), envLookup);
        exitCode2.Should().Be(0);

        var rolesCountAfter = await verifyContext.Roles.CountAsync();
        rolesCountAfter.Should().Be(4);
    }

    [Fact(DisplayName = "P2-06: AddRoadGuardSeeding registers identity and drone device seed steps")]
    public async Task AddRoadGuardSeeding_RegistersAllPersistenceSeedStepsAndExecutesSuccessfully()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{RoadGuardDatabaseOptions.SectionName}:ConnectionString"] = _fixture.ConnectionString,
                [$"{RoadGuardDatabaseOptions.SectionName}:EnableSensitiveDataLogging"] = "false"
            })
            .Build();

        services.AddRoadGuardPersistence(config, isProduction: false);
        services.AddRoadGuardSeeding();

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
        var dbContext = scope.ServiceProvider.GetRequiredService<RoadGuardDbContext>();

        var result = await seeder.SeedAsync(dbContext);
        result.StepsExecuted.Should().Be(2);
        result.ExecutedStepNames.Should().ContainInOrder(
            "IdentityRoleSeedStep",
            "DroneDeviceSeedStep");
    }
}
